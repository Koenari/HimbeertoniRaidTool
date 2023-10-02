
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
    private readonly GearDB _gearDb;
    private readonly CharacterDB _characterDb;
    private readonly List<RaidGroup>? _groups;
    //File names
    private const string GearDbJsonFileName = "GearDB.json";
    private const string CharDbJsonFileName = "CharacterDB.json";
    private const string RaidGroupJsonFileName = "RaidGroups.json";
    //Files
    private readonly FileInfo _gearDbJsonFile;
    private readonly FileInfo _charDbJsonFile;
    private readonly FileInfo _raidGroupJsonFile;
    //Converters
    private readonly GearsetReferenceConverter? _gearSetRefConv;
    private readonly CharacterReferenceConverter? _charRefConv;
    //Directly Accessed Members
    public bool Ready => Initialized && !_serializing;
    internal List<RaidGroup> Groups => _groups ?? new List<RaidGroup>();
    internal CharacterDB CharDB => GetCharacterDb();
    internal GearDB GearDB => GetGearSetDb();
    internal readonly IModuleConfigurationManager ModuleConfigurationManager
        = new NotLoadedModuleConfigurationManager();
    internal readonly IIDProvider IDProvider = new NullIdProvider();
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
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
        LocalIDProvider localIDProvider = new(this);
        IDProvider = localIDProvider;
        _raidGroupJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{RaidGroupJsonFileName}");
        _charDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{CharDbJsonFileName}");
        _gearDbJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{GearDbJsonFileName}");
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
        //Migrate old Data
        if (File.Exists($"{configDirName}{Path.DirectorySeparatorChar}{"HrtGearDB.json"}"))
        {
#pragma warning disable CS0612 // Type or member is obsolete
            bool migrationError = false;
            ServiceManager.PluginLog.Information("Started Migration of Data");
            //Fully load old data
            FileInfo HrtGearDBJsonFile = new($"{configDirName}{Path.DirectorySeparatorChar}{"HrtGearDB.json"}");
            FileInfo EtroGearDBJsonFile = new($"{configDirName}{Path.DirectorySeparatorChar}{"EtroGearDB.json"}");
            migrationError |= !TryRead(HrtGearDBJsonFile, out string hrtGearJson);
            migrationError |= !TryRead(EtroGearDBJsonFile, out string etroGearJson);
            migrationError |= !TryRead(_charDbJsonFile, out string oldCharDBJson);
            migrationError |= !TryRead(_raidGroupJsonFile, out string oldRaidGRoupJson);
            LegacyGearDB oldGearDB = new(hrtGearJson, etroGearJson, JsonSettings);
            LegacyGearSetReferenceConverter gsRefConv = new(oldGearDB);
            LegacyCharacterDB oldCharacterDB = new(oldCharDBJson, gsRefConv, JsonSettings);
            LegacyCharacterReferenceConverter charRefConv = new(oldCharacterDB);
            JsonSettings.Converters.Add(charRefConv);
            var groups = JsonConvert.DeserializeObject<List<RaidGroup>>(oldRaidGRoupJson, JsonSettings) ?? new();
            JsonSettings.Converters.Remove(charRefConv);
            //Real migration
            _gearDb = new(this, oldGearDB, localIDProvider);
            _gearSetRefConv = new GearsetReferenceConverter(_gearDb);
            _characterDb = new(this, oldCharacterDB, localIDProvider);
            _charRefConv = new CharacterReferenceConverter(_characterDb);
            _groups = groups;
            //Save new data and backup old
            HrtGearDBJsonFile.MoveTo(HrtGearDBJsonFile.FullName + ".bak", true);
            EtroGearDBJsonFile.MoveTo(EtroGearDBJsonFile.FullName + ".bak", true);
            _charDbJsonFile.CopyTo(_charDbJsonFile.FullName + ".bak", true);
            _raidGroupJsonFile.CopyTo(_raidGroupJsonFile.FullName + ".bak", true);
            Initialized = !migrationError;
            migrationError |= !Save();
            if (migrationError)
                ServiceManager.PluginLog.Error("Database migration failed");
            else
                ServiceManager.PluginLog.Information("Database migration ended successful");
            loadError |= migrationError;
#pragma warning restore CS0612 // Type or member is obsolete
        }
        //Read files
        loadError |= !TryRead(_gearDbJsonFile, out string gearJson);
        loadError |= !TryRead(_charDbJsonFile, out string charDBJson);
        loadError |= !TryRead(_raidGroupJsonFile, out string RaidGRoupJson);
        _gearDb = new(this, gearJson, JsonSettings);
        _gearSetRefConv = new GearsetReferenceConverter(_gearDb);
        _characterDb = new(this, charDBJson, _gearSetRefConv, JsonSettings);
        _charRefConv = new CharacterReferenceConverter(_characterDb);
        JsonSettings.Converters.Add(_charRefConv);
        _groups = JsonConvert.DeserializeObject<List<RaidGroup>>(RaidGRoupJson, JsonSettings) ?? new();
        JsonSettings.Converters.Remove(_charRefConv);
        Initialized = !loadError;

    }

    internal void PruneDatabase()
    {
        if(!Initialized) return;
        CharDB.Prune(this);
        GearDB.Prune(CharDB);
    }

    internal IEnumerable<HrtID> FindOrphanedCharacters(IEnumerable<HrtID> possibleOrphans)
    {
        HashSet<HrtID> orphans = new(possibleOrphans);
        if (_groups is null) return Array.Empty<HrtID>();
        foreach (Character character in _groups.SelectMany(g => g).SelectMany(p => p.Chars))
        {
            orphans.Remove(character.LocalID);
        }
        ServiceManager.PluginLog.Information($"Found {orphans.Count} orphaned characters.");
        return orphans;
    }
    private CharacterDB GetCharacterDb()
    {
        while (_serializing)
        {
            Thread.Sleep(1);
        }
        return _characterDb;
    }
    private GearDB GetGearSetDb()
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
    private string SerializeGroupData(CharacterReferenceConverter charRefCon)
    {
        JsonSettings.Converters.Add(charRefCon);
        string result = JsonConvert.SerializeObject(_groups, JsonSettings);
        JsonSettings.Converters.Remove(charRefCon);
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
        string characterData = _characterDb.Serialize(_gearSetRefConv, JsonSettings);
        string gearData = _gearDb.Serialize(JsonSettings);
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
    public class NullIdProvider : IIDProvider
    {
        public HrtID CreateID(HrtID.IDType type) => HrtID.Empty;
        public uint GetAuthorityIdentifier() => 0;
        public bool SignID(HrtID id) => false;
        public bool VerifySignature(HrtID id) => false;
    }
}
