using System.ComponentModel;
using System.IO;
using Dalamud.Utility;
using Newtonsoft.Json;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal interface IModuleConfigurationManager
{
    bool SaveConfiguration<T>(string internalName, T configData) where T : IHrtConfigData, new();
    bool LoadConfiguration<T>(string internalName, ref T configData) where T : IHrtConfigData, new();
}

internal class ModuleConfigurationManager : IModuleConfigurationManager
{
    private readonly DirectoryInfo _moduleConfigDir;
    private readonly HrtDataManager _parent;
    private readonly ILogger _logger;
    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,

    };
    internal ModuleConfigurationManager(HrtDataManager parent, ILogger logger, string configDir)
    {
        _parent = parent;
        _logger = logger;
        _moduleConfigDir = new DirectoryInfo(configDir + "\\moduleConfigs\\");
        try
        {
            if (!_moduleConfigDir.Exists)
                _moduleConfigDir.Create();
        }
        catch (IOException)
        {
        }
    }

    public bool SaveConfiguration<T>(string internalName, T configData) where T : IHrtConfigData, new()
    {
        configData.BeforeSave();
        FileInfo file = new(_moduleConfigDir.FullName + internalName + ".json");
        string json = JsonConvert.SerializeObject(configData, _jsonSerializerSettings);
        bool writeSuccess;
        try
        {
            FilesystemUtil.WriteAllTextSafe(file.FullName, json);
            writeSuccess = true;
        }
        catch (Win32Exception)
        {
            writeSuccess = false;
        }
        return writeSuccess;
    }
    public bool LoadConfiguration<T>(string internalName, ref T configData) where T : IHrtConfigData, new()
    {
        FileInfo file = new(_moduleConfigDir.FullName + internalName + ".json");
        if (!file.Exists) return true;
        if (!FileHelpers.TryRead(file, out string json, _logger))
            return false;
        var fromJson = JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        if (fromJson != null)
        {
            configData = fromJson;
            configData.AfterLoad(_parent);
            return true;
        }
        else
            return false;
    }
}