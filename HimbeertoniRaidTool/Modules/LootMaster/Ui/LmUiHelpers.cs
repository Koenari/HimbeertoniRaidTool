using System.Numerics;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

[Flags]
public enum SlotDrawFlags
{
    None = 0,
    SingleItem = 1,
    ItemCompare = 2,
    SimpleView = 4,
    ExtendedView = 8,
    Default = ItemCompare | SimpleView,
    DetailedSingle = SingleItem | ExtendedView,
}

internal static class LmUiHelpers
{

    private static Vector4 LevelColor(LootMasterConfiguration.ConfigData config, GearItem item) =>
        (config.SelectedRaidTier.ItemLevel(item.Slots.FirstOrDefault(GearSetSlot.None)) - (int)item.ItemLevel) switch
        {
            <= 0  => config.ItemLevelColors[0],
            <= 10 => config.ItemLevelColors[1],
            <= 20 => config.ItemLevelColors[2],
            _     => config.ItemLevelColors[3],
        };

    internal static void DrawSlot(LootMasterModule module, GearItem item,
                                  SlotDrawFlags style = SlotDrawFlags.SingleItem | SlotDrawFlags.SimpleView) =>
        DrawSlot(module, (item, GearItem.Empty), style);
    internal static void DrawSlot(LootMasterModule module, (GearItem, GearItem) itemTuple,
                                  SlotDrawFlags style = SlotDrawFlags.Default)
    {
        float originalY = ImGui.GetCursorPosY();
        float fullLineHeight = ImGui.GetTextLineHeightWithSpacing();
        float lineSpacing = fullLineHeight - ImGui.GetTextLineHeight();
        float cursorDualTopY = originalY + lineSpacing * 2f;
        float cursorDualBottomY = cursorDualTopY + fullLineHeight * 1.7f;
        float cursorSingleSmall = originalY + fullLineHeight + lineSpacing;
        float cursorSingleLarge = originalY + fullLineHeight * 0.7f + lineSpacing;
        bool extended = style.HasFlag(SlotDrawFlags.ExtendedView);
        bool singleItem = style.HasFlag(SlotDrawFlags.SingleItem);
        var comparisonMode = module.ConfigImpl.Data.IgnoreMateriaForBiS
            ? ItemComparisonMode.IgnoreMateria : ItemComparisonMode.Full;
        var (item, bis) = itemTuple;
        if (!item.Filled && !bis.Filled)
            return;
        if (singleItem || item.Filled && bis.Filled && item.Equals(bis, comparisonMode))
        {
            ImGui.SetCursorPosY(extended ? cursorSingleLarge : cursorSingleSmall);
            ImGui.BeginGroup();
            DrawItem(item, true);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
            ImGui.NewLine();
        }
        else
        {
            ImGui.BeginGroup();
            ImGui.SetCursorPosY(cursorDualTopY);
            DrawItem(item);
            if (!extended)
                ImGui.NewLine();
            ImGui.SetCursorPosY(cursorDualBottomY);
            DrawItem(bis);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (item.Filled && bis.Filled)
                    itemTuple.Draw();
                else if (item.Filled)
                {
                    ImGui.TextColored(Colors.TextPetrol, LootmasterLoc.ItemTooltip_hdg_Equipped);
                    item.Draw();
                }
                else
                {
                    ImGui.TextColored(Colors.TextPetrol, LootmasterLoc.ItemTooltip_hdg_bis);
                    bis.Draw();
                }
                ImGui.EndTooltip();
            }
        }
        void DrawItem(GearItem itemToDraw, bool multiLine = false)
        {
            if (itemToDraw.Filled)
            {
                if (extended || module.ConfigImpl.Data.ShowIconInGroupOverview)
                {
                    var icon = module.Services.UiSystem.GetIcon(itemToDraw);
                    {
                        Vector2 iconSize = new(ImGui.GetTextLineHeightWithSpacing()
                                             * (extended ? multiLine ? 2.4f : 1.4f : 1f));
                        ImGui.Image(icon.ImGuiHandle, iconSize * HrtWindow.ScaleFactor);
                        ImGui.SameLine();
                    }
                }
                string toDraw = string.Format(module.ConfigImpl.Data.ItemFormatString,
                                              itemToDraw.ItemLevel,
                                              itemToDraw.Source().FriendlyName(),
                                              itemToDraw.Slots.FirstOrDefault(GearSetSlot.None).FriendlyName());
                if (extended) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fullLineHeight * (multiLine ? 0.7f : 0.2f));
                Action<string> drawText = module.ConfigImpl.Data.ColoredItemNames
                    ? t => ImGui.TextColored(LevelColor(module.ConfigImpl.Data, itemToDraw), t)
                    : ImGui.Text;
                drawText(toDraw);
                if (!extended || !itemToDraw.Materia.Any())
                    return;
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fullLineHeight * (multiLine ? 0.7f : 0.2f));
                ImGui.Text(
                    $"( {string.Join(" | ", itemToDraw.Materia.ToList().ConvertAll(mat => $"{mat.StatType.Abbrev()} +{mat.GetStat()}"))} )");
            }
            else
                ImGui.Text(GeneralLoc.CommonTerms_Empty);

        }
    }

}