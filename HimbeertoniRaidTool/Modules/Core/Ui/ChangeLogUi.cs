using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

internal class ChangeLogUi : HrtWindow
{
    private readonly ChangeLog _log;
    private ChangelogShowOptions _options;
    private readonly Vector2 _size = new(700, 500);
    public ChangeLogUi(IUiSystem uiSystem, ChangeLog log) : base(uiSystem, null,
                                                                 ImGuiWindowFlags.NoCollapse
                                                               | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Persistent = true;
        IsOpen = false;
        _log = log;
        OpenCentered = true;
        Size = _size;
        SizeCondition = ImGuiCond.Appearing;
        Title = CoreLoc.ChangeLogUi_Title;
        _options = _log.Config.ChangelogNotificationOptions;
        UiSystem.AddWindow(this);
    }
    public override void Draw()
    {
        if (ImGui.BeginChildFrame(1, _size with { Y = _size.Y - 100 }))
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var versionEntry in _log.UnseenChangeLogs)
            {
                DrawVersionEntry(versionEntry, true);
            }
            ImGui.NewLine();
            ImGui.Separator();
            ImGui.TextColored(Colors.TextWhite, CoreLoc.ChangeLogUi_hdg_seen);
            foreach (var versionEntry in _log.SeenChangeLogs)
            {
                DrawVersionEntry(versionEntry);
            }
        }
        ImGui.EndChildFrame();
        const int comboWidth = 200;
        ImGui.SetNextItemWidth(comboWidth * ScaleFactor);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (_size.X - comboWidth) / 2);
        InputHelper.Combo("##opt", ref _options, t => t.LocalizedDescription());
        float buttonWidth = ImGui.CalcTextSize(CoreLoc.ChangeLogUi_btn_haveRead).X + 20;
        ImGui.SetNextItemWidth(buttonWidth);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (_size.X - buttonWidth) / 2);
        if (ImGuiHelper.Button(CoreLoc.ChangeLogUi_btn_haveRead, null))
        {
            _log.Config.ChangelogNotificationOptions = _options;
            _log.Config.LastSeenChangelog = ChangeLog.CurrentVersion;
            Hide();
        }
    }
    private void DrawVersionEntry(SingleVersionChangelog versionEntry, bool defaultOpen = false)
    {
        using var id = ImRaii.PushId(versionEntry.Version.ToString());
        using var style = ImRaii.PushColor(ImGuiCol.Header, Colors.PetrolDark, versionEntry.HasNotableFeatures);
        if (!ImGui.CollapsingHeader(
                string.Format(CoreLoc.ChangeLogUi_hdg_version, versionEntry.Version),
                defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
            return;

        foreach (var entry in versionEntry.NotableFeatures)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
            DrawLogEntry(entry, true);
        }
        foreach (var entry in versionEntry.MinorFeatures)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
            DrawLogEntry(entry);
        }
        if (!versionEntry.HasKnownIssues)
            return;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f * ScaleFactor);
        ImGui.TextColored(Colors.TextSoftRed, CoreLoc.ChangeLogUi_hdg_KnownIssues);
        foreach (var entry in versionEntry.KnownIssues)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
            DrawLogEntry(entry);
        }
    }
    private void DrawLogEntry(ChangeLogEntry entry, bool important = false)
    {
        using var color = ImRaii.PushColor(ImGuiCol.Text, Colors.TextPetrol, important);
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped($"{entry.Category.Localized()}: {entry.Description}");
        if (entry.HasGitHubIssue)
        {
            ImGui.SameLine();
            ImGui.TextColored(Colors.TextLink,
                              string.Format(CoreLoc.ChangeLogUi_text_issueLink, entry.GitHubIssueNumber));
            if (ImGui.IsItemClicked())
                Util.OpenLink($"https://github.com/Koenari/HimbeertoniRaidTool/issues/{entry.GitHubIssueNumber}");
            ImGuiHelper.AddTooltip(CoreLoc.ChangeLogUi_text_issueLink_tooltip);
        }
        if (entry.NewSetting)
        {
            ImGui.SameLine();
            ImGui.TextColored(Colors.TextLink, CoreLoc.ChangeLogUi_text_openSettings);
            if (ImGui.IsItemClicked())
            {
                UiSystem.OpenSettingsWindow();
            }
        }
        foreach (string entryBulletPoint in entry.BulletPoints)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 15);
            ImGui.Bullet();
            ImGui.SameLine();
            ImGui.TextWrapped(entryBulletPoint);
        }
    }
}

public static class ChangelogEnumExtensions
{
    public static string Localized(this ChangeLogEntryCategory cat) => cat switch
    {
        ChangeLogEntryCategory.General       => CoreLoc.ChangelogCategory_General,
        ChangeLogEntryCategory.NewFeature    => CoreLoc.ChangelogCategory_NewFeature,
        ChangeLogEntryCategory.Bugfix        => CoreLoc.ChangelogCategory_Bugfix,
        ChangeLogEntryCategory.Options       => CoreLoc.ChangelogCategory_Configuration,
        ChangeLogEntryCategory.UserInterface => CoreLoc.ChangelogCategory_UserInterface,
        ChangeLogEntryCategory.Lootmaster    => CoreLoc.ChangelogCategory_LootMaster,
        ChangeLogEntryCategory.LootSession   => CoreLoc.ChangelogCategory_LootSession,
        ChangeLogEntryCategory.Bis           => CoreLoc.ChangelogCategory_BiS,
        ChangeLogEntryCategory.System        => CoreLoc.ChangelogCategory_System,
        ChangeLogEntryCategory.Translation   => CoreLoc.ChangelogCategory_Localization,
        ChangeLogEntryCategory.Performance   => CoreLoc.ChangelogCategory_Performance,
        ChangeLogEntryCategory.Gear          => CoreLoc.ChangelogCategory_Gear,
        ChangeLogEntryCategory.KnownIssues   => CoreLoc.ChangelogCategory_KnownIssues,
        ChangeLogEntryCategory.Lodestone     => CoreLoc.ChangelogCategory_Lodestone,
        ChangeLogEntryCategory.NewModule     => CoreLoc.ChangelogCategory_NewModule,
        _                                    => GeneralLoc.CommonTerms_Unknown,
    };
    public static string LocalizedDescription(this ChangelogShowOptions showOption) => showOption switch
    {

        ChangelogShowOptions.ShowAll     => CoreLoc.ChangelogShowOption_ShowAll,
        ChangelogShowOptions.ShowNotable => CoreLoc.ChangelogShowOption_ShowNotable,
        ChangelogShowOptions.ShowNone    => CoreLoc.ChangelogShowOption_ShowNone,
        _                                => GeneralLoc.CommonTerms_Unknown,
    };
}