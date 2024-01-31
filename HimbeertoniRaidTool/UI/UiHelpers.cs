using System.Diagnostics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

internal static class UiHelpers
{
    private static readonly Vector2 _maxMateriaCatSize;
    private static readonly Vector2 _maxMateriaLevelSize;

    static UiHelpers()
    {
        _maxMateriaCatSize =
            ImGui.CalcTextSize(Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "");
        _maxMateriaLevelSize =
            ImGui.CalcTextSize(Enum.GetNames<MateriaLevel>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "");
    }
    public static void DrawGearEdit(HrtWindowWithModalChild parent, GearSetSlot slot,
        GearItem item, Action<GearItem> onItemChange, Job curJob = Job.ADV)
    {
        //Item Icon with Info
        ImGui.Image(ServiceManager.IconCache[item.Icon].ImGuiHandle, new Vector2(24, 24));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            item.Draw();
            ImGui.EndTooltip();
        }
        ImGui.SameLine();
        //Quick select
        string itemName = item.Name;
        if (ImGuiHelper.ExcelSheetCombo($"##NewGear{slot}", out Item? outItem, _ => itemName,
                i => i.Name.RawString, IsApplicable, ImGuiComboFlags.NoArrowButton))
        {
            onItemChange(new GearItem(outItem.RowId));
        }
        //Select Window
        ImGui.SameLine();
        ImGui.BeginDisabled(parent.ChildIsOpen);
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}changeItem", Localize("Select item", "Select item")))
                parent.AddChild(new SelectGearItemWindow(onItemChange, _ => { }, item, slot, curJob,
                    Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage?.ItemLevel(slot) ?? 0));
            ImGui.EndDisabled();
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Eraser,
                $"Delete{slot}", Localize("Remove this item", "Remove this item")))
        {
            item = GearItem.Empty;
            onItemChange(item);
        }
        float eraserButtonWidth = ImGui.GetItemRectSize().X;
        int matCount = item.Materia.Count();
        //Relic stats
        if (item.IsRelic())
        {
            item.RelicStats ??= new Dictionary<StatType, int>();
            DrawRelicStaField(item.RelicStats, StatType.CriticalHit);
            DrawRelicStaField(item.RelicStats, StatType.DirectHitRate);
            DrawRelicStaField(item.RelicStats, StatType.Determination);
            DrawRelicStaField(item.RelicStats,
                item.GetStat(StatType.MagicalDamage) >= item.GetStat(StatType.PhysicalDamage) ? StatType.SpellSpeed
                    : StatType.SkillSpeed);
        }
        void DrawRelicStaField(IDictionary<StatType, int> stats, StatType type)
        {
            stats.TryGetValue(type, out int value);
            if (ImGui.InputInt(type.FriendlyName(), ref value, 5, 10))
                stats[type] = value;
        }
        //Materia
        for (int i = 0; i < matCount; i++)
        {
            if (i == matCount - 1)
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Eraser,
                        $"Delete{slot}mat{i}", Localize("Remove this materia", "Remove this materia")))
                {
                    item.RemoveMateria(i);
                    i--;
                    continue;
                }
                ImGui.SameLine();
            }
            else
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + eraserButtonWidth
                                                          + (ImGui.GetTextLineHeightWithSpacing()
                                                             - ImGui.GetTextLineHeight()) * 2);

            HrtMateria mat = item.Materia.Skip(i).First();
            ImGui.Image(ServiceManager.IconCache[mat.Icon].ImGuiHandle, new Vector2(24, 24));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                mat.Draw();
                ImGui.EndTooltip();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(_maxMateriaCatSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                    $"##mat{slot}{i}",
                    out MateriaCategory cat,
                    mat.Category.PrefixName(),
                    Enum.GetValues<MateriaCategory>(),
                    category => category.PrefixName(),
                    (category, s) => category.PrefixName().Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                                     category.GetStatType().FriendlyName()
                                         .Contains(s, StringComparison.InvariantCultureIgnoreCase),
                    ImGuiComboFlags.NoArrowButton
                ) && cat != MateriaCategory.None
               )
            {
                item.ReplaceMateria(i, new HrtMateria(cat, mat.Level));
            }
            ImGui.SameLine();
            ImGui.Text(Localize("Materia", "Materia"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(_maxMateriaLevelSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                    $"##matLevel{slot}{i}",
                    out MateriaLevel level,
                    mat.Level.ToString(),
                    Enum.GetValues<MateriaLevel>(),
                    val => val.ToString(),
                    (val, s) => val.ToString().Contains(s, StringComparison.InvariantCultureIgnoreCase)
                                || byte.TryParse(s, out byte search) && search - 1 == (byte)val,
                    ImGuiComboFlags.NoArrowButton
                ))
            {
                item.ReplaceMateria(i, new HrtMateria(mat.Category, level));
            }

        }
        if (item.CanAffixMateria())
        {
            MateriaLevel levelToAdd = item.MaxAffixableMateriaLevel();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}addMat",
                    Localize("Select materia", "Select materia")))
            {
                parent.AddChild(new SelectMateriaWindow(item.AddMateria, _ => { }, levelToAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(_maxMateriaCatSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                    $"##matAdd{slot}",
                    out MateriaCategory cat,
                    MateriaCategory.None.ToString(),
                    Enum.GetValues<MateriaCategory>(),
                    category => category.GetStatType().FriendlyName(),
                    (category, s) => category.GetStatType().FriendlyName()
                        .Contains(s, StringComparison.InvariantCultureIgnoreCase),
                    ImGuiComboFlags.NoArrowButton
                ) && cat != MateriaCategory.None)
            {
                item.AddMateria(new HrtMateria(cat, levelToAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(_maxMateriaLevelSize.X + 10 * HrtWindow.ScaleFactor);
            ImGui.BeginDisabled();
            ImGui.BeginCombo("##Undef", $"{levelToAdd}", ImGuiComboFlags.NoArrowButton);
            ImGui.EndDisabled();
        }
        bool IsApplicable(Item itemToCheck)
        {
            return itemToCheck.ClassJobCategory.Value.Contains(curJob)
                   && itemToCheck.EquipSlotCategory.Value.Contains(slot);
        }
    }
}