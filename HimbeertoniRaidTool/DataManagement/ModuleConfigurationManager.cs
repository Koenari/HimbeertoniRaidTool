using System;
using System.IO;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    internal class ModuleConfigurationManager
    {
        private readonly DirectoryInfo ModuleConfigDir = new(Services.PluginInterface.ConfigDirectory.FullName + "\\moduleConfigs\\");
        private static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,

        };
        internal ModuleConfigurationManager()
        {
            if (!ModuleConfigDir.Exists)
                ModuleConfigDir.Create();
        }

        internal void SaveConfiguration<T>(string internalName, T configData) where T : new()
        {
            var file = new FileInfo(ModuleConfigDir.FullName + internalName + ".json");
            string json = JsonConvert.SerializeObject(configData, JsonSerializerSettings);
            File.WriteAllText(file.FullName, json);
        }
        internal bool LoadConfiguration<T>(string internalName, ref T configData) where T : new()
        {
            var file = new FileInfo(ModuleConfigDir.FullName + internalName + ".json");
            if (file.Exists)
            {
                try
                {
                    T? fromJson = JsonConvert.DeserializeObject<T>(file.OpenText().ReadToEnd(), JsonSerializerSettings);
                    if (fromJson != null)
                    {
                        configData = fromJson;
                        return true;
                    }
                    else
                        return false;
                }
                catch (Exception e)
                {
                    PluginLog.Error("Could not load module config \n {0}", e);
                    return false;
                }

            }
            return true;
        }
    }
}
