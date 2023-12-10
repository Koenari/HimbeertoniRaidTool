using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

internal class ChangeLogUi : HrtWindow
{
    private readonly ChangeLog _log;
    public ChangeLogUi(ChangeLog log) : base(null,
        ImGuiWindowFlags.NoCollapse)
    {
        _log = log;
        Size = new Vector2(500, 300);
        SizeCondition = ImGuiCond.Appearing;
        OpenCentered = true;
        Title = Localization.Localize("window:changelog:title", "Changelog");

    }
    public override void Draw()
    {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (SingleVersionChangelog versionEntry in _log.UnseenChangeLogs)
        {
            DrawVersionEntry(versionEntry);
        }
        ImGui.Separator();
        foreach (SingleVersionChangelog versionEntry in _log.SeenChangeLogs)
        {
            DrawVersionEntry(versionEntry);
        }
    }
    private static void DrawVersionEntry(SingleVersionChangelog versionEntry)
    {
        ImGui.PushID(versionEntry.Version.ToString());
        if (versionEntry.HasNotableFeatures)
            ImGui.PushStyleColor(ImGuiCol.Header, Colors.RedWood);
        if (ImGui.CollapsingHeader(
                $"{Localization.Localize("changelog:versionHeader", "Version")} {versionEntry.Version}"))
        {
            ImGui.Text(Localization.Localize("changelog:major:header", "Notable Changes"));
            if (versionEntry.HasNotableFeatures)
            {
                foreach (ChangeLogEntry entry in versionEntry.NotableFeatures)
                {
                    DrawLogEntry(entry);
                }
            }
            else
            {
                ImGui.Text(Localization.Localize("None", "None"));
            }
            if (versionEntry.HasMinorFeatures)
            {
                ImGui.Separator();
                ImGui.Text(Localization.Localize("changelog:minor:header", "Minor Changes"));
                foreach (ChangeLogEntry entry in versionEntry.MinorFeatures)
                {
                    DrawLogEntry(entry);
                }
            }
        }
        if (versionEntry.HasNotableFeatures)
            ImGui.PopStyleColor();
        ImGui.PopID();
    }
    private static void DrawLogEntry(ChangeLogEntry entry)
    {
        ImGui.Text($"{entry.Category.Localized()}: {entry.Description}");
        foreach (string entryBulletPoint in entry.BulletPoints)
        {
            ImGui.BulletText(entryBulletPoint);
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
        _ => Localization.Localize("changelog:category:unknown", "Unknown"),
    };
}