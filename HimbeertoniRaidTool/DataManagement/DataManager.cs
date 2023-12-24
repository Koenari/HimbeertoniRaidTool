using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public class HrtDataManager
{
    public readonly bool Initialized;
    private volatile bool _saving = false;
    private volatile bool _serializing = false;
    //Data
    private readonly GearDb _gearDb;
    private readonly CharacterDb _characterDb;
    private readonly PlayerDb _playerDb;
    private readonly RaidGroupDb _raidGroupDb;
    private readonly List<RaidGroup>? _groups;
    //File names
    private const string GEAR_DB_JSON_FILE_NAME = "GearDB.json";
    private const string CHAR_DB_JSON_FILE_NAME = "CharacterDB.json";
    private const string PLAYER_DB_JSON_FILE_NAME = "PlayerDB.json";
    private const string RAID_GROUP_DB_JSON_FILE_NAME = "RaidGroupDB.json";
    private const string RAID_GROUP_JSON_FILE_NAME = "RaidGroups.json";
    //Files
    private readonly FileInfo _gearDbJsonFile;
    private readonly FileInfo _charDbJsonFile;
    private readonly FileInfo _playerDbJsonFile;
    private readonly FileInfo _raidGroupDbJsonFile;
    //Directly Accessed Members
    public bool Ready => Initialized && !_serializing;
    [Obsolete]
    internal List<RaidGroup> Groups => _groups ?? new List<RaidGroup>();

    internal IDataBaseTable<RaidGroup> RaidGroupDb
    {
        get
        {
            while (_serializing)
                Thread.Sleep(1);
            return _raidGroupDb;
        }
    }
    internal IDataBaseTable<Player> PlayerDb
    {
        get
        {
            while (_serializing)
                Thread.Sleep(1);
            return _playerDb;
        }
    }
    internal CharacterDb CharDb
    {
        get
        {
            while (_serializing)
                Thread.Sleep(1);
            return _characterDb;
        }
    }
    internal GearDb GearDb
    {
        get
        {
            while (_serializing)
                Thread.Sleep(1);
            return _gearDb;
        }
    }
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
        string configDirName = pluginInterface.ConfigDirectory.FullName;
        //Set up files &folders
        try
        {
            if (!pluginInterface.ConfigDirectory.Exists)
                pluginInterface.ConfigDirectory.Create();
        }
        catch (IOException ioe)
        {
            ServiceManager.PluginLog.Error(ioe, "Could not create data directory");
            loadedSuccessful = false;
        }
        ModuleConfigurationManager = new ModuleConfigurationManager(pluginInterface);
        IdProvider = new LocalIdProvider(this);
        var raidGroupJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{RAID_GROUP_JSON_FILE_NAME}");
        _playerDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{PLAYER_DB_JSON_FILE_NAME}");
        _charDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{CHAR_DB_JSON_FILE_NAME}");
        _raidGroupDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{RAID_GROUP_DB_JSON_FILE_NAME}");
        _gearDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{GEAR_DB_JSON_FILE_NAME}");

        try
        {
            if(!_playerDbJsonFile.Exists)
                _playerDbJsonFile.Create().Close();
            if (!_charDbJsonFile.Exists)
                _charDbJsonFile.Create().Close();
            if(!_raidGroupDbJsonFile.Exists)
                _raidGroupDbJsonFile.Create().Close();
            if (!_gearDbJsonFile.Exists)
                _gearDbJsonFile.Create().Close();
        }
        catch (IOException)
        {
            loadedSuccessful = false;
        }

        //Read files
        loadedSuccessful &= TryRead(_gearDbJsonFile, out string gearJson);
        loadedSuccessful &= TryRead(_charDbJsonFile, out string charDbJson);
        
        _gearDb = new GearDb(IdProvider, gearJson, _jsonSettings);
        _characterDb = new CharacterDb(IdProvider, charDbJson,  new HrtIdReferenceConverter<GearSet>(_gearDb), _jsonSettings);
        var charRefConv = new HrtIdReferenceConverter<Character>(_characterDb);
        //Migration
        if (raidGroupJsonFile.Exists)
        {
            loadedSuccessful = TryRead(raidGroupJsonFile, out string raidGroupJson);
            _jsonSettings.Converters.Add(charRefConv);
            _groups = JsonConvert.DeserializeObject<List<RaidGroup>>(raidGroupJson, _jsonSettings) ?? new List<RaidGroup>();
            _jsonSettings.Converters.Remove(charRefConv);
            _playerDb = new PlayerDb(IdProvider, "[]", charRefConv,_jsonSettings);
            _raidGroupDb = new RaidGroupDb(IdProvider, "[]", new HrtIdReferenceConverter<Player>(_playerDb), _jsonSettings);
            foreach (RaidGroup group in _groups)
            {
                foreach (Player player in group)
                {
                    _playerDb.TryAdd(player);
                }
                _raidGroupDb.TryAdd(group);
            }
            Initialized = true;
            if(Save())
                try
                {
                    raidGroupJsonFile.MoveTo($"{configDirName}{Path.DirectorySeparatorChar}{RAID_GROUP_JSON_FILE_NAME}.bak",true);
                }
                catch (Exception)
                {
                    // ignored
                }
            Initialized = false;
            //Remove old backup files
            File.Delete($"{configDirName}{Path.DirectorySeparatorChar}HrtGearDB.json.bak");
            File.Delete($"{configDirName}{Path.DirectorySeparatorChar}EtroGearDB.json.bak");
            File.Delete($"{configDirName}{Path.DirectorySeparatorChar}CharacterDB.json.bak");
        }
        loadedSuccessful &= TryRead(_playerDbJsonFile, out string playerDbJson);
        loadedSuccessful &= TryRead(_raidGroupDbJsonFile, out string raidGroupDbJson); 
        _playerDb = new PlayerDb(IdProvider, playerDbJson, new HrtIdReferenceConverter<Character>(_characterDb),_jsonSettings);
        _raidGroupDb = new RaidGroupDb(IdProvider, raidGroupDbJson, new HrtIdReferenceConverter<Player>(_playerDb), _jsonSettings);
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
            ServiceManager.PluginLog.Error(e, "Could not load data file");
            return false;
        }
    }
    internal static bool TryWrite(FileInfo file, in string data)
    {

        try
        {
            using StreamWriter writer = file.CreateText();
            writer.Write(data);
            return true;
        }
        catch (Exception e)
        {
            ServiceManager.PluginLog.Error(e, "Could not write data file");
            return false;
        }
    }
    public bool Save()
    {
        if (!Initialized || _saving)
            return false;
        _saving = true;
        DateTime time1 = DateTime.Now;
        //Serialize all data (functions are locked while this happens)
        _serializing = true;
        string characterData = _characterDb.Serialize(_jsonSettings);
        string gearData = _gearDb.Serialize(_jsonSettings);
        string playerData = _playerDb.Serialize(_jsonSettings);
        string groupData = _raidGroupDb.Serialize(_jsonSettings);
        _serializing = false;
        //Write serialized data
        DateTime time2 = DateTime.Now;
        bool hasError = !TryWrite(_gearDbJsonFile, gearData);
        if (!hasError)
            hasError |= !TryWrite(_charDbJsonFile, characterData);
        if (!hasError)
            hasError |= !TryWrite(_playerDbJsonFile, playerData);
        if (!hasError)
            hasError |= !TryWrite(_raidGroupDbJsonFile, groupData);
        _saving = false;
        DateTime time3 = DateTime.Now;
        ServiceManager.PluginLog.Debug($"Database serializing time: {time2 - time1}");
        ServiceManager.PluginLog.Debug($"Database saving IO time: {time3 - time2}");
        return !hasError;
    }
}
public interface IDataBaseTable<T> where T : IHasHrtId
{
    internal bool TryGet(HrtId id, [NotNullWhen(true)] out T? value);
    internal bool TryAdd(in T value);
    internal IEnumerable<T> GetValues();
    internal HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    public HashSet<HrtId> GetReferencedIds();
    internal ulong GetNextSequence();
    internal bool Contains(HrtId hrtId);
    public void RemoveUnused(HashSet<HrtId> referencedIds);
    public void FixEntries();
}

public abstract class DataBaseTable<T, S> : IDataBaseTable<T> where T : class, IHasHrtId where S : IHasHrtId ,new()
{

    protected readonly Dictionary<HrtId, T> Data = new();
    protected readonly HrtIdReferenceConverter<S>? RefConv;
    protected readonly IIdProvider IdProvider;
    protected ulong NextSequence = 0;
    protected bool LoadError = false;
    protected DataBaseTable(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<S>? conv, JsonSerializerSettings settings)
    {
        IdProvider = idProvider;
        RefConv = conv;
        if(RefConv is not null) settings.Converters.Add(RefConv);
        var data = JsonConvert.DeserializeObject<List<T>>(serializedData, settings);
        if(RefConv is not null) settings.Converters.Remove(RefConv);
        if (data is null)
        {
            ServiceManager.PluginLog.Error($"Could not load {typeof(T)} database");
            LoadError = true;
            return;
        }
        foreach (T value in data)
        {
            if (value.LocalId.IsEmpty)
            {
                ServiceManager.PluginLog.Error(
                    $"{typeof(T).Name} {value} was missing an ID and was removed from the database");
                continue;
            }
            if(Data.TryAdd(value.LocalId, value))
                NextSequence = Math.Max(NextSequence, value.LocalId.Sequence);
        }
        NextSequence++;
        ServiceManager.PluginLog.Information($"Database contains {Data.Count} entries of type {typeof(T).Name}");
    }
    public virtual bool TryGet(HrtId id, [NotNullWhen(true)] out T? value) => Data.TryGetValue(id, out value);
    public virtual bool TryAdd(in T c)
    {
        if (c.LocalId.IsEmpty)
            c.LocalId = IdProvider.CreateId(c.IdType);
        return Data.TryAdd(c.LocalId, c);
    }
    public void RemoveUnused(HashSet<HrtId> referencedIds)
    {
        ServiceManager.PluginLog.Debug($"Begin pruning of {typeof(T).Name} database.");
        IEnumerable<HrtId> keyList = new List<HrtId>(Data.Keys);
        foreach (HrtId id in keyList.Where(id => !referencedIds.Contains(id)))
        {
            Data.Remove(id);
            ServiceManager.PluginLog.Information($"Removed {id} from {typeof(T).Name} database");
        }
        ServiceManager.PluginLog.Debug($"Finished pruning of {typeof(T).Name} database.");
    }
    public bool Contains(HrtId hrtId) => Data.ContainsKey(hrtId);
    public IEnumerable<T> GetValues() => Data.Values;
    public ulong GetNextSequence() => NextSequence++;
    public abstract HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    internal string Serialize(JsonSerializerSettings settings)
    {
        if(RefConv is not null) settings.Converters.Add(RefConv);
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        if(RefConv is not null) settings.Converters.Remove(RefConv);
        return result;
    }
    public abstract HashSet<HrtId> GetReferencedIds();
    public virtual void FixEntries() {}

    internal abstract class SearchWindow<Q,R> : HrtWindow where R : IDataBaseTable<Q> where Q : IHasHrtId
    {
        protected readonly R Database;
        private readonly Action<Q> _onSelect;
        private readonly Action? _onCancel;

        protected Q? Selected;
        protected SearchWindow(R dataBase, Action<Q> onSelect, Action? onCancel)
        {
            this._onSelect = onSelect;
            this._onCancel = onCancel;
            Database = dataBase;
        }

        protected void Save()
        {
            if (Selected == null)
                return;
            _onSelect.Invoke(Selected!);
            Hide();
        }

        public override void Draw()
        {
            if (ImGuiHelper.SaveButton(null, Selected is not null))
            {
                Save();
            }
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
            {
                _onCancel?.Invoke();
                Hide();
            }
            DrawContent();
        }
        protected abstract void DrawContent();
    }
}