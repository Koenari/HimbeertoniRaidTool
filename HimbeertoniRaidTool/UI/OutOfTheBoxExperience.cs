using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public class OutOfTheBoxExperience : HrtWindow
{
    private const string WIKI_URL = "https://github.com/Koenari/HimbeertoniRaidTool/wiki";
    private readonly IClientState _clientState;
    private readonly ConfigurationManager _configurationManager;
    public OutOfTheBoxExperience(IUiSystem uiSystem, ConfigurationManager configurationManager,
                                 IClientState clientState) : base(uiSystem)
    {
        _clientState = clientState;
        _configurationManager = configurationManager;
        Persistent = true;
        IsOpen = false;
        (Size, SizeCondition) = (new Vector2(520, 345), ImGuiCond.Always);
        Title = CoreLoc.WelcomeUi_Title;
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
        if (!configurationManager.CoreConfig.Data.ShowWelcomeWindow) return;
        if (_clientState.IsLoggedIn)
        {
            ShowOnLoginEvent();
        }
        else
        {
            _clientState.Login += ShowOnLoginEvent;
        }

    }
    private void ShowOnLoginEvent()
    {
        _clientState.Login -= ShowOnLoginEvent;
        _configurationManager.CoreConfig.Data.ShowWelcomeWindow = false;
        Show();
    }

    public override void Draw()
    {
        ImGui.TextWrapped(CoreLoc.WelcomeUi_text);
        ImGui.NewLine();
        //Buttons
        if (ImGuiHelper.Button(CoreLoc.WelcomeUi_btn_OpenOptions,
                               CoreLoc.WelcomeUi_btn_tt_OpenOptions))
        {
            UiSystem.OpenSettingsWindow();
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