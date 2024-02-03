using System.Numerics;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

internal class WelcomeWindow : HrtWindow
{
    private const string WIKI_URL = "https://github.com/Koenari/HimbeertoniRaidTool/wiki";
    private readonly CoreModule _coreModule;
    public WelcomeWindow(CoreModule coreModule)
    {
        _coreModule = coreModule;
        (Size, SizeCondition) = (new Vector2(520, 345), ImGuiCond.Always);
        Title = CoreLoc.WelcomeWindow_Title;
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
    }
    public override void Draw()
    {
        ImGui.TextWrapped(CoreLoc.WelcomeWindowLine1);
        ImGui.TextWrapped(CoreLoc.WelcomeWindowLine2);
        ImGui.TextWrapped(CoreLoc.WelcomeWindowLine3);
        ImGui.TextWrapped(CoreLoc.WelcomeWindowLine4);
        ImGui.TextWrapped(CoreLoc.WelcomeWindowLine5);
        ImGui.TextWrapped(CoreLoc.WelcomeWindowLine6);
        ImGui.NewLine();
        //Buttons
        if (ImGuiHelper.Button(CoreLoc.WelcomeWindow_button_OpenLootMaster,
                               CoreLoc.WelcomeWindow_button_OpenLootMaster_tooltip))
        {
            _coreModule.OnCommand("/hrt", "lootmaster");
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLoc.WelcomeWindow_button_OpenOptions,
                               CoreLoc.WelcomeWindow_button_OpenOptions_tooltip))
        {
            _coreModule.OnCommand("/hrt", "config");
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLoc.WelcomeWindow_button_openWiki,
                               CoreLoc.WelcomeWindow_button_openWiki_tooltip))
        {
            Util.OpenLink(WIKI_URL);
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLoc.WelcomeWindow_button_Close,
                               CoreLoc.WelcomeWindow_button_Close_tooltip))
            Hide();
    }
}