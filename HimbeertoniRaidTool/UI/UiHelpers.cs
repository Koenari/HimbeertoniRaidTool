using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

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
        if (ImGuiHelper.Button(FontAwesomeIcon.Eraser,
                $"Delete{slot}", Localize("Remove this item", "Remove this item")))
        {
            item = GearItem.Empty;
            onItemChange(item);
        }
        ImGui.SameLine();
        ImGui.Image(ServiceManager.IconCache[item.Item?.Icon ?? 0].ImGuiHandle, new(24, 24));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            item.Draw();
            ImGui.EndTooltip();
        }
        ImGui.SameLine();
        //Quick select
        if (ImGuiHelper.ExcelSheetCombo($"##NewGear{slot}", out Item? outItem, x => item.Name,
            i => i.Name.RawString, IsApplicable, ImGuiComboFlags.NoArrowButton))
        {
            onItemChange(new(outItem.RowId));
        }
        //Select Window
        ImGui.SameLine();
        ImGui.BeginDisabled(parent.ChildIsOpen);
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}changeitem", Localize("Select item", "Select item")))
                parent.AddChild(new SelectGearItemWindow(onItemChange, (x) => { }, item, slot, curJob,
                     Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.ItemLevel(slot)));
            ImGui.EndDisabled();
        }
        MateriaLevel maxMatLevel = Common.Services.ServiceManager.GameInfo.CurrentExpansion.MaxMateriaLevel;
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
            ImGui.Image(ServiceManager.IconCache[mat.Item?.Icon ?? 0].ImGuiHandle, new(24, 24));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                mat.Draw();
                ImGui.EndTooltip();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaCatSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                $"##mat{slot}{i}",
                out MateriaCategory cat,
                mat.Category.PrefixName(),
                Enum.GetValues<MateriaCategory>(),
                cat => cat.PrefixName(),
                (cat, s) => cat.PrefixName().Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                cat.GetStatType().FriendlyName().Contains(s, StringComparison.InvariantCultureIgnoreCase),
                ImGuiComboFlags.NoArrowButton
                ) && cat != MateriaCategory.None
               )
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
                Enum.GetValues<MateriaLevel>(),
                val => val.ToString(),
                (val, s) => val.ToString().Contains(s, StringComparison.InvariantCultureIgnoreCase)
                        || (byte.TryParse(s, out byte search) && search - 1 == (byte)val),
                ImGuiComboFlags.NoArrowButton
                ))
            {
                item.ReplacecMateria(i, new(mat.Category, level));
            }

        }
        if (item.CanAffixMateria())
        {
            MateriaLevel leveltoAdd = item.MaxAffixableMateriaLevel();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}addmat", Localize("Select materia", "Select materia")))
            {
                parent.AddChild(new SelectMateriaWindow(item.AddMateria, (x) => { }, leveltoAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaCatSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                $"##matadd{slot}",
                out MateriaCategory cat,
                MateriaCategory.None.ToString(),
                Enum.GetValues<MateriaCategory>(),
                cat => cat.GetStatType().FriendlyName(),
                (cat, s) => cat.GetStatType().FriendlyName().Contains(s, StringComparison.InvariantCultureIgnoreCase),
                ImGuiComboFlags.NoArrowButton
                ) && cat != MateriaCategory.None)
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
