using System.Numerics;
using Dalamud.Utility;
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
        Title = Services.Localization.Localize("window:changelog:title", "Himbeertoni Raid Tool Changelog");
        _options = _log.Config.Data.ChangelogNotificationOptions;
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
            ImGui.TextColored(Colors.TextWhite,
                $"{Services.Localization.Localize("changelog:seen:header", "Previous changelogs")}:");
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
        float buttonWidth = ImGui.CalcTextSize(Services.Localization.Localize("changelog:button:haveRead", "Yeah, I read it!"))
            .X + 20;
        ImGui.SetNextItemWidth(buttonWidth);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (Size.Value.X - buttonWidth) / 2);
        if (ImGuiHelper.Button(Services.Localization.Localize("changelog:button:haveRead", "Yeah, I read it!"),
                null))
        {
            _log.Config.Data.ChangelogNotificationOptions = _options;
            _log.Config.Data.LastSeenChangelog = _log.CurrentVersion;
            Hide();
        }
    }
    private static void DrawVersionEntry(SingleVersionChangelog versionEntry, bool defaultOpen = false)
    {
        ImGui.PushID(versionEntry.Version.ToString());
        if (versionEntry.HasNotableFeatures)
            ImGui.PushStyleColor(ImGuiCol.Header, Colors.PetrolDark);
        if (ImGui.CollapsingHeader(
                $"{Services.Localization.Localize("changelog:versionHeader", "Version")} {versionEntry.Version}",
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
                ImGui.TextColored(Colors.TextSoftRed, Services.Localization.Localize("changelog:knownIssues", "Known Issues"));
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
            ImGui.TextColored(Colors.TextLink, $"Fixes issue #{entry.GitHubIssueNumber}");
            if (ImGui.IsItemClicked())
                Util.OpenLink($"https://github.com/Koenari/HimbeertoniRaidTool/issues/{entry.GitHubIssueNumber}");
            ImGuiHelper.AddTooltip(Services.Localization.Localize("changelog:issueLink:tooltip", "Open on GitHub"));
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

internal static class ChangelogEnumExtensions
{
    internal static string Localized(this ChangeLogEntryCategory cat) => cat switch
    {
        ChangeLogEntryCategory.General => Services.Localization.Localize("changelog:category:general", "General"),
        ChangeLogEntryCategory.NewFeature => Services.Localization.Localize("changelog:category:feature", "New Feature"),
        ChangeLogEntryCategory.Bugfix => Services.Localization.Localize("changelog:category:bugfix", "Bugfix"),
        ChangeLogEntryCategory.Options => Services.Localization.Localize("changelog:category:options", "Configuration"),
        ChangeLogEntryCategory.Ui => Services.Localization.Localize("changelog:category:ui", "User Interface"),
        ChangeLogEntryCategory.Lootmaster => Services.Localization.Localize("changelog:category:lootMaster", "Loot Master"),
        ChangeLogEntryCategory.LootSession => Services.Localization.Localize("changelog:category:lootSession", "Loot Session"),
        ChangeLogEntryCategory.Bis => Services.Localization.Localize("changelog:category:bis", "BiS"),
        ChangeLogEntryCategory.System => Services.Localization.Localize("changelog:category:system", "System"),
        ChangeLogEntryCategory.Translation => Services.Localization.Localize("changelog:category:translation", "Localization"),
        ChangeLogEntryCategory.Performance => Services.Localization.Localize("changelog:category:performance", "Performance"),
        ChangeLogEntryCategory.Gear => Services.Localization.Localize("changelog:category:gear", "Gear"),
        _ => Services.Localization.Localize("changelog:category:unknown", "Unknown"),
    };
    public static string LocalizedDescription(this ChangelogShowOptions showOption) => showOption switch
    {

        ChangelogShowOptions.ShowAll => Services.Localization.Localize("enum:ChangelogShowOptions:ShowAll:description",
            "Show me all changes"),
        ChangelogShowOptions.ShowNotable => Services.Localization.Localize("enum:ChangelogShowOptions:ShowNotable:description",
            "Show me notable changes"),
        ChangelogShowOptions.ShowNone => Services.Localization.Localize("enum:ChangelogShowOptions:ShowNone:description",
            "Do NOT show changes"),
        _ => Services.Localization.Localize("enum:ChangelogShowOptions:unknown:description", "Unknown"),
    };
}