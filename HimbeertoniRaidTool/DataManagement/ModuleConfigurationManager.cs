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
    private readonly DirectoryInfo ModuleConfigDir;
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,

    };
    internal ModuleConfigurationManager(DalamudPluginInterface pluginInterface)
    {
        ModuleConfigDir = new(pluginInterface.ConfigDirectory.FullName + "\\moduleConfigs\\");
        try
        {
            if (!ModuleConfigDir.Exists)
                ModuleConfigDir.Create();
        }
        catch (Exception) { }
    }

    public bool SaveConfiguration<T>(string internalName, T configData) where T : new()
    {
        FileInfo file = new(ModuleConfigDir.FullName + internalName + ".json");
        string json = JsonConvert.SerializeObject(configData, JsonSerializerSettings);
        return HrtDataManager.TryWrite(file, json);
    }
    public bool LoadConfiguration<T>(string internalName, ref T configData) where T : new()
    {
        FileInfo file = new(ModuleConfigDir.FullName + internalName + ".json");
        if (file.Exists)
        {
            if (!HrtDataManager.TryRead(file, out string json))
                return false;
            var fromJson = JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
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
