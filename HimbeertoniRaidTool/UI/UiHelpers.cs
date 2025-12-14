using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Core;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using XIVCalc.Interfaces;
using Role = HimbeertoniRaidTool.Common.Data.Role;

namespace HimbeertoniRaidTool.Plugin.UI;

public interface IStatTable : IDrawable;

public class UiHelpers(IUiSystem uiSystem, IGlobalServiceContainer services)
{
    private static readonly Lazy<Vector2> MaxMateriaCatSizeImpl =
        new(() => ImGui.CalcTextSize(Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? ""));
    private static readonly Lazy<Vector2> MaxMateriaLevelSizeImpl =
        new(() => ImGui.CalcTextSize(Enum.GetNames<MateriaLevel>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? ""));

    public IStatTable CreateStatTable(PlayableClass jobClass,
                                      Tribe? tribe,
                                      IReadOnlyGearSet leftGear,
                                      IReadOnlyGearSet rightGear,
                                      string leftHeader,
                                      string diffHeader,
                                      string rightHeader,
                                      StatTableCompareMode compareMode = StatTableCompareMode.Default) =>
        new StatTable(services, jobClass, tribe, leftGear, rightGear, leftHeader, diffHeader, rightHeader, compareMode);

    public void DrawFoodEdit(HrtWindowWithModalChild parent, FoodItem? item, Action<FoodItem?> onItemChange)
    {
        //LuminaItem Icon with Info
        var icon = item is null ? null : uiSystem.GetIcon(item);
        if (icon is not null)
        {
            ImGui.Image(icon.Handle, new Vector2(24, 24));
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                item?.Draw();
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
        using (ImRaii.Disabled(parent.ChildIsOpen))
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"FoodChangeItem",
                                   GeneralLoc.EditGearSetUi_btn_tt_selectItem))
                parent.AddChild(new SelectFoodItemWindow(uiSystem, onItemChange, _ => { },
                                                         item,
                                                         GameInfo.PreviousSavageTier
                                                                 ?.ItemLevel(GearSetSlot.Body) + 10 ?? 0));
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
        ImGui.Image(uiSystem.GetIcon(item).Handle, new Vector2(24, 24));
        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            item.Draw();
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
        using (ImRaii.Disabled(parent.ChildIsOpen))
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}changeItem",
                                   GeneralLoc.EditGearSetUi_btn_tt_selectItem))
                parent.AddChild(new SelectGearItemWindow(uiSystem, onItemChange, _ => { }, item, slot, curJob,
                                                         GameInfo.CurrentExpansion.CurrentSavage?.ItemLevel(slot)
                                                      ?? 0));
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
            ImGui.Image(uiSystem.GetIcon(mat).Handle, new Vector2(24, 24));
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                mat.Draw();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaCatSizeImpl.Value.X + 10 * HrtWindow.ScaleFactor);
            if (InputHelper.SearchableCombo(
                    $"##mat{slot}{i}",
                    out var cat,
                    mat.Category.PrefixName(),
                    Enum.GetValues<MateriaCategory>(),
                    category => category.PrefixName(),
                    (category, s) => category.PrefixName().Contains(s, StringComparison.InvariantCultureIgnoreCase) ||
                                     category.GetStatType().FriendlyName()
                                             .Contains(s, StringComparison.InvariantCultureIgnoreCase), false,
                    ImGuiComboFlags.NoArrowButton
                ) && cat != MateriaCategory.None
               )
            {
                item.ReplaceMateria(i, new MateriaItem(cat, mat.Level));
            }
            ImGui.SameLine();
            ImGui.Text(GeneralLoc.CommonTerms_Materia);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaLevelSizeImpl.Value.X + 10 * HrtWindow.ScaleFactor);
            if (InputHelper.SearchableCombo(
                    $"##matLevel{slot}{i}",
                    out var level,
                    mat.Level.ToString(),
                    Enum.GetValues<MateriaLevel>(),
                    val => val.ToString(),
                    (val, s) => val.ToString().Contains(s, StringComparison.InvariantCultureIgnoreCase)
                             || byte.TryParse(s, out byte search) && search - 1 == (byte)val, false,
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
            ImGui.SetNextItemWidth(MaxMateriaCatSizeImpl.Value.X + 10 * HrtWindow.ScaleFactor);
            if (InputHelper.SearchableCombo(
                    $"##matAdd{slot}",
                    out var cat,
                    MateriaCategory.None.GetStatType().FriendlyName(),
                    Enum.GetValues<MateriaCategory>(),
                    category => category.GetStatType().FriendlyName(),
                    (category, s) => category.GetStatType().FriendlyName()
                                             .Contains(s, StringComparison.InvariantCultureIgnoreCase), false,
                    ImGuiComboFlags.NoArrowButton
                ) && cat != MateriaCategory.None)
            {
                item.AddMateria(new MateriaItem(cat, levelToAdd));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxMateriaLevelSizeImpl.Value.X + 10 * HrtWindow.ScaleFactor);
            using (ImRaii.Disabled())
            {
                using var combo = ImRaii.Combo("##Undef", $"{levelToAdd}", ImGuiComboFlags.NoArrowButton);
            }
        }
        return;
        bool IsApplicable(LuminaItem itemToCheck)
        {
            return itemToCheck.ClassJobCategory.Value.Contains(curJob)
                && itemToCheck.EquipSlotCategory.Value.Contains(slot);
        }
    }
    //Credit to UnknownX
    //Modified to have filtering of the excel-sheet and be usable by keyboard only
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

        return InputHelper.SearchableCombo(id, out selected, getPreview(sheet), sheet, toName, searchPredicate,
                                           preFilter, false, flags);
    }
    internal void DrawPlayerCombo(string id, Player player,
                                  Action<Player> replaceCallback, float width = 80)
    {
        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo(id, player.NickName, ImGuiComboFlags.NoArrowButton);
        if (!combo) return;
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_ReplaceNew, Player.DataTypeName)))
            uiSystem.EditWindows.Create(new Player(), replaceCallback);
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_ReplaceKnown, Player.DataTypeName)))
            services.HrtDataManager.GetTable<Player>().OpenSearchWindow(uiSystem, replaceCallback);

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
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddNew, Character.DataTypeName)))
        {
            uiSystem.EditWindows.Create(new Character(), player.AddCharacter);
        }
        if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddKnown, Character.DataTypeName)))
        {
            services.HrtDataManager.GetTable<Character>().OpenSearchWindow(uiSystem, player.AddCharacter);
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
            newJobs.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.InvariantCulture));
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
        using (var combo = ImRaii.Combo($"##{id}", $"{current}", ImGuiComboFlags.NoArrowButton))
        {
            if (combo)
            {
                foreach (var curJobGearSet in list)
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, curJobGearSet.ManagedBy.TextColor());
                    if (ImGui.Selectable(
                            $"{curJobGearSet} - {curJobGearSet.ManagedBy.FriendlyName()}##{curJobGearSet.LocalId}"))
                        changeCallback(curJobGearSet);
                }
                if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddNew, GearSet.DataTypeName)))
                {
                    uiSystem.EditWindows.Create(new GearSet(), changeCallback, null, null, job);
                }
                if (ImGui.Selectable(string.Format(GeneralLoc.UiHelpers_txt_AddKnown, GearSet.DataTypeName)))
                {
                    services.HrtDataManager.GetTable<GearSet>().OpenSearchWindow(uiSystem, changeCallback);
                }
            }
        }
        if (ImGui.CalcTextSize(current.Name).X > width)
            ImGuiHelper.AddTooltip(current.Name);
    }

    public void DrawFood(FoodItem? food)
    {
        if (food is not null)
        {
            using (ImRaii.Group())
            {
                ImGui.Image(uiSystem.GetIcon(food).Handle,
                            new Vector2(ImGui.GetTextLineHeightWithSpacing() * 1.4f));
                ImGui.SameLine();
                ImGui.Text(food.ToString());
            }
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                food.Draw();
            }
        }
        else
        {
            ImGui.Text("No Food");
        }
    }

    private class StatTable(
        IGlobalServiceContainer services,
        PlayableClass jobClass,
        Tribe? tribe,
        IReadOnlyGearSet leftGear,
        IReadOnlyGearSet rightGear,
        string leftHeader,
        string diffHeader,
        string rightHeader,
        StatTableCompareMode compareMode = StatTableCompareMode.Default) : IStatTable
    {
        private readonly CoreConfig? _coreConfig =
            services.ConfigManager.TryGetConfig(typeof(CoreConfig), out CoreConfig? config) ? config : null;
        private PartyBonus? _bonusOverride;

        private PartyBonus _bonus =>
            _bonusOverride ?? (services.ConfigManager.TryGetConfig(typeof(CoreConfig), out CoreConfig? config) ?
                config.Data.PartyBonus : PartyBonus.None);

        private GearSetStatBlock _left => new(jobClass, leftGear, tribe, _bonus);
        private GearSetStatBlock _right => new(jobClass, rightGear, tribe, _bonus);

        public void Draw()
        {
            (int leftMain, int rightMain) = jobClass.Job.MainStat() switch
            {
                StatType.Strength     => (_left.Strength, _right.Strength),
                StatType.Dexterity    => (_left.Dexterity, _right.Dexterity),
                StatType.Intelligence => (_left.Intelligence, _right.Intelligence),
                StatType.Mind         => (_left.Mind, _right.Mind),
                _                     => (0, 0),
            };
            var bonusInput = _bonus;
            if (InputHelper.Combo("Party Bonus", ref bonusInput, b => b.FriendlyName()))
            {
                if (bonusInput != _bonus)
                    _bonusOverride = bonusInput;
                if (_bonusOverride == _coreConfig?.Data.PartyBonus)
                    _bonusOverride = null;
            }
            BeginAndSetupTable("##MainStats", LootmasterLoc.StatTable_MainStats_Title);
            DrawStatRow(_left.WeaponDamage, _right.WeaponDamage, "WeaponDamage",
            [
                ("Weapon DMG Multiplier", s => s.WeaponDamageMultiplier(), val => $"{100 * val:N0} %%", false),
                ("Dmg100/s", s => s.AverageSkillDamage(100) / s.Gcd(), val => $"{val:N0}", false),
            ]);
            DrawStatRow(_left.Vitality, _right.Vitality, StatType.Vitality.FriendlyName(),
                        [("MaxHP", s => s.MaxHp(), val => $"{val:N0} HP", false)]);
            DrawStatRow(leftMain, rightMain, jobClass.Job.MainStat().FriendlyName(),
                        [("Main Stat Multiplier", s => s.MainStatMultiplier(), val => $"{100 * val:N0} %%", false)]);
            DrawStatRow(_left.PhysicalDefense, _right.PhysicalDefense, StatType.Defense.FriendlyName(),
                        [("Mitigation", s => s.PhysicalDefenseMitigation(), val => $"{val * 100:N1} %%", false)]);
            DrawStatRow(_left.MagicalDefense, _right.MagicalDefense, StatType.MagicDefense.FriendlyName(),
                        [("Mitigation", s => s.MagicalDefenseMitigation(), val => $"{val * 100:N1} %%", false)]);
            ImGui.EndTable();
            ImGui.NewLine();
            BeginAndSetupTable("##SecondaryStats", LootmasterLoc.StatTable_SecondaryStats_Title);
            DrawStatRow(_left.CriticalHit, _right.CriticalHit, StatType.CriticalHit.FriendlyName(),
            [
                ("Chance", s => s.CritChance(), val => $"{val * 100:N1} %%", false),
                ("Damage", s => s.CritDamage(), val => $"{val * 100:N1} %%", false),
            ]);
            DrawStatRow(_left.Determination, _right.Determination, StatType.Determination.FriendlyName(),
                        [("Multiplier", s => s.DeterminationMultiplier(), val => $"{val * 100:N1} %%", false)]);
            DrawStatRow(_left.DirectHit, _right.DirectHit, StatType.DirectHitRate.FriendlyName(),
                        [("Chance", s => s.DirectHitChance(), val => $"{val * 100:N1} %%", false)]);
            if (jobClass.Job.GetRole() is Role.Healer or Role.Caster)
            {
                DrawStatRow(_left.SpellSpeed, _right.SpellSpeed, StatType.SpellSpeed.FriendlyName(),
                [
                    ("Gcd", s => s.Gcd(), val => $"{val:N2} s", true),
                    ("Dot/HoT Multiplier", s => s.HotMultiplier(), val => $"{val * 100:N1} %%", false),
                ]);
                if (jobClass.Job.GetRole() == Role.Healer)
                    DrawStatRow(_left.Piety, _right.Piety, StatType.Piety.FriendlyName(),
                                [("MP Regen", s => s.MpPerTick(), val => $"{val:N0} MP/s", false)]);
            }
            else
            {
                DrawStatRow(_left.SkillSpeed, _right.SkillSpeed, StatType.SkillSpeed.FriendlyName(),
                [
                    ("Gcd", s => s.Gcd(), val => $"{val:N2} s", true),
                    ("Dot/HoT Multiplier", s => s.DotMultiplier(), val => $"{val * 100:N1} %%", false),
                ]);
                if (jobClass.Job.GetRole() == Role.Tank)
                    DrawStatRow(_left.Tenacity, _right.Tenacity, StatType.Tenacity.FriendlyName(),
                    [
                        ("Outgoing Damage", s => s.TenacityOffensiveModifier(), val => $"{val * 100:N1} %%", false),
                        ("Incoming Damage", s => s.TenacityDefensiveModifier(), val => $"{val * 100:N1} %%", true),
                    ]);
            }
            ImGui.EndTable();
            ImGui.NewLine();
            return;
            void DrawStatRow(int leftStat, int rightStat, string heading,
                             (string hdg, Func<IStatEquations, double> eval, Func<double, string> format, bool
                                 lowerIsBeter)
                                 []
                                 evalDefinitions)
            {
                var leftEvaluations = evalDefinitions.Select(s => s.eval(_left.StatEquations)).ToImmutableArray();
                var rightEvaluations = evalDefinitions.Select(s => s.eval(_right.StatEquations)).ToImmutableArray();
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
                for (int i = 0; i < leftEvaluations.Length; i++)
                {
                    ImGui.Text(formats[i](leftEvaluations[i]));
                }
                if (compareMode.HasFlag(StatTableCompareMode.DoCompare))
                {
                    ImGui.TableNextColumn();
                    int intDiff = rightStat - leftStat;
                    if (intDiff == 0)
                        ImGui.Text(" - ");
                    else
                        ImGui.TextColored(Color(intDiff), $" {(intDiff < 0 ? "" : "+")}{intDiff} ");
                    for (int i = 0; i < leftEvaluations.Length; i++)
                    {
                        double diff = rightEvaluations[i] - leftEvaluations[i];
                        if (double.IsNaN(diff) || Math.Abs(diff) < 0.001)
                            ImGui.Text(" - ");
                        else
                            ImGui.TextColored(Color(diff, lowerIsBetters[i]),
                                              $" {(diff < 0 ? "" : "+")}{formats[i](diff)} ");
                    }
                }
                ImGui.TableNextColumn();
                ImGui.Text(rightStat.ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < rightEvaluations.Length; i++)
                {
                    ImGui.Text(formats[i](rightEvaluations[i]));
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
                int numCol = compareMode.HasFlag(StatTableCompareMode.DoCompare) ? 5 : 4;
                ImGui.BeginTable(id, numCol,
                                 ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH
                                                                   | ImGuiTableFlags.BordersOuterV
                                                                   | ImGuiTableFlags.RowBg);
                ImGui.TableSetupColumn(name);
                ImGui.TableSetupColumn("Effect");
                ImGui.TableSetupColumn(leftHeader);
                if (compareMode.HasFlag(StatTableCompareMode.DoCompare))
                    ImGui.TableSetupColumn(diffHeader);
                ImGui.TableSetupColumn(rightHeader);
                ImGui.TableHeadersRow();
            }
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