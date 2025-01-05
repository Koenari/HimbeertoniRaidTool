using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using XIVCalc.Interfaces;
using Role = HimbeertoniRaidTool.Common.Data.Role;

namespace HimbeertoniRaidTool.Plugin.UI;

public class UiHelpers(IUiSystem uiSystem, IGlobalServiceContainer services)
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
    internal void DrawPlayerCombo(string id, Player player,
                                  Action<Player> replaceCallback, float width = 80)
    {
        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo(id, player.NickName, ImGuiComboFlags.NoArrowButton);
        if (!combo) return;
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_ReplaceNew, player.DataTypeName)))
            uiSystem.EditWindows.Create(new Player(), replaceCallback);
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_ReplaceKnown, player.DataTypeName)))
            services.HrtDataManager.PlayerDb.OpenSearchWindow(uiSystem, replaceCallback);

    }
    internal void DrawCharacterCombo(string id, Player player, string nameFormat,
                                     float width = 110)
    {
        ImGui.SetNextItemWidth(width);
        using var combo =
            ImRaii.Combo(id, ToName(player.MainChar), ImGuiComboFlags.NoArrowButton);
        if (!combo) return;
        foreach (var character in player.Characters)
        {
            if (ImGui.Selectable(ToName(character)))
                player.MainChar = character;
        }
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddNew, Character.DataTypeNameStatic)))
        {
            uiSystem.EditWindows.Create(new Character(), player.AddCharacter);
        }
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddKnown, Character.DataTypeNameStatic)))
        {
            services.HrtDataManager.CharDb.OpenSearchWindow(uiSystem, player.AddCharacter);
        }
        return;
        string ToName(Character character)
        {
            return character.ToString(nameFormat, null);
        }
    }
    internal void DrawClassCombo(string id, Character character, float width = 110)
    {
        if (character.Classes.Any())
        {
            ImGui.SetNextItemWidth(width);
            using var combo = ImRaii.Combo(id, character.MainClass!.ToString(), ImGuiComboFlags.NoArrowButton);
            if (!combo) return;
            var classes = character.Classes.Where(c => !c.HideInUi).ToList();
            /*classes.Sort((a, b) =>
            {
                int levelDiff = b.Level - a.Level;
                if (levelDiff != 0) return levelDiff;
                return string.Compare(a.Name, b.Name, StringComparison.InvariantCulture);
            });*/
            foreach (var job in classes)
            {
                if (ImGui.Selectable(job.ToString()))
                    character.MainJob = job.Job;
            }
            ImGui.Separator();
            var newJobs = Enum.GetValues<Job>()
                              .Where(j => j.IsCombatJob())
                              .Where(j => character.Classes.All(c => c.Job != j)).ToList();
            newJobs.Sort(
                (a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.InvariantCulture));
            foreach (var newClass in newJobs)
            {
                if (!ImGui.Selectable(string.Format(GeneralLoc.Ui_btn_tt_add, newClass.ToString()))) continue;
                character.AddClass(newClass);
                character.MainJob = newClass;
            }
        }
        else
        {
            ImGui.Text(LootmasterLoc.Ui_txt_noJobs);
        }
    }
    internal void DrawGearSetCombo(string id, GearSet current, IEnumerable<GearSet> list,
                                   Action<GearSet> changeCallback, Job job = Job.ADV, float width = 85f)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo($"##{id}", $"{current}", ImGuiComboFlags.NoArrowButton))
        {
            foreach (var curJobGearSet in list)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, curJobGearSet.ManagedBy.TextColor());
                if (ImGui.Selectable(
                        $"{curJobGearSet} - {curJobGearSet.ManagedBy.FriendlyName()}##{curJobGearSet.LocalId}"))
                    changeCallback(curJobGearSet);
                ImGui.PopStyleColor();
            }
            if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddNew, current.DataTypeName)))
            {
                uiSystem.EditWindows.Create(new GearSet(), changeCallback, null, null, job);
            }
            if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddKnown, current.DataTypeName)))
            {
                services.HrtDataManager.GearDb.OpenSearchWindow(uiSystem, changeCallback);
            }
            ImGui.EndCombo();
        }
        if (ImGui.CalcTextSize(current.Name).X > width)
            ImGuiHelper.AddTooltip(current.Name);
    }
    public static void DrawStatTable(PlayableClass curClass, Tribe? tribe, IReadOnlyGearSet left,
                                     IReadOnlyGearSet right, string leftHeader, string diffHeader, string rightHeader,
                                     StatTableCompareMode compareMode = StatTableCompareMode.Default)
    {
        var leftStats = left.GetStatEquations(curClass, tribe);
        var rightStats = right.GetStatEquations(curClass, tribe);
        bool doCompare = compareMode.HasFlag(StatTableCompareMode.DoCompare);
        var curJob = curClass.Job;
        var curRole = curJob.GetRole();
        var mainStat = curJob.MainStat();
        var weaponStat = curRole is Role.Healer or Role.Caster ? StatType.MagicalDamage
            : StatType.PhysicalDamage;
        BeginAndSetupTable("##MainStats", LootmasterLoc.StatTable_MainStats_Title);
        DrawStatRow(weaponStat, "WeaponDamage",
        [
            ("Weapon DMG Multiplier", s => s.WeaponDamageMultiplier(), val => $"{100 * val:N0} %%", false),
            ("Dmg100/s", s => s.AverageSkillDamage(100) / s.Gcd(), val => $"{val:N0}", false),
        ]);
        DrawStatRow(StatType.Vitality, StatType.Vitality.FriendlyName(),
                    [("MaxHP", s => s.MaxHp(), val => $"{val:N0} HP", false)]);
        DrawStatRow(mainStat, mainStat.FriendlyName(),
                    [("Main Stat Multiplier", s => s.MainStatMultiplier(), val => $"{100 * val:N0} %%", false)]);
        DrawStatRow(StatType.Defense, StatType.Defense.FriendlyName(),
                    [("Mitigation", s => s.PhysicalDefenseMitigation(), val => $"{val * 100:N1} %%", false)]);
        DrawStatRow(StatType.MagicDefense, StatType.MagicDefense.FriendlyName(),
                    [("Mitigation", s => s.MagicalDefenseMitigation(), val => $"{val * 100:N1} %%", false)]);
        ImGui.EndTable();
        ImGui.NewLine();
        BeginAndSetupTable("##SecondaryStats", LootmasterLoc.StatTable_SecondaryStats_Title);
        DrawStatRow(StatType.CriticalHit, StatType.CriticalHit.FriendlyName(),
        [
            ("Chance", s => s.CritChance(), val => $"{val * 100:N1} %%", false),
            ("Damage", s => s.CritDamage(), val => $"{val * 100:N1} %%", false),
        ]);
        DrawStatRow(StatType.Determination, StatType.Determination.FriendlyName(),
                    [("Multiplier", s => s.DeterminationMultiplier(), val => $"{val * 100:N1} %%", false)]);
        DrawStatRow(StatType.DirectHitRate, StatType.DirectHitRate.FriendlyName(),
                    [("Chance", s => s.DirectHitChance(), val => $"{val * 100:N1} %%", false)]);
        if (curRole is Role.Healer or Role.Caster)
        {
            DrawStatRow(StatType.SpellSpeed, StatType.SpellSpeed.FriendlyName(),
            [
                ("Gcd", s => s.Gcd(), val => $"{val:N2} s", true),
                ("Dot/HoT Multiplier", s => s.HotMultiplier(), val => $"{val * 100:N1} %%", false),
            ]);
            if (curRole == Role.Healer)
                DrawStatRow(StatType.Piety, StatType.Piety.FriendlyName(),
                            [("MP Regen", s => s.MpPerTick(), val => $"{val:N0} MP/s", false)]);
        }
        else
        {
            DrawStatRow(StatType.SkillSpeed, StatType.SkillSpeed.FriendlyName(),
            [
                ("Gcd", s => s.Gcd(), val => $"{val:N2} s", true),
                ("Dot/HoT Multiplier", s => s.DotMultiplier(), val => $"{val * 100:N1} %%", false),
            ]);
            if (curRole == Role.Tank)
                DrawStatRow(StatType.Tenacity, StatType.Tenacity.FriendlyName(),
                [
                    ("Outgoing Damage", s => s.TenacityOffensiveModifier(), val => $"{val * 100:N1} %%", false),
                    ("Incoming Damage", s => s.TenacityDefensiveModifier(), val => $"{val * 100:N1} %%", true),
                ]);
        }
        ImGui.EndTable();
        ImGui.NewLine();
        return;
        void DrawStatRow(StatType statType, string heading,
                         (string hdg, Func<IStatEquations, double> eval, Func<double, string> format, bool lowerIsBeter)
                             []
                             evalDefinitions)
        {
            int leftStat = curClass.GetStat(statType, left, tribe);
            int rightStat = curClass.GetStat(statType, right, tribe);
            var leftEvals = evalDefinitions.Select(s => s.eval(leftStats)).ToImmutableArray();
            var rightEvals = evalDefinitions.Select(s => s.eval(rightStats)).ToImmutableArray();
            var formats = evalDefinitions.Select(s => s.format).ToImmutableArray();
            var lowerIsBetters = evalDefinitions.Select(s => s.lowerIsBeter).ToImmutableArray();

            ImGui.TableNextColumn();
            ImGui.Text(heading);
            ImGui.TableNextColumn();
            ImGui.NewLine();
            foreach ((string hdg, _, _, _) in evalDefinitions)
            {
                ImGui.Text(hdg);
            }
            ImGui.TableNextColumn();
            ImGui.Text(leftStat.ToString(LootmasterLoc.Culture));
            for (int i = 0; i < leftEvals.Length; i++)
            {
                ImGui.Text(formats[i](leftEvals[i]));
            }
            if (doCompare)
            {
                ImGui.TableNextColumn();
                int intDiff = rightStat - leftStat;
                if (intDiff == 0)
                    ImGui.Text(" - ");
                else
                    ImGui.TextColored(Color(intDiff), $" {(intDiff < 0 ? "" : "+")}{intDiff} ");
                for (int i = 0; i < leftEvals.Length; i++)
                {
                    double diff = rightEvals[i] - leftEvals[i];
                    if (double.IsNaN(diff) || Math.Abs(diff) < 0.001)
                        ImGui.Text(" - ");
                    else
                        ImGui.TextColored(Color(diff, lowerIsBetters[i]),
                                          $" {(diff < 0 ? "" : "+")}{formats[i](diff)} ");
                }
            }
            ImGui.TableNextColumn();
            ImGui.Text(rightStat.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < rightEvals.Length; i++)
            {
                ImGui.Text(formats[i](rightEvals[i]));
            }
        }
        Vector4 Color(double diff, bool lowerIsBetter = false)
        {
            if (lowerIsBetter) diff *= -1;
            if (compareMode.HasFlag(StatTableCompareMode.DiffRightToLeft)) diff *= -1;
            return diff < 0 ? Colors.TextSoftRed : Colors.TextGreen;
        }
        void BeginAndSetupTable(string id, string name)
        {
            int numCol = doCompare ? 5 : 4;
            ImGui.BeginTable(id, numCol,
                             ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH
                                                               | ImGuiTableFlags.BordersOuterV
                                                               | ImGuiTableFlags.RowBg);
            ImGui.TableSetupColumn(name);
            ImGui.TableSetupColumn("Effect");
            ImGui.TableSetupColumn(leftHeader);
            if (doCompare)
                ImGui.TableSetupColumn(diffHeader);
            ImGui.TableSetupColumn(rightHeader);
            ImGui.TableHeadersRow();
        }
    }
    public void DrawFood(FoodItem? food)
    {
        if (food is not null)
        {
            ImGui.BeginGroup();
            ImGui.Image(uiSystem.GetIcon(food).ImGuiHandle,
                        new Vector2(ImGui.GetTextLineHeightWithSpacing() * 1.4f));
            ImGui.SameLine();
            ImGui.Text(food.ToString());
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                food.Draw();
                ImGui.EndTooltip();
            }
        }
        else
        {
            ImGui.Text("No Food");
        }
    }

    [Flags]
    public enum StatTableCompareMode
    {
        None = 0,
        DoCompare = 1,
        DiffLeftToRight = 2,
        DiffRightToLeft = 4,
        Default = DoCompare | DiffLeftToRight,
    }
}