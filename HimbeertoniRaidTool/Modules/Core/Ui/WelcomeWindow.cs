using System.Diagnostics;
using System.Numerics;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

internal class WelcomeWindow : HrtWindow
{
    private const string WIKI_URL = "https://github.com/Koenari/HimbeertoniRaidTool/wiki";
    public WelcomeWindow() : base()
    {
        (Size, SizeCondition) = (new Vector2(520, 345), ImGuiCond.Always);
        Title = Localize("Welcome to HRT", "Welcome to Himbeertoni Raid Tool");
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
    }
    public override void Draw()
    {
        ImGui.TextWrapped(Localize("WelcomeWindowLine1", "Welcome to Himbeertoni Raid Tool. Your companion in managing your raid group."));
        ImGui.TextWrapped(Localize("WelcomeWindowLine2", "Start your journey by opening LootMaster by typing \"/lootmaster\" (or \"/lm\") in chat (or the button below). There we already added your character for you."));
        ImGui.TextWrapped(Localize("WelcomeWindowLine3", $"Next you can get your current gear either by using the \"magnifying glass\" button or by examining your character via right clicking."));
        ImGui.TextWrapped(Localize("WelcomeWindowLine4", "The plugin will always update the gear for characters that were added to a group or solo tab when examining the character in-game."));
        ImGui.TextWrapped(Localize("WelcomeWindowLine5", "To really start using this you'd need to add your group via the \"+ button\" right to the Solo tab. For this you have two possibilities." +
                                                         " \"From scratch\" let's you input everything yourself like a noob. Or you can gather your group into a party (or wait for the next gathering) and let the plugin do most of the work by"
                                                         +
                                                         " choosing \"From current group\". You still have to give the group a name and maybe adjust nicknames for your players."));
        ImGui.TextWrapped(Localize("WelcomeWindowLine6", "If you for example want the loot master to open on start, I would suggest you take a quick look at the options. And go to the wiki for more detailed instructions :)"));
        ImGui.NewLine();
        //Buttons
        if (ImGuiHelper.Button(Localize("Open LootMaster", "Open LootMaster"),
                Localize("Open LootMaster main window (/lootmaster)", "Open LootMaster main window (/lootmaster)")))
        {
            ServiceManager.CoreModule.OnCommand("/hrt", "lootmaster");
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(Localize("Open Options", "Open Options"),
                Localize("Show configuration options (/hrt config)", "Show configuration options (/hrt config)")))
        {
            ServiceManager.CoreModule.OnCommand("/hrt", "config");

        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(Localize("Open Wiki", "Open Wiki"),
                Localize("Open the wiki in your browser", "Open the wiki in your browser")))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = WIKI_URL,
                UseShellExecute = true,

            });
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(Localize("Close", "Close"), Localize("Close this window", "Close this window")))
            Hide();
    }
}