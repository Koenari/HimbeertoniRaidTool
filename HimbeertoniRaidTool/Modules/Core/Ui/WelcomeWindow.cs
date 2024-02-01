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
        Title = CoreLocalization.WelcomeWindow_Title;
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
    }
    public override void Draw()
    {
        ImGui.TextWrapped(CoreLocalization.WelcomeWindowLine1);
        ImGui.TextWrapped(CoreLocalization.WelcomeWindowLine2);
        ImGui.TextWrapped(CoreLocalization.WelcomeWindowLine3);
        ImGui.TextWrapped(CoreLocalization.WelcomeWindowLine4);
        ImGui.TextWrapped(CoreLocalization.WelcomeWindowLine5);
        ImGui.TextWrapped(CoreLocalization.WelcomeWindowLine6);
        ImGui.NewLine();
        //Buttons
        if (ImGuiHelper.Button(CoreLocalization.WelcomeWindow_button_OpenLootMaster,
                               CoreLocalization.WelcomeWindow_button_OpenLootMaster_tooltip))
        {
            _coreModule.OnCommand("/hrt", "lootmaster");
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLocalization.WelcomeWindow_button_OpenOptions,
                               CoreLocalization.WelcomeWindow_button_OpenOptions_tooltip))
        {
            _coreModule.OnCommand("/hrt", "config");
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLocalization.WelcomeWindow_button_openWiki,
                               CoreLocalization.WelcomeWindow_button_openWiki_tooltip))
        {
            Util.OpenLink(WIKI_URL);
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLocalization.WelcomeWindow_button_Close,
                               CoreLocalization.WelcomeWindow_button_Close_tooltip))
            Hide();
    }
}