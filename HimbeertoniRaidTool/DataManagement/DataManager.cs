using System.ComponentModel;
using System.IO;
using System.Threading;
using Dalamud.Plugin;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public class HrtDataManager
{
    public readonly bool Initialized;
    private volatile bool _saving = false;
    //Data
    private readonly DataBaseWrapper<GearDb, GearSet> _gearDb;
    private readonly DataBaseWrapper<CharacterDb, Character> _characterDb;
    private readonly DataBaseWrapper<PlayerDb, Player> _playerDb;
    private readonly DataBaseWrapper<RaidGroupDb, RaidGroup> _raidGroupDb;
    private readonly DataBaseWrapper<RaidSessionDb, RaidSession> _raidSessionDb;
    private readonly List<RaidGroup>? _groups;

    //Directly Accessed Members
    public bool Ready => Initialized && !_saving;
    internal readonly string SaveDir;
    [Obsolete]
    internal List<RaidGroup> Groups => _groups ?? new List<RaidGroup>();

    internal IDataBaseTable<RaidSession> RaidSessionDb => _raidSessionDb.Database;
    internal IDataBaseTable<RaidGroup> RaidGroupDb => _raidGroupDb.Database;
    internal IDataBaseTable<Player> PlayerDb => _playerDb.Database;
    internal CharacterDb CharDb => _characterDb.Database;
    internal GearDb GearDb => _gearDb.Database;
    internal readonly IModuleConfigurationManager ModuleConfigurationManager;
    internal readonly IIdProvider IdProvider;
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
    };
    public HrtDataManager(DalamudPluginInterface pluginInterface)
    {
        bool loadedSuccessful = true;
        //Set up files &folders
        try
        {
            if (!pluginInterface.ConfigDirectory.Exists)
                pluginInterface.ConfigDirectory.Create();
        }
        catch (IOException ioe)
        {
            ServiceManager.Logger.Error(ioe, "Could not create data directory");
            throw new FailedToLoadException("Could not create data directory");
        }
        SaveDir = pluginInterface.ConfigDirectory.FullName;
        ModuleConfigurationManager = new ModuleConfigurationManager(pluginInterface);
        IdProvider = new LocalIdProvider(this);
        _gearDb = new DataBaseWrapper<GearDb, GearSet>(this, new GearDb(IdProvider), "GearDB.json");
        var gearRefConv = new HrtIdReferenceConverter<GearSet>(_gearDb.Database);
        _characterDb =
            new DataBaseWrapper<CharacterDb, Character>(this, new CharacterDb(IdProvider, new[] { gearRefConv }),
                                                        "CharacterDB.json");
        var charRefConv = new HrtIdReferenceConverter<Character>(_characterDb.Database);
        _playerDb = new DataBaseWrapper<PlayerDb, Player>(this, new PlayerDb(IdProvider, new[] { charRefConv }),
                                                          "PlayerDB.json");
        var playerRefConv = new HrtIdReferenceConverter<Player>(_playerDb.Database);
        _raidGroupDb = new DataBaseWrapper<RaidGroupDb, RaidGroup>(
            this, new RaidGroupDb(IdProvider, new[] { playerRefConv }),
            "RaidGroupDB.json");
        _raidSessionDb = new DataBaseWrapper<RaidSessionDb, RaidSession>(
            this,
            new RaidSessionDb(
                IdProvider,
                new JsonConverter[] { playerRefConv, new HrtIdReferenceConverter<RaidGroup>(_raidGroupDb.Database) }),
            "RaidSessionDB.json");

        loadedSuccessful &= _gearDb.Load();
        loadedSuccessful &= _characterDb.Load();

        //Migration
        var raidGroupJsonFile = new FileInfo($"{SaveDir}{Path.DirectorySeparatorChar}RaidGroups.json");
        if (raidGroupJsonFile.Exists)
        {
            loadedSuccessful = TryRead(raidGroupJsonFile, out string raidGroupJson);
            _jsonSettings.Converters.Add(charRefConv);
            _groups = JsonConvert.DeserializeObject<List<RaidGroup>>(raidGroupJson, _jsonSettings)
                   ?? new List<RaidGroup>();
            _jsonSettings.Converters.Remove(charRefConv);
            _playerDb.LoadEmpty();
            _raidGroupDb.LoadEmpty();
            foreach (RaidGroup group in _groups)
            {
                foreach (Player player in group)
                {
                    _playerDb.Database.TryAdd(player);
                }
                _raidGroupDb.Database.TryAdd(group);
            }
            Initialized = true;
            if (Save())
                try
                {
                    raidGroupJsonFile.MoveTo(raidGroupJsonFile.FullName + ".bak", true);
                }
                catch (Exception)
                {
                    // ignored
                }
            Initialized = false;
            //Remove old backup files
            try
            {
                File.Delete($"{SaveDir}{Path.DirectorySeparatorChar}HrtGearDB.json.bak");
                File.Delete($"{SaveDir}{Path.DirectorySeparatorChar}EtroGearDB.json.bak");
                File.Delete($"{SaveDir}{Path.DirectorySeparatorChar}CharacterDB.json.bak");
            }
            catch (Exception e) when (e is ArgumentException or ArgumentNullException or DirectoryNotFoundException
                                        or IOException or NotSupportedException or PathTooLongException
                                        or UnauthorizedAccessException) { }

        }
        loadedSuccessful &= _playerDb.Load();
        loadedSuccessful &= _raidGroupDb.Load();
        loadedSuccessful &= _raidSessionDb.Load();

        Initialized = loadedSuccessful;
    }

    internal void CleanupDatabase()
    {
        if (!Initialized) return;
        /*
         * Keeping characters and players in DB for users to add again later
         *
         * PlayerDb.RemoveUnused(RaidGroupDb.GetReferencedIds());
         * CharDb.RemoveUnused(PlayerDb.GetReferencedIds());
         */
        GearDb.RemoveUnused(CharDb.GetReferencedIds());
        RaidSessionDb.FixEntries();
        RaidGroupDb.FixEntries();
        PlayerDb.FixEntries();
        CharDb.FixEntries();
        GearDb.FixEntries();
    }

    internal static bool TryRead(FileInfo file, out string data)
    {
        data = "";
        try
        {
            using StreamReader reader = file.OpenText();
            data = reader.ReadToEnd();
            return true;
        }
        catch (Exception e)
        {
            ServiceManager.Logger.Error(e, "Could not load data file");
            return false;
        }
    }
    public bool Save()
    {
        if (!Initialized || _saving)
            return false;
        //Saving all data (functions are locked while this happens)
        _saving = true;
        DateTime time1 = DateTime.Now;
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
        DateTime time2 = DateTime.Now;
        ServiceManager.Logger.Debug($"Database saving time: {time2 - time1}");
        return savedSuccessful;
    }

    private class DataBaseWrapper<TDb, TEntry> where TDb : IDataBaseTable<TEntry> where TEntry : IHasHrtId
    {
        private readonly HrtDataManager _parent;
        private readonly TDb _database;
        internal TDb Database
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

        internal DataBaseWrapper(HrtDataManager parent, TDb database, string fileName)
        {
            _parent = parent;
            _database = database;
            _file = new FileInfo($"{parent.SaveDir}{Path.DirectorySeparatorChar}{fileName}");
        }
        internal bool Load()
        {
            if (!_file.Exists)
                return LoadEmpty();
            if (!TryRead(_file, out string jsonData))
            {
                LoadEmpty();
                return false;
            }
            try
            {
                return _database.Load(_jsonSettings, jsonData);
            }
            catch (JsonSerializationException)
            {
                LoadEmpty();
                return false;
            }
        }
        internal bool LoadEmpty() => _database.Load(_jsonSettings, "[]");
        internal bool Save()
        {
            string data = _database.Serialize(_jsonSettings);
            try
            {
                Util.WriteAllTextSafe(_file.FullName, data);
                return true;
            }
            catch (Win32Exception e)
            {
                ServiceManager.Logger.Error(e, "Could not write data file");
                return false;
            }
        }
    }
}