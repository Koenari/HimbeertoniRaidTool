using System.IO;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal interface IModuleConfigurationManager
{
    bool SaveConfiguration<T>(string internalName, T configData) where T : new();
    bool LoadConfiguration<T>(string internalName, ref T configData) where T : new();
}

internal class ModuleConfigurationManager : IModuleConfigurationManager
{
    private readonly DirectoryInfo _moduleConfigDir;
    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,

    };
    internal ModuleConfigurationManager(DalamudPluginInterface pluginInterface)
    {
        _moduleConfigDir = new DirectoryInfo(pluginInterface.ConfigDirectory.FullName + "\\moduleConfigs\\");
        try
        {
            if (!_moduleConfigDir.Exists)
                _moduleConfigDir.Create();
        }
        catch (Exception) { }
    }

    public bool SaveConfiguration<T>(string internalName, T configData) where T : new()
    {
        FileInfo file = new(_moduleConfigDir.FullName + internalName + ".json");
        string json = JsonConvert.SerializeObject(configData, _jsonSerializerSettings);
        return HrtDataManager.TryWrite(file, json);
    }
    public bool LoadConfiguration<T>(string internalName, ref T configData) where T : new()
    {
        FileInfo file = new(_moduleConfigDir.FullName + internalName + ".json");
        if (file.Exists)
        {
            if (!HrtDataManager.TryRead(file, out string json))
                return false;
            var fromJson = JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
            if (fromJson != null)
            {
                configData = fromJson;
                return true;
            }
            else
                return false;
        }
        return true;
    }
}

internal class NotLoadedModuleConfigurationManager : IModuleConfigurationManager
{
    public bool LoadConfiguration<T>(string internalName, ref T configData) where T : new()
        => false;

    public bool SaveConfiguration<T>(string internalName, T configData) where T : new()
        => false;
}