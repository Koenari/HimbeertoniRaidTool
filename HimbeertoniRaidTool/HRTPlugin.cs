using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using HimbeertoniRaidTool.HrtServices;
using HimbeertoniRaidTool.Modules;
using HimbeertoniRaidTool.Modules.LootMaster;
using HimbeertoniRaidTool.Modules.WelcomeWindow;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool
{
    public sealed class HRTPlugin : IDalamudPlugin
    {
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
            Localization.Init(pluginInterface);
            Services.Config = _Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            if (!LoadError)
            {
                //Load and update/correct configuration + ConfigUi
                _Configuration.AfterLoad();
                AddCommand(new HrtCommand()
                {
                    Command = "/hrt",
                    Description = Localization.Localize("/hrt", "Open Welcome Window with explanations"),
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
                Services.PluginInterface.UiBuilder.Draw += module.WindowSystem.Draw;
                module.AfterFullyLoaded();
            }
            catch (Exception e)
            {
                if (RegisteredModules.ContainsKey(typeof(T)))
                    RegisteredModules.Remove(typeof(T));
                PluginLog.Error("Error loading module: {0}\n {1}", module?.Name ?? string.Empty, e.ToString());
            }
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
                    WindowSystem w = moduleEntry.Value.WindowSystem;
                    Services.PluginInterface.UiBuilder.Draw -= w.Draw;
                    moduleEntry.Value.Dispose();
                }
                catch (Exception e)
                {
                    PluginLog.Fatal($"Unable to Dispose module \"{moduleEntry.Key}\"\n{e}");
                }
            }
            Localization.Dispose();
            _Configuration.Dispose();
            Services.Dispose();
        }
        private void OnCommand(string args)
        {
            switch (args)
            {
                case string a when a.Contains("option") || a.Contains("config"): _Configuration.Ui.Show(); break;
#if DEBUG
                case string b when b.Contains("exportlocale"): Localization.ExportLocalizable(); break;
#endif
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
