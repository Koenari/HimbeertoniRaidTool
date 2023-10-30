using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using Newtonsoft.Json;
using System.IO;
using System.Threading;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public class HrtDataManager
{
    public readonly bool Initialized;
    private volatile bool _saving = false;
    private volatile bool _serializing = false;
    //Data
    private readonly GearDb _gearDb;
    private readonly CharacterDb _characterDb;
    private readonly List<RaidGroup>? _groups;
    //File names
    private const string GEAR_DB_JSON_FILE_NAME = "GearDB.json";
    private const string CHAR_DB_JSON_FILE_NAME = "CharacterDB.json";
    private const string RAID_GROUP_JSON_FILE_NAME = "RaidGroups.json";
    //Files
    private readonly FileInfo _gearDbJsonFile;
    private readonly FileInfo _charDbJsonFile;
    private readonly FileInfo _raidGroupJsonFile;
    //Converters
    private readonly HrtIdReferenceConverter<GearSet>? _gearSetRefConv;
    private readonly HrtIdReferenceConverter<Character>? _charRefConv;
    //Directly Accessed Members
    public bool Ready => Initialized && !_serializing;
    internal List<RaidGroup> Groups => _groups ?? new List<RaidGroup>();
    internal CharacterDb CharDb => GetCharacterDb();
    internal GearDb GearDb => GetGearSetDb();
    internal readonly IModuleConfigurationManager ModuleConfigurationManager
        = new NotLoadedModuleConfigurationManager();
    internal readonly IIdProvider IdProvider = new NullIdProvider();
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
        bool loadError = false;
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
            loadError = true;
        }
        ModuleConfigurationManager = new ModuleConfigurationManager(pluginInterface);
        LocalIdProvider localIdProvider = new(this);
        IdProvider = localIdProvider;
        _raidGroupJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{RAID_GROUP_JSON_FILE_NAME}");
        _charDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{CHAR_DB_JSON_FILE_NAME}");
        _gearDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{GEAR_DB_JSON_FILE_NAME}");
        try
        {
            if (!_raidGroupJsonFile.Exists)
                _raidGroupJsonFile.Create().Close();
            if (!_charDbJsonFile.Exists)
                _charDbJsonFile.Create().Close();
            if (!_gearDbJsonFile.Exists)
                _gearDbJsonFile.Create().Close();
        }
        catch (IOException)
        {
            loadError = true;
        }
        //Read files
        loadError |= !TryRead(_gearDbJsonFile, out string gearJson);
        loadError |= !TryRead(_charDbJsonFile, out string charDbJson);
        loadError |= !TryRead(_raidGroupJsonFile, out string raidGroupJson);
        _gearDb = new GearDb(this, gearJson, _jsonSettings);
        _gearSetRefConv = new HrtIdReferenceConverter<GearSet>(_gearDb);
        _characterDb = new CharacterDb(this, charDbJson, _gearSetRefConv, _jsonSettings);
        _charRefConv = new HrtIdReferenceConverter<Character>(_characterDb);
        _jsonSettings.Converters.Add(_charRefConv);
        _groups = JsonConvert.DeserializeObject<List<RaidGroup>>(raidGroupJson, _jsonSettings) ?? new List<RaidGroup>();
        _jsonSettings.Converters.Remove(_charRefConv);
        Initialized = !loadError;
    }

    internal void PruneDatabase()
    {
        if (!Initialized) return;
        CharDb.Prune(this);
        GearDb.Prune(CharDb);
    }

    internal IEnumerable<HrtId> FindOrphanedCharacters(IEnumerable<HrtId> possibleOrphans)
    {
        HashSet<HrtId> orphans = new(possibleOrphans);
        if (_groups is null) return Array.Empty<HrtId>();
        foreach (Character character in _groups.SelectMany(g => g).SelectMany(p => p.Chars))
        {
            orphans.Remove(character.LocalId);
        }
        ServiceManager.PluginLog.Information($"Found {orphans.Count} orphaned characters.");
        return orphans;
    }
    private CharacterDb GetCharacterDb()
    {
        while (_serializing)
        {
            Thread.Sleep(1);
        }
        return _characterDb;
    }
    private GearDb GetGearSetDb()
    {
        while (_serializing)
        {
            Thread.Sleep(1);
        }
        return _gearDb;
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

    // ReSharper disable once SuggestBaseTypeForParameter
    private string SerializeGroupData(HrtIdReferenceConverter<Character> charRefCon)
    {
        _jsonSettings.Converters.Add(charRefCon);
        string result = JsonConvert.SerializeObject(_groups, _jsonSettings);
        _jsonSettings.Converters.Remove(charRefCon);
        return result;
    }
    public bool Save()
    {
        if (!Initialized || _saving || _gearDb == null
            || _characterDb == null || _gearDbJsonFile == null
            || _charDbJsonFile == null || _raidGroupJsonFile == null
            || _gearSetRefConv == null || _charRefConv == null)
            return false;
        _saving = true;
        DateTime time1 = DateTime.Now;
        //Serialize all data (functions are locked while this happens)
        _serializing = true;
        string characterData = _characterDb.Serialize(_gearSetRefConv, _jsonSettings);
        string gearData = _gearDb.Serialize(_jsonSettings);
        string groupData = SerializeGroupData(_charRefConv);
        _serializing = false;
        //Write serialized data
        DateTime time2 = DateTime.Now;
        bool hasError = !TryWrite(_gearDbJsonFile, gearData);
        if (!hasError)
            hasError |= !TryWrite(_charDbJsonFile, characterData);
        if (!hasError)
            hasError |= !TryWrite(_raidGroupJsonFile, groupData);
        _saving = false;
        DateTime time3 = DateTime.Now;
        ServiceManager.PluginLog.Debug($"Serializing time: {time2 - time1}");
        ServiceManager.PluginLog.Debug($"IO time: {time3 - time2}");
        return !hasError;
    }

    public class NullIdProvider : IIdProvider
    {
        public HrtId CreateId(HrtId.IdType type) => HrtId.Empty;
        public uint GetAuthorityIdentifier() => 0;
        public bool SignId(HrtId id) => false;
        public bool VerifySignature(HrtId id) => false;
    }


}

public interface IDataBaseTable<T>
{
    public bool TryGet(HrtId id, [NotNullWhen(true)] out T? value);

}