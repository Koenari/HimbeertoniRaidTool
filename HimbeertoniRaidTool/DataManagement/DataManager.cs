using System.IO;
using System.Threading;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public class HrtDataManager
{
    private readonly bool _initialized;
    private volatile bool _saving;
    private readonly ILogger _logger;
    //Data
    private readonly DataBaseWrapper<GearSet> _gearDb;
    private readonly DataBaseWrapper<Character> _characterDb;
    private readonly DataBaseWrapper<Player> _playerDb;
    private readonly DataBaseWrapper<RaidGroup> _raidGroupDb;
    private readonly DataBaseWrapper<RaidSession> _raidSessionDb;

    //Directly Accessed Members
    public bool Ready => _initialized && !_saving;
    private readonly string _saveDir;

    internal readonly IModuleConfigurationManager ModuleConfigurationManager;
    private readonly List<JsonConverter> _idRefConverters = [];
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
    };
    public HrtDataManager(IDalamudPluginInterface pluginInterface, ILogger logger, IDataManager dataManager)
    {
        _logger = logger;
        bool loadedSuccessful = true;
        //Set up files &folders
        try
        {
            if (!pluginInterface.ConfigDirectory.Exists)
                pluginInterface.ConfigDirectory.Create();
        }
        catch (IOException ioe)
        {
            _logger.Error(ioe, "Could not create data directory");
            throw new FailedToLoadException("Could not create data directory");
        }
        _saveDir = pluginInterface.ConfigDirectory.FullName;
        ModuleConfigurationManager = new ModuleConfigurationManager(this, _saveDir);
        IIdProvider idProvider = new LocalIdProvider(this);
        _gearDb = new DataBaseWrapper<GearSet>(this, new GearDb(idProvider, logger), "GearDB.json");
        _characterDb =
            new DataBaseWrapper<Character>(this, new CharacterDb(idProvider, _idRefConverters, logger, dataManager),
                                           "CharacterDB.json");

        _playerDb = new DataBaseWrapper<Player>(this, new PlayerDb(idProvider, _idRefConverters, logger),
                                                "PlayerDB.json");
        _raidGroupDb =
            new DataBaseWrapper<RaidGroup>(this, new RaidGroupDb(idProvider, _idRefConverters, logger),
                                           "RaidGroupDB.json");
        _raidSessionDb = new DataBaseWrapper<RaidSession>(this, new RaidSessionDb(idProvider, _idRefConverters, logger),
                                                          "RaidSessionDB.json");

        loadedSuccessful &= _gearDb.Load();
        loadedSuccessful &= _characterDb.Load();
        loadedSuccessful &= _playerDb.Load();
        loadedSuccessful &= _raidGroupDb.Load();
        loadedSuccessful &= _raidSessionDb.Load();

        _initialized = loadedSuccessful;
        if (!_initialized)
            throw new FailedToLoadException("Could not initialize data manager");
    }

    internal void CleanupDatabase()
    {
        if (!_initialized) return;
        /*
         * Keeping characters and players in DB for users to add again later
         *
         * PlayerDb.RemoveUnused(RaidGroupDb.GetReferencedIds());
         * CharDb.RemoveUnused(PlayerDb.GetReferencedIds());
         */
        _gearDb.RemoveUnused(GetTable<Character>().GetReferencedIds());
        _raidSessionDb.FixEntries(this);
        _raidGroupDb.FixEntries(this);
        _playerDb.FixEntries(this);
        _characterDb.FixEntries(this);
        _gearDb.FixEntries(this);
    }

    internal bool TryRead(FileInfo file, out string data)
    {
        data = "";
        try
        {
            using var reader = file.OpenText();
            data = reader.ReadToEnd();
            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Could not load data file");
            return false;
        }
    }
    internal bool TryWrite(FileInfo file, string data)
    {
        try
        {
            FilesystemUtil.WriteAllTextSafe(file.FullName, data);
            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Could not write data file: {FileFullName}", file.FullName);
            return false;
        }
    }

    public IDataBaseTable<TData> GetTable<TData>() where TData : class, IHrtDataTypeWithId<TData> =>
        typeof(TData) switch
        {
            var cls when cls == typeof(GearSet)     => _gearDb.Database as IDataBaseTable<TData>,
            var cls when cls == typeof(Character)   => _characterDb.Database as IDataBaseTable<TData>,
            var cls when cls == typeof(Player)      => _playerDb.Database as IDataBaseTable<TData>,
            var cls when cls == typeof(RaidGroup)   => _raidGroupDb.Database as IDataBaseTable<TData>,
            var cls when cls == typeof(RaidSession) => _raidSessionDb.Database as IDataBaseTable<TData>,
            _                                       => null,

        } ?? throw new ArgumentOutOfRangeException($"No table exists for type: {typeof(TData)} ");

    public bool Save()
    {
        if (!_initialized || _saving)
            return false;
        //Saving all data (functions are locked while this happens)
        _saving = true;
        var time1 = DateTime.Now;
        bool savedSuccessful = _gearDb.Save();
        if (savedSuccessful)
            savedSuccessful &= _characterDb.Save();
        if (savedSuccessful)
            savedSuccessful &= _playerDb.Save();
        if (savedSuccessful)
            savedSuccessful &= _raidGroupDb.Save();
        if (savedSuccessful)
            savedSuccessful &= _raidSessionDb.Save();
        _saving = false;
        var time2 = DateTime.Now;
        _logger.Debug("Database saving time: {TimeSpan}", time2 - time1);
        return savedSuccessful;
    }

    private class DataBaseWrapper<TEntry> where TEntry : class, IHrtDataTypeWithId<TEntry>
    {
        private readonly HrtDataManager _parent;
        private readonly IInternalDataBaseTable<TEntry> _database;
        internal IDataBaseTable<TEntry> Database
        {
            get
            {
                while (_parent._saving)
                {
                    Thread.Sleep(1);
                }
                return _database;
            }
        }
        private readonly FileInfo _file;

        internal DataBaseWrapper(HrtDataManager parent, IInternalDataBaseTable<TEntry> database, string fileName)
        {
            _parent = parent;
            _database = database;
            _file = new FileInfo($"{parent._saveDir}{Path.DirectorySeparatorChar}{fileName}");
            _parent._idRefConverters.Add(_database.GetOldRefConverter());
            _parent._idRefConverters.Add(_database.GetRefConverter());
        }
        internal bool Load()
        {
            if (!_file.Exists)
                return LoadEmpty();
            if (!_parent.TryRead(_file, out string jsonData))
            {
                LoadEmpty();
                return false;
            }
            try
            {
                return _database.Load(_jsonSettings, jsonData);
            }
            catch (JsonSerializationException e)
            {
                _parent._logger.Error(e, "Could not load {Type} data.", typeof(TEntry));
                LoadEmpty();
                return false;
            }
        }
        private bool LoadEmpty() => _database.Load(_jsonSettings, "[]");
        internal bool Save() => _parent.TryWrite(_file, _database.Serialize(_jsonSettings));
        internal void RemoveUnused(HashSet<HrtId> ids) => _database.RemoveUnused(ids);
        internal void FixEntries(HrtDataManager parent) => _database.FixEntries(parent);
    }

}