using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.Command;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.HrtServices;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster;
using HimbeertoniRaidTool.Plugin.Modules.WelcomeWindow;

namespace HimbeertoniRaidTool.Plugin
{
    public sealed class HRTPlugin : IDalamudPlugin
    {
        private readonly Configuration _Configuration;
        public string Name => "Himbeertoni Raid Tool";

        private readonly bool LoadError = false;

        private readonly List<string> RegisteredCommands = new();
        private readonly Dictionary<Type, IHrtModule> RegisteredModules = new();

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

                AddModule<LootMasterModule, LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>();
                AddModule<WelcomeWindowModule, WelcomeWindowConfig.ConfigData, IHrtConfigUi>();
            }
            else
            {
                pluginInterface.UiBuilder.AddNotification(Name + " did not load correctly. Please disbale/enable to try again", "Error in HRT", NotificationType.Error, 10000);
                Services.ChatGui.PrintError(Name + " did not load correctly. Please disbale/enable to try again");
            }
        }
        private bool TryGetModule<T>([NotNullWhen(true)] out T? module) where T : class, IHrtModule
        {
            module = null;
            if (RegisteredModules.TryGetValue(typeof(T), out IHrtModule? value))
            {
                module = (T)value;
                return true;
            }
            return false;
        }
        private void AddModule<T, S, Q>() where T : IHrtModule<S, Q>, new() where S : new() where Q : IHrtConfigUi
        {
            if (RegisteredModules.ContainsKey(typeof(T)))
            {
                PluginLog.Error($"Tried to register module \"{typeof(T)}\" twice");
                return;
            }
            T module = new();
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
            foreach ((Type type, IHrtModule module) in RegisteredModules)
            {
                try
                {
                    Services.PluginInterface.UiBuilder.Draw -= module.WindowSystem.Draw;
                    module.WindowSystem.RemoveAllWindows();
                    module.Dispose();
                }
                catch (Exception e)
                {
                    PluginLog.Fatal($"Unable to Dispose module \"{type}\"\n{e}");
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
                case string when args.IsNullOrEmpty(): if (TryGetModule(out WelcomeWindowModule? wcw)) wcw.Show(); break;
                default:
                    PluginLog.LogError($"Argument {args} for command \"/hrt\" not recognized");
                    break;
            }
        }
    }
}
