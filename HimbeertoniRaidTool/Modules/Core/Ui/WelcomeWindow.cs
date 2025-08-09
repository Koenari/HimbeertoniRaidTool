using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

internal class WelcomeWindow : HrtWindow
{
    private const string WIKI_URL = "https://github.com/Koenari/HimbeertoniRaidTool/wiki";
    private readonly CoreModule _coreModule;
    public WelcomeWindow(CoreModule coreModule) : base(coreModule.Services.UiSystem)
    {
        _coreModule = coreModule;
        Persistent = true;
        IsOpen = false;
        (Size, SizeCondition) = (new Vector2(520, 345), ImGuiCond.Always);
        Title = CoreLoc.WelcomeUi_Title;
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
    }
    public override void Draw()
    {
        ImGui.TextWrapped(CoreLoc.WelcomeUi_text);
        ImGui.NewLine();
        //Buttons
        if (ImGuiHelper.Button(CoreLoc.WelcomeUi_btn_OpenLootMaster,
                               CoreLoc.WelcomeUi_btn_tt_OpenLootMaster))
        {
            _coreModule.OnCommand("/hrt", "lootmaster");
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLoc.WelcomeUi_btn_OpenOptions,
                               CoreLoc.WelcomeUi_btn_tt_OpenOptions))
        {
            _coreModule.OnCommand("/hrt", "config");
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLoc.Welcomeui_btn_openWiki,
                               CoreLoc.Welcomeui_btn_tt_openWiki))
        {
            Util.OpenLink(WIKI_URL);
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CoreLoc.WelcomeUi_btn_close,
                               CoreLoc.WelcomeUi_btn_tt_close))
            Hide();
    }
}