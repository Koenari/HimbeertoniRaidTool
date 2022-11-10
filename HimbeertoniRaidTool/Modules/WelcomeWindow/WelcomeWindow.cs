using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Logging;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.Modules.WelcomeWindow
{
    internal class WelcomeWindowModule : IHrtModule<WelcomeWindowConfig.ConfigData, IHrtConfigUi>
    {
        public string Name => "Welcome Window";
        public string Description => "Shows a welcome window with information on how to use";
        public IEnumerable<HrtCommand> Commands => Array.Empty<HrtCommand>();

        public string InternalName => "WelcomeWindow";

        public HRTConfiguration<WelcomeWindowConfig.ConfigData, IHrtConfigUi> Configuration => _config;

        public Dalamud.Interface.Windowing.WindowSystem WindowSystem { get; }

        private readonly WelcomeWindowui _ui;
        private readonly WelcomeWindowConfig _config;
        public WelcomeWindowModule()
        {
            WindowSystem = new(InternalName);
            _ui = new(this);
            WindowSystem.AddWindow(_ui);
            _config = new WelcomeWindowConfig(this);
        }
        public void Update(Framework fw) { }
        public void Dispose()
        {
            _ui?.Dispose();
        }

        public void HandleMessage(HrtUiMessage message)
        {
            if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
                PluginLog.Warning(message.Message);
            else
                PluginLog.Information(message.Message);
        }

        internal void Show()
        {
            _ui.Show();
        }

        public void AfterFullyLoaded()
        {
            if (_config.Data.ShowWelcomeWindow)
                Show();
        }

        private class WelcomeWindowui : HrtWindow
        {
            private const string WikiURL = "https://github.com/Koenari/HimbeertoniRaidTool/wiki";
            private readonly WelcomeWindowModule _parent;
            public WelcomeWindowui(WelcomeWindowModule parent) : base()
            {
                (Size, SizeCondition) = (new Vector2(520, 345), ImGuiCond.Always);
                Title = Localize("Welcome to HRT", "Welcome to Himbeertoni Raid Tool");
                Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
                _parent = parent;
            }
            public override void Draw()
            {
                ImGui.TextWrapped(Localize("WelcomeWindowLine1", "Welcome to Himbeertoni Raid Tool. Your companion in managing your raid group."));
                ImGui.TextWrapped(Localize("WelcomeWindowLine2", "Start your journey by opening LootMaster by typing \"/lootmaster\" (or \"/lm\") in chat (or the button below). There we already added your character for you."));
                ImGui.TextWrapped(Localize("WelcomeWindowLine3", $"Next you can get your current gear either by using the \"magnifying glass\" button or by examining your character via right clicking."));
                ImGui.TextWrapped(Localize("WelcomeWindowLine4", "The plugin will always update the gear for characters that were added to a group or solo tab when examining the character in-game."));
                ImGui.TextWrapped(Localize("WelcomeWindowLine5", "To really start using this you'd need to add your group via the \"+ button\" right to the Solo tab. For this you have two possibilities." +
                    " \"From scratch\" let's you input everything yourself like a noob. Or you can gather your group into a party (or wait for the next gathering) and let the plugin do most of the work by" +
                    " choosing \"From current group\". You still have to give the group a name and maybe adjust nicknames for your players."));
                ImGui.TextWrapped(Localize("WelcomeWindowLine6", "If you for example want the loot master to open on start, I would suggest you take a quick look at the options. And go to the wiki for more detailed instructions :)"));
                ImGui.NewLine();
                //Buttons
                if (ImGuiHelper.Button(Localize("Open LootMaster", "Open LootMaster"),
                    Localize("Open LootMaster main window (/lootmaster)", "Open LootMaster main window (/lootmaster)")))
                {
                    Services.CommandManager.ProcessCommand("/lootmaster");
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(Localize("Open Options", "Open Options"),
                     Localize("Show configuration options (/hrt config)", "Show configuration options (/hrt config)")))
                {
                    Services.CommandManager.ProcessCommand("/hrt config");

                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(Localize("Open Wiki", "Open Wiki"),
                    Localize("Open the wiki in your browser", "Open the wiki in your browser")))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = WikiURL,
                        UseShellExecute = true,

                    });
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(Localize("Close", "Close"), Localize("Close this window", "Close this window")))
                    Hide();
            }
            public override void OnClose()
            {
                _parent.Configuration.Data.ShowWelcomeWindow = false;
                _parent.Configuration.Save();
            }
        }
    }

    internal sealed class WelcomeWindowConfig : HRTConfiguration<WelcomeWindowConfig.ConfigData, IHrtConfigUi>
    {
        public override IHrtConfigUi? Ui => null;

        public WelcomeWindowConfig(WelcomeWindowModule hrtModule) : base(hrtModule.InternalName, hrtModule.Name)
        {
        }
        public override void AfterLoad() { }

        internal sealed class ConfigData
        {
            [JsonProperty]
            internal bool ShowWelcomeWindow = true;
            public ConfigData() { }
        }
    }
}
