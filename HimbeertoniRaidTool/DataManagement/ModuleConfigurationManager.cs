using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    internal static class ModuleConfigurationManager
    {
        private static readonly DirectoryInfo ModuleConfigDir = new(Services.PluginInterface.ConfigDirectory.FullName + "\\moduleConfigs\\");
        private static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,

        };
        static ModuleConfigurationManager()
        {
            if (!ModuleConfigDir.Exists)
                ModuleConfigDir.Create();
        }

        internal static void SaveConfiguration<T>(string internalName, T configData) where T : new()
        {
            var file = new FileInfo(ModuleConfigDir.FullName + internalName + ".json");
            var json = JsonConvert.SerializeObject(configData, JsonSerializerSettings);
            File.WriteAllText(file.FullName, json);
        }
        internal static bool LoadConfiguration<T>(string internalName, ref T configData) where T : new()
        {
            var file = new FileInfo(ModuleConfigDir.FullName + internalName + ".json");
            if (file.Exists)
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
            else
                return false;
        }
    }
}
