using System.Numerics;
using HimbeertoniRaidTool.Common.Calculations;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

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

    public static Vector4 LevelColor(LootMasterConfiguration.ConfigData config, GearItem item) =>
        (config.SelectedRaidTier.ItemLevel(item.Slots.First()) - (int)item.ItemLevel) switch
        {
            <= 0  => config.ItemLevelColors[0],
            <= 10 => config.ItemLevelColors[1],
            <= 20 => config.ItemLevelColors[2],
            _     => config.ItemLevelColors[3],
        };
    internal static void DrawGearSetCombo(string id, GearSet current, IEnumerable<GearSet> list,
                                          Action<GearSet> changeCallback,
                                          Func<HrtWindow, bool> addWindow, Job job = Job.ADV,
                                          float width = 85f)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.BeginCombo($"##{id}", $"{current}", ImGuiComboFlags.NoArrowButton))
        {
            foreach (GearSet curJobGearSet in list)
            {
                if (ImGui.Selectable($"{curJobGearSet}##{curJobGearSet.LocalId}"))
                    changeCallback(curJobGearSet);
            }
            if (ImGui.Selectable(LootmasterLoc.GearSetSelect_Add_new))
            {
                addWindow(new EditGearSetWindow(new GearSet(), job, changeCallback));
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
        ItemComparisonMode comparisonMode = config.IgnoreMateriaForBiS
            ? ItemComparisonMode.IgnoreMateria : ItemComparisonMode.Full;
        (GearItem? item, GearItem? bis) = itemTuple;
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
                    ImGui.Columns(2);
                item.Draw();
                if (item.Filled && bis.Filled)
                    ImGui.NextColumn();
                bis.Draw();
                ImGui.Columns();
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
                if (!extended)
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
        bool doCompare = compareMode.HasFlag(StatTableCompareMode.DoCompare);
        Job curJob = curClass.Job;
        Role curRole = curJob.GetRole();
        StatType mainStat = curJob.MainStat();
        StatType weaponStat = curRole is Role.Healer or Role.Caster ? StatType.MagicalDamage
            : StatType.PhysicalDamage;
        BeginAndSetupTable("##MainStats", LootmasterLoc.StatTable_MainStats_Title);
        DrawStatRow(weaponStat);
        DrawStatRow(StatType.Vitality);
        DrawStatRow(mainStat);
        DrawStatRow(StatType.Defense);
        DrawStatRow(StatType.MagicDefense);
        ImGui.EndTable();
        ImGui.NewLine();
        BeginAndSetupTable("##SecondaryStats", LootmasterLoc.StatTable_SecondaryStats_Title);
        DrawStatRow(StatType.CriticalHit);
        DrawStatRow(StatType.Determination);
        DrawStatRow(StatType.DirectHitRate);
        if (curRole == Role.Healer || curRole == Role.Caster)
        {
            DrawStatRow(StatType.SpellSpeed);
            if (curRole == Role.Healer)
                DrawStatRow(StatType.Piety);
        }
        else
        {
            DrawStatRow(StatType.SkillSpeed);
            if (curRole == Role.Tank)
                DrawStatRow(StatType.Tenacity);
        }
        ImGui.EndTable();
        ImGui.NewLine();
        void DrawStatRow(StatType type)
        {
            Vector4 Color(double diff)
            {
                bool negative;
                if (compareMode.HasFlag(StatTableCompareMode.DiffRightToLeft))
                    negative = diff > 0;
                else
                    negative = diff < 0;
                return negative ? new Vector4(0.85f, 0.17f, 0.17f, 1f) : new Vector4(0.17f, 0.85f, 0.17f, 1f);
            }
            int numEvals = 1;
            if (type == StatType.CriticalHit || type == StatType.Tenacity || type == StatType.SpellSpeed
             || type == StatType.SkillSpeed)
                numEvals++;
            int leftStat = curClass.GetStat(type, left, tribe);
            int rightStat = curClass.GetStat(type, right, tribe);
            double[] leftEvalStat = new double[numEvals];
            double[] rightEvalStat = new double[numEvals];
            for (int i = 0; i < numEvals; i++)
            {
                leftEvalStat[i] = AllaganLibrary.EvaluateStat(type, curClass, left, tribe, i);
                rightEvalStat[i] = AllaganLibrary.EvaluateStat(type, curClass, right, tribe, i);
            }
            ImGui.TableNextColumn();
            ImGui.Text(type.FriendlyName());
            if (type is StatType.CriticalHit or StatType.SkillSpeed or StatType.SpellSpeed)
                ImGui.Text(type.AlternativeFriendlyName());
            //Stats
            ImGui.TableNextColumn();
            ImGui.Text(leftStat.ToString());
            if (doCompare)
            {
                ImGui.TableNextColumn();
                int intDiff = rightStat - leftStat;
                if (intDiff == 0)
                    ImGui.Text(" - ");
                else
                    ImGui.TextColored(Color(intDiff), $" {(intDiff < 0 ? "" : "+")}{intDiff} ");
            }
            ImGui.TableNextColumn();
            ImGui.Text(rightStat.ToString());
            ImGui.TableNextColumn();
            //Evals
            ImGui.TableNextColumn();
            string[] units = new string[numEvals];
            for (int i = 0; i < numEvals; i++)
            {
                (string? val, string? unit) = AllaganLibrary.FormatStatValue(leftEvalStat[i], type, i);
                units[i] = unit;
                ImGui.Text(val);
            }
            if (type == weaponStat)
                ImGuiHelper.AddTooltip(LootmasterLoc.Ui_tt_Dmgper100);
            if (doCompare)
            {
                ImGui.TableNextColumn();
                for (int i = 0; i < numEvals; i++)
                {
                    double diff = rightEvalStat[i] - leftEvalStat[i];
                    if (double.IsNaN(diff) || diff == 0)
                    {
                        ImGui.Text(" - ");
                    }
                    else
                    {
                        ImGui.TextColored(Color(diff),
                                          $"{(diff < 0 ? "" : "+")}{AllaganLibrary.FormatStatValue(diff, type, i).Val}");
                    }
                }
            }
            ImGui.TableNextColumn();
            for (int i = 0; i < numEvals; i++)
            {
                ImGui.Text(AllaganLibrary.FormatStatValue(rightEvalStat[i], type, i).Val);
            }
            if (type == weaponStat)
                ImGuiHelper.AddTooltip(LootmasterLoc.Ui_tt_Dmgper100);
            ImGui.TableNextColumn();
            for (int i = 0; i < numEvals; i++)
            {
                ImGui.Text(units[i]);
            }
        }
        void BeginAndSetupTable(string id, string name)
        {
            int numCol = doCompare ? 9 : 7;
            ImGui.BeginTable(id, numCol,
                             ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH
                                                               | ImGuiTableFlags.BordersOuterV
                                                               | ImGuiTableFlags.RowBg);
            ImGui.TableSetupColumn(name);
            ImGui.TableSetupColumn(leftHeader);
            if (doCompare)
                ImGui.TableSetupColumn(diffHeader);
            ImGui.TableSetupColumn(rightHeader);
            ImGui.TableSetupColumn("     ");
            ImGui.TableSetupColumn(leftHeader);
            if (doCompare)
                ImGui.TableSetupColumn(diffHeader);
            ImGui.TableSetupColumn(rightHeader);
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
        }
    }
}