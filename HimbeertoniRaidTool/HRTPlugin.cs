using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using HimbeertoniRaidTool.UI;
using System;
using System.Collections.Generic;
using XivCommon;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool
{

#pragma warning disable CS8618
    public class Services
    {
        [PluginService] public static SigScanner SigScanner { get; private set; }
        [PluginService] public static CommandManager CommandManager { get; private set; }
        [PluginService] public static ChatGui ChatGui { get; private set; }
        [PluginService] public static DataManager DataManager { get; private set; }
        [PluginService] public static GameGui GameGui { get; private set; }
        [PluginService] public static TargetManager TargetManager { get; private set; }
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public static ClientState ClientState { get; private set; }
        [PluginService] public static Framework Framework { get; private set; }
        public static XivCommonBase XivCommonBase { get; private set; } = new XivCommonBase();


    }
#pragma warning restore CS8618
    public sealed class HRTPlugin : IDalamudPlugin
    {
        private readonly Dalamud.Localization Loc;
        private static HRTPlugin? _Plugin;
        private static HRTPlugin Plugin => _Plugin ?? throw new NullReferenceException();
        private readonly Configuration _Configuration;
        public static Configuration Configuration => Plugin._Configuration;
        public string Name => "Himbeertoni Raid Tool";

        private readonly List<(string, Func<string>, bool)> Commands = new()
        {
            ("/hrt", () => Localize("/hrt", "Does nothing at the moment"), true),
            ("/lootmaster", () => Localize("/lootmaster", "Opens LootMaster Window"), false),
            ("/lm", () => Localize("/lm", "Opens LootMaster Window (short version)"), true),

        };

        private ConfigUI OptionsUi { get; init; }

        public HRTPlugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            //Init all services and public staic references
            _Plugin = this;
            pluginInterface.Create<Services>();
            FFXIVClientStructs.Resolver.Initialize(Services.SigScanner.SearchBase);
            //Init Localization
            Loc = new(Services.PluginInterface.AssemblyLocation.Directory + "\\locale");
            Loc.SetupWithLangCode(Services.PluginInterface.UiLanguage);
            Services.PluginInterface.LanguageChanged += OnLanguageChanged;
            InitCommands();
            //Load and update/correct configuration + ConfigUi
            _Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            _Configuration.AfterLoad();
            LootMaster.LootMaster.Init();
            OptionsUi = new();
        }
        private void OnLanguageChanged(string langCode)
        {
            Loc.SetupWithLangCode(langCode);
            Commands.ForEach(c => Services.CommandManager.RemoveHandler(c.Item1));
            InitCommands();
        }
        private void InitCommands()
        {
            foreach (var command in Commands)
            {
                Services.CommandManager.AddHandler(command.Item1, new CommandInfo(OnCommand)
                {
                    HelpMessage = command.Item2.Invoke(),
                    ShowInHelp = command.Item3
                });
            }
        }

        public void Dispose()
        {
            _Configuration.Save();
            OptionsUi.Dispose();
            Commands.ForEach(command => Services.CommandManager.RemoveHandler(command.Item1));
            Services.PluginInterface.LanguageChanged -= OnLanguageChanged;
            LootMaster.LootMaster.Dispose();
            Services.XivCommonBase.Dispose();
        }
        private void OnCommand(string command, string args)
        {
            switch (command)
            {
                case "/hrt":
                    if (args.Equals("exportlocale"))
                        Loc.ExportLocalizable();
                    else if (args.Contains("option"))
                        OptionsUi.Show();
                    else
                        PluginLog.LogError($"Argument {args} for command hrt not recognized");
                    break;
                case "/lm":
                case "/lootmaster":
                    LootMaster.LootMaster.OnCommand(args);
                    break;
                default:
                    PluginLog.LogError("Command \"" + command + "\" not found");
                    break;
            }
        }
    }
}
