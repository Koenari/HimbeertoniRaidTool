using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;
internal class UiHelpers
{
    private static Vector2 MaxMateriaCatSize;
    private static Vector2 MaxMateriaLevelSize;

    static UiHelpers()
    {
        MaxMateriaCatSize = ImGui.CalcTextSize(Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "");
        MaxMateriaLevelSize = ImGui.CalcTextSize(Enum.GetNames<MateriaLevel>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "");
    }
    public static void DrawGearEdit(HRTWindowWithModalChild parent, GearSetSlot slot,
        GearItem item, Action<GearItem> onItemChange, Job curJob = Job.ADV)
    {
        //Item Icon with Info
        ImGui.Image(Services.IconCache[item.Item?.Icon ?? 0].ImGuiHandle, new(24, 24));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            item.Draw();
            ImGui.EndTooltip();
        }
        ImGui.SameLine();
        //Quick select
        if (ImGuiHelper.ExcelSheetCombo($"##NewGear{slot}", out Item? outItem, x => item.Name,
            ImGuiComboFlags.NoArrowButton, (i, search) => i.Name.RawString.Contains(search, System.StringComparison.InvariantCultureIgnoreCase),
            i => i.Name.ToString(), IsApplicable))
        {
            onItemChange(new(outItem.RowId));
        }
        //Select Window
        ImGui.SameLine();
        ImGui.BeginDisabled(parent.ChildIsOpen);
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}changeitem", Localize("Select item", "Select item")))
                parent.AddChild(new SelectGearItemWindow(onItemChange, (x) => { }, item, slot, curJob,
                     ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.ItemLevel(slot)));
            ImGui.EndDisabled();
        }
        byte maxMatLevel = ServiceManager.GameInfo.CurrentExpansion.MaxMateriaLevel;
        for (int i = 0; i < item.Materia.Count(); i++)
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser,
                $"Delete{slot}mat{i}", Localize("Remove this materia", "Remove this materia")))
            {
                item.RemoveMateria(i);
                i--;
                continue;
            }
            ImGui.SameLine();
            var mat = item.Materia.Skip(i).First();
            ImGui.SetNextItemWidth(MaxMateriaCatSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                $"##mat{slot}{i}",
                out MateriaCategory cat,
                mat.Category.PrefixName(),
                ImGuiComboFlags.NoArrowButton,
                Enum.GetValues<MateriaCategory>(),
                (cat, s) =>
                cat.PrefixName().Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                cat.GetStatType().FriendlyName().Contains(s, StringComparison.InvariantCultureIgnoreCase),
                cat => cat.PrefixName(),
                _ => true) && cat != MateriaCategory.None)
            {
                item.ReplacecMateria(i, new(cat, mat.Level));
            }
            ImGui.SameLine();
            ImGui.Text(Localize("Materia", "Materia"));
            bool overmeld = i >= item.Item?.MateriaSlotCount;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaLevelSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                $"##matlevel{slot}{i}",
                out MateriaLevel level,
                mat.Level.ToString(),
                ImGuiComboFlags.NoArrowButton,
                Enum.GetValues<MateriaLevel>(),
                (val, s) => val.ToString().Contains(s, StringComparison.InvariantCultureIgnoreCase)
                        || (byte.TryParse(s, out byte search) && search - 1 == (byte)val),
                v => v.ToString(),
                _ => true
                ))
            {
                item.ReplacecMateria(i, new(mat.Category, (byte)level));
            }

        }
        if (item.CanAffixMateria())
        {
            MateriaLevel leveltoAdd = item.MaxAffixableMateriaLevel();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}addmat", Localize("Select materia", "Select materia")))
            {
                parent.AddChild(new SelectMateriaWindow(item.AddMateria, (x) => { }, (byte)leveltoAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaCatSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                $"##matadd{slot}",
                out MateriaCategory cat,
                MateriaCategory.None.ToString(),
                ImGuiComboFlags.NoArrowButton,
                Enum.GetValues<MateriaCategory>(),
                (cat, s) => cat.GetStatType().FriendlyName().Contains(s, StringComparison.InvariantCultureIgnoreCase),
                cat => cat.GetStatType().FriendlyName(),
                _ => true) && cat != MateriaCategory.None)
            {
                item.AddMateria(new(cat, leveltoAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaLevelSize.X + 10 * HrtWindow.ScaleFactor);
            ImGui.BeginDisabled();
            ImGui.BeginCombo("##Undef", $"{leveltoAdd}", ImGuiComboFlags.NoArrowButton);
            ImGui.EndDisabled();
        }
        bool IsApplicable(Item item)
        {
            return item.ClassJobCategory.Value.Contains(curJob)
                && item.EquipSlotCategory.Value.Contains(slot);
        }
    }
}
