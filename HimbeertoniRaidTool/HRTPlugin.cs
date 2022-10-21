using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Game.Command;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using HimbeertoniRaidTool.Modules;
using HimbeertoniRaidTool.Modules.LootMaster;
using HimbeertoniRaidTool.Modules.WelcomeWindow;
using Newtonsoft.Json;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool
{
    public sealed class HRTPlugin : IDalamudPlugin
    {
        private readonly Dalamud.Localization Loc;

        private readonly Configuration _Configuration;
        public string Name => "Himbeertoni Raid Tool";

        private readonly bool LoadError = false;

        private readonly List<string> RegisteredCommands = new();
        private readonly Dictionary<Type, dynamic> RegisteredModules = new();

        public HRTPlugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            //Init all services
            LoadError = !Services.Init(pluginInterface);
            //Init Localization
            string localePath = Path.Combine(Services.PluginInterface.AssemblyLocation.DirectoryName!, @"locale");
            Loc = new(localePath, "HimbeertoniRaidTool_");
            Loc.SetupWithLangCode(Services.PluginInterface.UiLanguage);
            Services.PluginInterface.LanguageChanged += OnLanguageChanged;
            Services.Config = _Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            if (!LoadError)
            {
                //Load and update/correct configuration + ConfigUi
                _Configuration.AfterLoad();
                AddCommand(new HrtCommand()
                {
                    Command = "/hrt",
                    Description = Localize("/hrt", "Open Welcome Window with explanations"),
                    ShowInHelp = true,
                    OnCommand = OnCommand
                });
                //TODO: Some more elegant way to load modules

                AddModule<LootMasterModule, LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>(new());
                AddModule<WelcomeWindowModule, WelcomeWindowConfig.ConfigData, IHrtConfigUi>(new());
            }
            else
            {
                pluginInterface.UiBuilder.AddNotification(Name + " did not load correctly. Please disbale/enable to try again", "Error in HRT", NotificationType.Error, 10000);
                Services.ChatGui.PrintError(Name + " did not load correctly. Please disbale/enable to try again");
            }
        }
        private T? GetModule<T, S, Q>() where T : IHrtModule<S, Q> where S : new() where Q : IHrtConfigUi
        {
            RegisteredModules.TryGetValue(typeof(T), out dynamic? value);
            return (T?)value;
        }
        private void AddModule<T, S, Q>(T module) where T : IHrtModule<S, Q> where S : new() where Q : IHrtConfigUi
        {
            if (RegisteredModules.ContainsKey(typeof(T)))
            {
                PluginLog.Error($"Tried to register module \"{module.Name}\" twice");
                return;
            }
            try
            {
                RegisteredModules.Add(typeof(T), module);
                foreach (HrtCommand command in module.Commands)
                    AddCommand(command);
                if (!_Configuration.RegisterConfig(module.Configuration))
                    PluginLog.Error($"Configuration load error:{module.Name}");
                module.AfterFullyLoaded();
            }
            catch (Exception e)
            {
                if (RegisteredModules.ContainsKey(typeof(T)))
                    RegisteredModules.Remove(typeof(T));
                PluginLog.Error("Error loading module: {0}\n {1}", module?.Name ?? string.Empty, e.ToString());
            }
        }
        private void OnLanguageChanged(string langCode)
        {
            PluginLog.Information($"LOading localization for {langCode}");
            Loc.SetupWithLangCode(langCode);
        }
        private void AddCommand(HrtCommand command)
        {
            if (Services.CommandManager.AddHandler(command.Command,
                new CommandInfo((x, y) => command.OnCommand(y))
                {
                    HelpMessage = command.Description,
                    ShowInHelp = command.ShowInHelp
                }))
            { RegisteredCommands.Add(command.Command); }
        }
        public void Dispose()
        {
            Services.PluginInterface.LanguageChanged -= OnLanguageChanged;
            RegisteredCommands.ForEach(command => Services.CommandManager.RemoveHandler(command));
            if (!LoadError)
            {
                _Configuration.Save(false);
                Services.HrtDataManager.Save();
            }
            foreach (var moduleEntry in RegisteredModules)
            {
                try
                {
                    moduleEntry.Value.Dispose();
                }
                catch (Exception e)
                {
                    PluginLog.Fatal($"Unable to Dispose module \"{moduleEntry.Key}\"\n{e}");
                }
            }
            _Configuration.Dispose();
            Services.Dispose();
        }
        private void OnCommand(string args)
        {
            switch (args)
            {
                case string a when a.Contains("option") || a.Contains("config"): _Configuration.Ui.Show(); break;
                case string b when b.Contains("exportlocale"): Loc.ExportLocalizable(); break;
                case string when args.IsNullOrEmpty(): GetModule<WelcomeWindowModule, WelcomeWindowConfig.ConfigData, IHrtConfigUi>()?.Show(); break;
                default:
                    PluginLog.LogError($"Argument {args} for command \"/hrt\" not recognized");
                    break;
            }
        }
    }
    public static class HRTExtensions
    {
        public static T Clone<T>(this T source)
            => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source))!;
        public static int ConsistentHash(this string obj)
        {
            SHA512 alg = SHA512.Create();
            byte[] sha = alg.ComputeHash(Encoding.UTF8.GetBytes(obj));
            return sha[0] + 256 * sha[1] + 256 * 256 * sha[2] + 256 * 256 * 256 * sha[2];
        }
    }
}
