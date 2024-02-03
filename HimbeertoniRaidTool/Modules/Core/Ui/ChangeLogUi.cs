using System.Numerics;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

internal class ChangeLogUi : HrtWindow
{
    private readonly ChangeLog _log;
    private ChangelogShowOptions _options;
    public ChangeLogUi(ChangeLog log) : base(null, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)
    {
        _log = log;
        Size = new Vector2(600, 500);
        SizeCondition = ImGuiCond.Appearing;
        OpenCentered = true;
        Title = GeneralLoc.ChangeLogUi_Title;
        _options = _log.Config.ChangelogNotificationOptions;
    }
    public override void Draw()
    {
        if (ImGui.BeginChildFrame(1, Size!.Value with { Y = Size.Value.Y - 100 }))
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (SingleVersionChangelog versionEntry in _log.UnseenChangeLogs)
            {
                DrawVersionEntry(versionEntry, true);
            }
            ImGui.NewLine();
            ImGui.Separator();
            ImGui.TextColored(Colors.TextWhite, GeneralLoc.ChangeLogUi_heading_seen);
            foreach (SingleVersionChangelog versionEntry in _log.SeenChangeLogs)
            {
                DrawVersionEntry(versionEntry);
            }
            ImGui.EndChildFrame();
        }
        const int comboWidth = 200;
        ImGui.SetNextItemWidth(comboWidth * ScaleFactor);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (Size.Value.X - comboWidth) / 2);
        ImGuiHelper.Combo("##opt", ref _options, t => t.LocalizedDescription());
        float buttonWidth = ImGui.CalcTextSize(CoreLocalization.ChangeLogUi_button_read).X + 20;
        ImGui.SetNextItemWidth(buttonWidth);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (Size.Value.X - buttonWidth) / 2);
        if (ImGuiHelper.Button(CoreLocalization.ChangeLogUi_button_read, null))
        {
            _log.Config.ChangelogNotificationOptions = _options;
            _log.Config.LastSeenChangelog = _log.CurrentVersion;
            Hide();
        }
    }
    private static void DrawVersionEntry(SingleVersionChangelog versionEntry, bool defaultOpen = false)
    {
        ImGui.PushID(versionEntry.Version.ToString());
        if (versionEntry.HasNotableFeatures)
            ImGui.PushStyleColor(ImGuiCol.Header, Colors.PetrolDark);
        if (ImGui.CollapsingHeader(
                string.Format(CoreLocalization.ChangeLogUi_heading_version, versionEntry.Version),
                defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
        {
            foreach (ChangeLogEntry entry in versionEntry.NotableFeatures)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
                DrawLogEntry(entry, true);
            }
            foreach (ChangeLogEntry entry in versionEntry.MinorFeatures)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
                DrawLogEntry(entry);
            }
            if (versionEntry.HasKnownIssues)
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f * ScaleFactor);
                ImGui.TextColored(Colors.TextSoftRed, CoreLocalization.ChangeLogUi_heading_KnownIssues);
                foreach (ChangeLogEntry entry in versionEntry.KnownIssues)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
                    DrawLogEntry(entry);
                }
            }
        }
        if (versionEntry.HasNotableFeatures)
            ImGui.PopStyleColor();
        ImGui.PopID();
    }
    private static void DrawLogEntry(ChangeLogEntry entry, bool important = false)
    {
        Action<string> drawText = important ? s => ImGui.TextColored(Colors.TextPetrol, s) : ImGui.Text;
        ImGui.Bullet();
        ImGui.SameLine();
        drawText($"{entry.Category.Localized()}: {entry.Description}");
        if (entry.HasGitHubIssue)
        {
            ImGui.SameLine();
            ImGui.TextColored(Colors.TextLink,
                              string.Format(CoreLocalization.ChangeLogUi_text_issueLink, entry.GitHubIssueNumber));
            if (ImGui.IsItemClicked())
                Util.OpenLink($"https://github.com/Koenari/HimbeertoniRaidTool/issues/{entry.GitHubIssueNumber}");
            ImGuiHelper.AddTooltip(CoreLocalization.ChangeLogUi_text_issueLink_tooltip);
        }
        foreach (string entryBulletPoint in entry.BulletPoints)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 15);
            ImGui.Bullet();
            ImGui.SameLine();
            drawText(entryBulletPoint);
        }
    }
}

public static class ChangelogEnumExtensions
{
    public static string Localized(this ChangeLogEntryCategory cat) => cat switch
    {
        ChangeLogEntryCategory.General     => CoreLocalization.ChangelogCategory_General,
        ChangeLogEntryCategory.NewFeature  => CoreLocalization.ChangelogCategory_NewFeature,
        ChangeLogEntryCategory.Bugfix      => CoreLocalization.ChangelogCategory_Bugfix,
        ChangeLogEntryCategory.Options     => CoreLocalization.ChangelogCategory_Configuration,
        ChangeLogEntryCategory.Ui          => CoreLocalization.ChangelogCategory_UserInterface,
        ChangeLogEntryCategory.Lootmaster  => CoreLocalization.ChangelogCategory_LootMaster,
        ChangeLogEntryCategory.LootSession => CoreLocalization.ChangelogCategory_LootSession,
        ChangeLogEntryCategory.Bis         => CoreLocalization.ChangelogCategory_BiS,
        ChangeLogEntryCategory.System      => CoreLocalization.ChangelogCategory_System,
        ChangeLogEntryCategory.Translation => CoreLocalization.ChangelogCategory_Localization,
        ChangeLogEntryCategory.Performance => CoreLocalization.ChangelogCategory_Performance,
        ChangeLogEntryCategory.Gear        => CoreLocalization.ChangelogCategory_Gear,
        _                                  => GeneralLoc.Unknown,
    };
    public static string LocalizedDescription(this ChangelogShowOptions showOption) => showOption switch
    {

        ChangelogShowOptions.ShowAll     => CoreLocalization.ChangelogOption_LocalizedDescription_ShowAll,
        ChangelogShowOptions.ShowNotable => CoreLocalization.ChangelogOption_LocalizedDescription_ShowNotable,
        ChangelogShowOptions.ShowNone    => CoreLocalization.ChangelogOption_LocalizedDescription_ShowNone,
        _                                => GeneralLoc.Unknown,
    };
}