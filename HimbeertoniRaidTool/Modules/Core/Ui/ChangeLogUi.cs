using System.Diagnostics;
using System.Numerics;
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
        Title = Localization.Localize("window:changelog:title", "Himbeertoni Raid Tool Changelog");
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
            ImGui.Separator();
            ImGui.TextColored(Colors.TextWhite,
                $"{Localization.Localize("changelog:seen:header", "Previous changelogs")}:");
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
        float buttonWidth = ImGui.CalcTextSize(Localization.Localize("changelog:button:haveRead", "Yeah, I read it!"))
            .X + 20;
        ImGui.SetNextItemWidth(buttonWidth);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (Size.Value.X - buttonWidth) / 2);
        if (ImGuiHelper.Button(Localization.Localize("changelog:button:haveRead", "Yeah, I read it!"),
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
                $"{Localization.Localize("changelog:versionHeader", "Version")} {versionEntry.Version}",
                defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
        {

            foreach (ChangeLogEntry entry in versionEntry.NotableFeatures)
            {
                DrawLogEntry(entry, true);
            }
            foreach (ChangeLogEntry entry in versionEntry.MinorFeatures)
            {
                DrawLogEntry(entry);
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
                ServiceManager.TaskManager.RegisterTask(new HrtTask(() =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/Koenari/HimbeertoniRaidTool/issues/" + entry.GitHubIssueNumber,
                        UseShellExecute = true,
                    });
                    return new HrtUiMessage("");
                }, _ => { }, "Open Issue"));
            ImGuiHelper.AddTooltip(Localization.Localize("changelog:issueLink:tooltip", "Open on GitHub"));
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
        ChangeLogEntryCategory.General => Localization.Localize("changelog:category:general", "General"),
        ChangeLogEntryCategory.NewFeature => Localization.Localize("changelog:category:feature", "New Feature"),
        ChangeLogEntryCategory.Bugfix => Localization.Localize("changelog:category:bugfix", "Bugfix"),
        ChangeLogEntryCategory.Options => Localization.Localize("changelog:category:options", "Configuration"),
        ChangeLogEntryCategory.Ui => Localization.Localize("changelog:category:ui", "User Interface"),
        ChangeLogEntryCategory.Lootmaster => Localization.Localize("changelog:category:lootMaster", "Loot Master"),
        ChangeLogEntryCategory.LootSession => Localization.Localize("changelog:category:lootSession", "Loot Session"),
        ChangeLogEntryCategory.Bis => Localization.Localize("changelog:category:bis", "BiS"),
        ChangeLogEntryCategory.System => Localization.Localize("changelog:category:system", "System"),
        ChangeLogEntryCategory.Translation => Localization.Localize("changelog:category:translation", "Localization"),
        ChangeLogEntryCategory.Performance => Localization.Localize("changelog:category:performance", "Performance"),
        _ => Localization.Localize("changelog:category:unknown", "Unknown"),
    };
    public static string LocalizedDescription(this ChangelogShowOptions showOption) => showOption switch
    {

        ChangelogShowOptions.ShowAll => Localization.Localize("enum:ChangelogShowOptions:ShowAll:description",
            "Show me all changes"),
        ChangelogShowOptions.ShowNotable => Localization.Localize("enum:ChangelogShowOptions:ShowNotable:description",
            "Show me notable changes"),
        ChangelogShowOptions.ShowNone => Localization.Localize("enum:ChangelogShowOptions:ShowNone:description",
            "Do NOT show changes"),
        _ => Localization.Localize("enum:ChangelogShowOptions:unknown:description", "Unknown"),
    };
}
