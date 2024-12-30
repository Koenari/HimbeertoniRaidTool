using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel;

namespace HimbeertoniRaidTool.Plugin.UI;

public class UiHelpers(IUiSystem uiSystem)
{
    private static readonly Lazy<Vector2> MaxMateriaCatSizeImpl =
        new(() => ImGui.CalcTextSize(Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? ""));
    private static Vector2 MaxMateriaCatSize => MaxMateriaCatSizeImpl.Value;
    private static readonly Lazy<Vector2> MaxMateriaLevelSizeImpl =
        new(() => ImGui.CalcTextSize(Enum.GetNames<MateriaLevel>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? ""));
    private static Vector2 MaxMateriaLevelSize => MaxMateriaLevelSizeImpl.Value;

    public void DrawFoodEdit(HrtWindowWithModalChild parent, FoodItem? item, Action<FoodItem?> onItemChange)
    {
        //LuminaItem Icon with Info
        var icon = item is null ? null : uiSystem.GetIcon(item);
        if (icon is not null)
        {
            ImGui.Image(icon.ImGuiHandle, new Vector2(24, 24));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item?.Draw();
                ImGui.EndTooltip();
            }
            ImGui.SameLine();

        }
        //Quick select
        string itemName = item?.ToString() ?? string.Empty;
        if (ExcelSheetCombo($"##Food", out LuminaItem outItem, _ => itemName,
                            i => i.Name.ExtractText(), ItemExtensions.IsFood,
                            ImGuiComboFlags.NoArrowButton))
        {
            onItemChange(new FoodItem(outItem.RowId));
        }
        //Select Window
        ImGui.SameLine();
        ImGui.BeginDisabled(parent.ChildIsOpen);
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"FoodChangeItem",
                                   GeneralLoc.EditGearSetUi_btn_tt_selectItem))
                parent.AddChild(new SelectFoodItemWindow(uiSystem, onItemChange, _ => { },
                                                         item,
                                                         GameInfo.PreviousSavageTier
                                                                 ?.ItemLevel(GearSetSlot.Body) + 10 ?? 0));
            ImGui.EndDisabled();
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"DeleteFood", GeneralLoc.General_btn_tt_remove))
        {
            item = null;
            onItemChange(item);
        }
    }

    public void DrawGearEdit(HrtWindowWithModalChild parent, GearSetSlot slot,
                             GearItem item, Action<GearItem> onItemChange, Job curJob = Job.ADV)
    {
        //LuminaItem Icon with Info
        ImGui.Image(uiSystem.GetIcon(item).ImGuiHandle, new Vector2(24, 24));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            item.Draw();
            ImGui.EndTooltip();
        }
        ImGui.SameLine();
        //Quick select
        string itemName = item.Name;
        if (ExcelSheetCombo($"##NewGear{slot}", out LuminaItem outItem, _ => itemName,
                            i => i.Name.ExtractText(), IsApplicable, ImGuiComboFlags.NoArrowButton))
        {
            onItemChange(new GearItem(outItem.RowId));
        }
        //Select Window
        ImGui.SameLine();
        ImGui.BeginDisabled(parent.ChildIsOpen);
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}changeItem",
                                   GeneralLoc.EditGearSetUi_btn_tt_selectItem))
                parent.AddChild(new SelectGearItemWindow(uiSystem, onItemChange, _ => { }, item, slot, curJob,
                                                         GameInfo.CurrentExpansion.CurrentSavage?.ItemLevel(slot)
                                                      ?? 0));
            ImGui.EndDisabled();
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"Delete{slot}", GeneralLoc.General_btn_tt_remove))
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
                              item.GetStat(StatType.MagicalDamage) >= item.GetStat(StatType.PhysicalDamage)
                                  ? StatType.SpellSpeed
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
                if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"##delete{slot}mat{i}",
                                       string.Format(GeneralLoc.General_btn_tt_remove, MateriaItem.DataTypeNameStatic,
                                                     string.Empty)))
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

            var mat = item.Materia.Skip(i).First();
            ImGui.Image(uiSystem.GetIcon(mat).ImGuiHandle, new Vector2(24, 24));
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
                    out var cat,
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
                item.ReplaceMateria(i, new MateriaItem(cat, mat.Level));
            }
            ImGui.SameLine();
            ImGui.Text(GeneralLoc.CommonTerms_Materia);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaLevelSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                    $"##matLevel{slot}{i}",
                    out var level,
                    mat.Level.ToString(),
                    Enum.GetValues<MateriaLevel>(),
                    val => val.ToString(),
                    (val, s) => val.ToString().Contains(s, StringComparison.InvariantCultureIgnoreCase)
                             || byte.TryParse(s, out byte search) && search - 1 == (byte)val,
                    ImGuiComboFlags.NoArrowButton
                ))
            {
                item.ReplaceMateria(i, new MateriaItem(mat.Category, level));
            }

        }
        if (item.CanAffixMateria())
        {
            var levelToAdd = item.MaxAffixableMateriaLevel();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}addMat", GeneralLoc.Ui_GearEdit_btn_tt_selectMat))
            {
                parent.AddChild(new SelectMateriaWindow(uiSystem, item.AddMateria, _ => { }, levelToAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaCatSize.X + 10 * HrtWindow.ScaleFactor);
            if (ImGuiHelper.SearchableCombo(
                    $"##matAdd{slot}",
                    out var cat,
                    MateriaCategory.None.ToString(),
                    Enum.GetValues<MateriaCategory>(),
                    category => category.GetStatType().FriendlyName(),
                    (category, s) => category.GetStatType().FriendlyName()
                                             .Contains(s, StringComparison.InvariantCultureIgnoreCase),
                    ImGuiComboFlags.NoArrowButton
                ) && cat != MateriaCategory.None)
            {
                item.AddMateria(new MateriaItem(cat, levelToAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaLevelSize.X + 10 * HrtWindow.ScaleFactor);
            ImGui.BeginDisabled();
            ImGui.BeginCombo("##Undef", $"{levelToAdd}", ImGuiComboFlags.NoArrowButton);
            ImGui.EndDisabled();
        }
        return;
        bool IsApplicable(LuminaItem itemToCheck)
        {
            return itemToCheck.ClassJobCategory.Value.Contains(curJob)
                && itemToCheck.EquipSlotCategory.Value.Contains(slot);
        }
    }
    //Credit to UnknownX
    //Modified to have filtering of Excel sheet and be usable by keyboard only
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview,
                                   ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, IExcelRow<T>
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString() ?? string.Empty, flags);
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview, Func<T, string, bool> searchPredicate,
                                   ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, IExcelRow<T>
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString() ?? string.Empty, searchPredicate, flags);
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview, Func<T, bool> preFilter,
                                   ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, IExcelRow<T>
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString() ?? string.Empty, preFilter, flags);
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview, Func<T, string, bool> searchPredicate,
                                   Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : struct, IExcelRow<T>
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString() ?? string.Empty, searchPredicate, preFilter,
                           flags);
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                   ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, IExcelRow<T>
        => ExcelSheetCombo(id, out selected, getPreview, toName,
                           (t, s) => toName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase), flags);
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                   Func<T, string, bool> searchPredicate,
                                   ImGuiComboFlags flags = ImGuiComboFlags.None) where T : struct, IExcelRow<T>
        => ExcelSheetCombo(id, out selected, getPreview, toName, searchPredicate, _ => true, flags);
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                   Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : struct, IExcelRow<T>
        => ExcelSheetCombo(id, out selected, getPreview, toName,
                           (t, s) => toName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase), preFilter,
                           flags);
    public bool ExcelSheetCombo<T>(string id, out T selected,
                                   Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                   Func<T, string, bool> searchPredicate,
                                   Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : struct, IExcelRow<T>
    {
        var sheet = uiSystem.GetExcelSheet<T>();

        return ImGuiHelper.SearchableCombo(id, out selected, getPreview(sheet), sheet, toName, searchPredicate,
                                           preFilter, flags);
    }
}