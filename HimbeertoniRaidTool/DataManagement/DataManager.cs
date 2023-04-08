using System.IO;
using System.Threading;
using Dalamud.Logging;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public class HrtDataManager
{
    public bool Initialized { get; private set; }
    private volatile bool Saving = false;
    private volatile bool Serializing = false;
    //Data
    private readonly GearDB _gearDB;
    private readonly CharacterDB _characterDB;
    private readonly List<RaidGroup>? _Groups;
    //File names
    private const string GearDBJsonFileName = "GearDB.json";
    private const string CharDBJsonFileName = "CharacterDB.json";
    private const string RaidGroupJsonFileName = "RaidGroups.json";
    //Files
    private readonly FileInfo GearDBJsonFile;
    private readonly FileInfo CharDBJsonFile;
    private readonly FileInfo RaidGRoupJsonFile;
    //Converters
    private readonly GearsetReferenceConverter? GearSetRefConv;
    private readonly CharacterReferenceConverter? CharRefConv;
    //Directly Accessed Members
    public bool Ready => Initialized && !Serializing;
    internal List<RaidGroup> Groups => _Groups ?? new();
    internal CharacterDB CharDB => GetCharacterDB();
    internal GearDB GearDB => GetGearSetDB();
    internal readonly IModuleConfigurationManager ModuleConfigurationManager
        = new NotLoadedModuleConfigurationManager();
    internal readonly IIDProvider IDProvider = new NullIDProvider();
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
            PluginLog.Error(ioe, "Could not create data directory");
            loadError = true;
        }
        ModuleConfigurationManager = new ModuleConfigurationManager(pluginInterface);
        LocalIDProvider localIDProvider = new(this);
        IDProvider = localIDProvider;
        RaidGRoupJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{RaidGroupJsonFileName}");
        if (!RaidGRoupJsonFile.Exists)
            RaidGRoupJsonFile.Create().Close();
        CharDBJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{CharDBJsonFileName}");
        if (!CharDBJsonFile.Exists)
            CharDBJsonFile.Create().Close();
        GearDBJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{GearDBJsonFileName}");
        if (!GearDBJsonFile.Exists)
            GearDBJsonFile.Create().Close();
        //Migrate old Data
        if (File.Exists($"{configDirName}{Path.DirectorySeparatorChar}{"HrtGearDB.json"}"))
        {
#pragma warning disable CS0612 // Type or member is obsolete
            bool migrationError = false;
            PluginLog.Information("Started Migration of Data");
            //Fully load old data
            FileInfo HrtGearDBJsonFile = new($"{configDirName}{Path.DirectorySeparatorChar}{"HrtGearDB.json"}");
            FileInfo EtroGearDBJsonFile = new($"{configDirName}{Path.DirectorySeparatorChar}{"EtroGearDB.json"}");
            migrationError |= !TryRead(HrtGearDBJsonFile, out string hrtGearJson);
            migrationError |= !TryRead(EtroGearDBJsonFile, out string etroGearJson);
            migrationError |= !TryRead(CharDBJsonFile, out string oldCharDBJson);
            migrationError |= !TryRead(RaidGRoupJsonFile, out string oldRaidGRoupJson);
            LegacyGearDB oldGearDB = new(hrtGearJson, etroGearJson, JsonSettings);
            LegacyGearSetReferenceConverter gsRefConv = new(oldGearDB);
            LegacyCharacterDB oldCharacterDB = new(oldCharDBJson, gsRefConv, JsonSettings);
            LegacyCharacterReferenceConverter charRefConv = new(oldCharacterDB);
            JsonSettings.Converters.Add(charRefConv);
            var groups = JsonConvert.DeserializeObject<List<RaidGroup>>(oldRaidGRoupJson, JsonSettings) ?? new();
            JsonSettings.Converters.Remove(charRefConv);
            //Real migration
            _gearDB = new(this, oldGearDB, localIDProvider);
            GearSetRefConv = new GearsetReferenceConverter(_gearDB);
            _characterDB = new(this, oldCharacterDB, localIDProvider);
            CharRefConv = new CharacterReferenceConverter(_characterDB);
            _Groups = groups;
            //Save new data and backup old
            HrtGearDBJsonFile.MoveTo(HrtGearDBJsonFile.FullName + ".bak", true);
            EtroGearDBJsonFile.MoveTo(EtroGearDBJsonFile.FullName + ".bak", true);
            CharDBJsonFile.CopyTo(CharDBJsonFile.FullName + ".bak", true);
            RaidGRoupJsonFile.CopyTo(RaidGRoupJsonFile.FullName + ".bak", true);
            Initialized = !migrationError;
            migrationError |= !Save();
            loadError |= migrationError;
#pragma warning restore CS0612 // Type or member is obsolete
        }
        //Read files
        loadError |= !TryRead(GearDBJsonFile, out string gearJson);
        loadError |= !TryRead(CharDBJsonFile, out string charDBJson);
        loadError |= !TryRead(RaidGRoupJsonFile, out string RaidGRoupJson);
        _gearDB = new(this, gearJson, JsonSettings);
        GearSetRefConv = new GearsetReferenceConverter(_gearDB);
        _characterDB = new(this, charDBJson, GearSetRefConv, JsonSettings);
        CharRefConv = new CharacterReferenceConverter(_characterDB);
        JsonSettings.Converters.Add(CharRefConv);
        _Groups = JsonConvert.DeserializeObject<List<RaidGroup>>(RaidGRoupJson, JsonSettings) ?? new();
        JsonSettings.Converters.Remove(CharRefConv);
        Initialized = !loadError;

    }
    private CharacterDB GetCharacterDB()
    {
        while (Serializing)
        {
            Thread.Sleep(1);
        }
        return _characterDB;
    }
    private GearDB GetGearSetDB()
    {
        while (Serializing)
        {
            Thread.Sleep(1);
        }
        return _gearDB;
    }
    internal static bool TryRead(FileInfo file, out string data)
    {
        data = "";
        using var reader = file.OpenText();
        try
        {
            data = reader.ReadToEnd();
            return true;
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Could not load data file");
            return false;
        }
    }
    internal static bool TryWrite(FileInfo file, in string data)
    {
        using var writer = file.CreateText();
        try
        {
            writer.Write(data);
            return true;
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Could not write data file");
            return false;
        }
    }

    public void UpdateEtroSets(int maxAgeDays) => _gearDB?.UpdateEtroSets(maxAgeDays);
    private string SerializeGroupData(CharacterReferenceConverter charRefCon)
    {
        JsonSettings.Converters.Add(charRefCon);
        string result = JsonConvert.SerializeObject(_Groups, JsonSettings);
        JsonSettings.Converters.Remove(charRefCon);
        return result;
    }
    public bool Save()
    {
        if (!Initialized || Saving || _gearDB == null
            || _characterDB == null || GearDBJsonFile == null
            || CharDBJsonFile == null || RaidGRoupJsonFile == null
            || GearSetRefConv == null || CharRefConv == null)
            return false;
        Saving = true;
        var time1 = DateTime.Now;
        //Serialize all data (functions are locked while this happens)
        Serializing = true;
        string characterData = _characterDB.Serialize(GearSetRefConv, JsonSettings);
        string gearData = _gearDB.Serialize(JsonSettings);
        string groupData = SerializeGroupData(CharRefConv);
        Serializing = false;
        //Write serialized data
        var time2 = DateTime.Now;
        bool hasError = !TryWrite(GearDBJsonFile, gearData);
        if (!hasError)
            hasError |= !TryWrite(CharDBJsonFile, characterData);
        if (!hasError)
            hasError |= !TryWrite(RaidGRoupJsonFile, groupData);
        Saving = false;
        var time3 = DateTime.Now;
        PluginLog.Debug($"Serializing time: {time2 - time1}");
        PluginLog.Debug($"IO time: {time3 - time2}");
        return !hasError;
    }
    public class NullIDProvider : IIDProvider
    {
        public HrtID CreateID(HrtID.IDType type) => HrtID.Empty;
        public uint GetAuthorityIdentifier() => 0;
        public bool SignID(HrtID id) => false;
        public bool VerifySignature(HrtID id) => false;
    }
}
