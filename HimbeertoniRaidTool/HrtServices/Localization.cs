using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using Dalamud.Plugin;
#if DEBUG
using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Newtonsoft.Json;
using OpCodes = Mono.Cecil.Cil.OpCodes;
#endif
namespace HimbeertoniRaidTool.HrtServices
{
    internal static class Localization
    {
        private static bool FallBack = true;
        private static Dalamud.Localization? Loc;
        private static readonly Dictionary<string, string> _localizationCache = new();
        internal static void Init(DalamudPluginInterface pluginInterface)
        {
            if (Loc is not null)
                return;
            string localePath = Path.Combine(pluginInterface.AssemblyLocation.DirectoryName!, @"locale");
            Loc = new(localePath, "HimbeertoniRaidTool_");
            Loc.SetupWithLangCode(pluginInterface.UiLanguage);
            pluginInterface.LanguageChanged += OnLanguageChanged;
        }
        public static string Localize(string id, string fallBack)
        {
            if (FallBack || Loc is null)
                return fallBack;
            if (_localizationCache.TryGetValue(id, out string? val))
                return val;
            else
                return _localizationCache[id] = Dalamud.Localization.Localize(id, fallBack);
        }
        private static void OnLanguageChanged(string langCode)
        {
            FallBack = langCode.Equals("en");
            PluginLog.Information($"Loading localization for {langCode}");
            Loc?.SetupWithLangCode(langCode);
            _localizationCache.Clear();
        }
        public static void Dispose()
        {
            Services.PluginInterface.LanguageChanged -= OnLanguageChanged;
        }
#if DEBUG
        internal static void ExportLocalizable()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var debugOutput = string.Empty;
            var outList = new Dictionary<string, LocEntry>();

            var assemblyDef = AssemblyDefinition.ReadAssembly(assembly.Location);

            var toInspect = assemblyDef.MainModule.GetTypes()
                .SelectMany(t => t.Methods
                    .Where(m => m.HasBody)
                    .Select(m => new { t, m }));

            foreach (var tm in toInspect)
            {
                var instructions = tm.m.Body.Instructions;

                foreach (var instruction in instructions)
                {
                    if (instruction.OpCode == OpCodes.Call)
                    {
                        var methodInfo = instruction.Operand as MethodReference;

                        if (methodInfo != null)
                        {
                            var methodType = methodInfo.DeclaringType;
                            var parameters = methodInfo.Parameters;

                            if (!methodInfo.Name.Contains("Localize"))
                                continue;

                            debugOutput += string.Format("->{0}.{1}.{2}({3});\n",
                                    tm.t.FullName,
                                    methodType.Name,
                                    methodInfo.Name,
                                    string.Join(", ",
                                        parameters.Select(p =>
                                            p.ParameterType.FullName + " " + p.Name).ToArray())
                                );

                            var entry = new LocEntry
                            {
                                Message = instruction.Previous.Operand as string ?? string.Empty,
                                Description = $"{tm.t.Name}.{tm.m.Name}"
                            };

                            string? key = instruction.Previous.Previous.Operand as string;

                            if (string.IsNullOrEmpty(key))
                            {
                                debugOutput += $"Key was empty for message: {entry.Message} (from {entry.Description}) in {tm.t.FullName}::{tm.m.FullName}\n";
                                continue;
                            }

                            if (outList.Any(x => x.Key == key))
                            {
                                if (outList.Any(x => x.Key == key && x.Value.Message != entry.Message))
                                {
                                    throw new Exception(
                                        $"Message with key {key} has previous appearance but other fallback text in {entry.Description} in {tm.t.FullName}::{tm.m.FullName}");
                                }
                            }
                            else
                            {
                                debugOutput += $"    ->{key} - {entry.Message} (from {entry.Description})\n";
                                outList.Add(key, entry);
                            }
                        }
                    }
                }
            }

            File.WriteAllText("loc.log", debugOutput);
            File.WriteAllText($"{GetAssemblyName(assembly)}_Localizable.json", JsonConvert.SerializeObject(outList,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                }));

            return;
        }
        private static string? GetAssemblyName(Assembly assembly) => assembly.GetName().Name;
        public class LocEntry
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }
        }
#endif
    }
}
