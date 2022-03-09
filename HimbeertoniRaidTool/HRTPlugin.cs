using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using System;
using System.Collections.Generic;


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
    }
    public class Localization
    {
        public static Dalamud.Localization ParentLoc { get; private set; }
        public static void Init(Dalamud.Localization parent) => ParentLoc = parent;

        public static string Localize(string key, string fallBack) => Dalamud.Localization.Localize(key, fallBack);
        public static string Localize(string format, string fallBack, params object?[] args) => string.Format(Dalamud.Localization.Localize(format, format), fallBack);

    }
#pragma warning restore CS8618
    public sealed class HRTPlugin : IDalamudPlugin
    {

        private static HRTPlugin? _Plugin;
        private static HRTPlugin Plugin => _Plugin ?? throw new NullReferenceException();
        private readonly Configuration _Configuration;
        public static Configuration Configuration => Plugin._Configuration;
        public string Name => "Himbeertoni Raid Tool";

        private readonly List<(string, string, bool)> Commands = new()
        {
            ("/hrt", "Does nothing at the moment", true),
            ("/lootmaster", "Opens LootMaster Window", false),
            ("/lm", "Opens LootMaster Window (short version)", true),
#if DEBUG
            ("/hrtexport", "Exports tranlation", true),
#endif

        };

        private ConfigUI OptionsUi { get; init; }
        private LootMaster.LootMaster LM { get; init; }

        public HRTPlugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            //Init all services and public staic references
            _Plugin = this;
            pluginInterface.Create<Services>();
            FFXIVClientStructs.Resolver.Initialize(Services.SigScanner.SearchBase);
            InitCommands();
            //Load and update/correct configuration + ConfigUi
            _Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            _Configuration.AfterLoad();
            OptionsUi = new();
            //Init Localization
            Localization.Init(new(Services.PluginInterface.AssemblyLocation.Directory + "\\locale"));
            Localization.ParentLoc.SetupWithLangCode(Services.PluginInterface.UiLanguage);
            Services.PluginInterface.LanguageChanged += Localization.ParentLoc.SetupWithLangCode;


            LM = new(_Configuration.GroupInfo ?? new RaidGroup(""));
        }

        private void InitCommands()
        {
            foreach (var command in Commands)
            {
                Services.CommandManager.AddHandler(command.Item1, new CommandInfo(OnCommand)
                {
                    HelpMessage = command.Item2,
                    ShowInHelp = command.Item3
                });
            }
        }

        public void Dispose()
        {
            _Configuration.Save();
            OptionsUi.Dispose();
            LM.Dispose();
            foreach (var command in Commands)
            {
                Services.CommandManager.RemoveHandler(command.Item1);
            }
            Services.PluginInterface.LanguageChanged -= Localization.ParentLoc.SetupWithLangCode;
        }
        private void OnCommand(string command, string args)
        {
            switch (command)
            {
                case "/hrt":
                    this.OptionsUi.Show();
                    break;
                case "/lm":
                case "/lootmaster":
                    this.LM.OnCommand(args);
                    break;
#if DEBUG
                case "/hrtexport":
                    Localization.ParentLoc.ExportLocalizable();
                    break;
#endif
                default:
                    PluginLog.LogError("Command \"" + command + "\" not found");
                    break;
            }
        }
    }
}
