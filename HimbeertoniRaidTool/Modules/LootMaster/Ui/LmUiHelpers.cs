using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using XIVCalc.Interfaces;
using Role = HimbeertoniRaidTool.Common.Data.Role;

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

    [Flags]
    public enum StatTableCompareMode
    {
        None = 0,
        DoCompare = 1,
        DiffLeftToRight = 2,
        DiffRightToLeft = 4,
        Default = DoCompare | DiffLeftToRight,
    }

    private static Vector4 LevelColor(LootMasterConfiguration.ConfigData config, GearItem item) =>
        (config.SelectedRaidTier.ItemLevel(item.Slots.FirstOrDefault(GearSetSlot.None)) - (int)item.ItemLevel) switch
        {
            <= 0  => config.ItemLevelColors[0],
            <= 10 => config.ItemLevelColors[1],
            <= 20 => config.ItemLevelColors[2],
            _     => config.ItemLevelColors[3],
        };
    internal static void DrawGearSetCombo(string id, GearSet current, IEnumerable<GearSet> list,
                                          Action<GearSet> changeCallback,
                                          Func<HrtWindow?, bool> addWindow, Job job = Job.ADV,
                                          float width = 85f)
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
            if (ImGui.Selectable(LootmasterLoc.GearSetSelect_Add_new))
            {
                addWindow(EditWindowFactory.Create(new GearSet(), changeCallback, null, null, job));
            }
            if (ImGui.Selectable(LootmasterLoc.GearSetSelect_AddFromDB))
            {
                addWindow(ServiceManager.HrtDataManager.GearDb.OpenSearchWindow(changeCallback));
            }
            ImGui.EndCombo();
        }
        if (ImGui.CalcTextSize(current.Name).X > width)
            ImGuiHelper.AddTooltip(current.Name);
    }
    internal static void DrawSlot(LootMasterConfiguration.ConfigData config, GearItem item,
                                  SlotDrawFlags style = SlotDrawFlags.SingleItem | SlotDrawFlags.SimpleView)
        => DrawSlot(config, (item, GearItem.Empty), style);
    internal static void DrawSlot(LootMasterConfiguration.ConfigData config, (GearItem, GearItem) itemTuple,
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
        var comparisonMode = config.IgnoreMateriaForBiS
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
                if (extended || config.ShowIconInGroupOverview)
                {
                    Vector2 iconSize = new(ImGui.GetTextLineHeightWithSpacing()
                                         * (extended ? multiLine ? 2.4f : 1.4f : 1f));
                    ImGui.Image(ServiceManager.IconCache.LoadIcon(itemToDraw.Icon, itemToDraw.IsHq).ImGuiHandle,
                                iconSize * HrtWindow.ScaleFactor);
                    ImGui.SameLine();
                }
                string toDraw = string.Format(config.ItemFormatString,
                                              itemToDraw.ItemLevel,
                                              itemToDraw.Source.FriendlyName(),
                                              itemToDraw.Slots.FirstOrDefault(GearSetSlot.None).FriendlyName());
                if (extended) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fullLineHeight * (multiLine ? 0.7f : 0.2f));
                Action<string> drawText = config.ColoredItemNames
                    ? t => ImGui.TextColored(LevelColor(config, itemToDraw), t)
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
            return;

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
}