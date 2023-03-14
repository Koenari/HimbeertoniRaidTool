using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Calculations;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;
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
internal static class UiHelpers
{
    private static Vector2 MaxMateriaCatSize;
    private static Vector2 MaxMateriaLevelSize;

    static UiHelpers()
    {
        MaxMateriaCatSize = ImGui.CalcTextSize(Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "");
        MaxMateriaLevelSize = ImGui.CalcTextSize(Enum.GetNames<MateriaLevel>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "");
    }
    public static Vector4 ILevelColor(LootMasterConfiguration.ConfigData config, GearItem item)
    {
        return (config.SelectedRaidTier.ItemLevel(item.Slots.First()) - (int)item.ItemLevel) switch
        {
            <= 0 => config.ItemLevelColors[0],
            <= 10 => config.ItemLevelColors[1],
            <= 20 => config.ItemLevelColors[2],
            _ => config.ItemLevelColors[3],
        };
    }
    internal static void DrawSlot(LootMasterConfiguration.ConfigData config, GearItem item, SlotDrawFlags style = SlotDrawFlags.SingleItem | SlotDrawFlags.SimpleView)
        => DrawSlot(config, (item, GearItem.Empty), style);
    internal static void DrawSlot(LootMasterConfiguration.ConfigData config, (GearItem, GearItem) itemTuple, SlotDrawFlags style = SlotDrawFlags.Default)
    {
        bool extended = style.HasFlag(SlotDrawFlags.ExtendedView);
        bool singleItem = style.HasFlag(SlotDrawFlags.SingleItem);
        ItemComparisonMode comparisonMode = config.IgnoreMateriaForBiS
            ? ItemComparisonMode.IgnoreMateria : ItemComparisonMode.Full;
        (var item, var bis) = itemTuple;
        ImGui.TableNextColumn();
        if (singleItem || (item.Filled && bis.Filled && item.Equals(bis, comparisonMode)))
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() / (extended ? 2 : 1));
            ImGui.BeginGroup();
            DrawItem(item, extended, true);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
        }
        else
        {
            ImGui.BeginGroup();
            DrawItem(item, extended);
            ImGui.NewLine();
            DrawItem(bis, extended);
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
        void DrawItem(GearItem item, bool extended, bool multiLine = false)
        {
            if (item.Filled)
            {
                if (extended)
                {
                    ImGui.Image(Services.IconCache[item.Item!.Icon].ImGuiHandle, new Vector2(24) * HrtWindow.ScaleFactor);
                    ImGui.SameLine();
                }
                string toDraw = string.Format(config.ItemFormatString,
                    item.ItemLevel,
                    item.Source.FriendlyName(),
                    item.Slots.FirstOrDefault(GearSetSlot.None).FriendlyName());
                if (config.ColoredItemNames)
                    ImGui.TextColored(ILevelColor(config, item), toDraw);
                else
                    ImGui.Text(toDraw);
                if (extended)
                {
                    if (multiLine)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 32 * HrtWindow.ScaleFactor);
                    else
                        ImGui.SameLine();
                    ImGui.Text($"( {string.Join(" | ", item.Materia.ConvertAll(mat => $"{mat.StatType.Abbrev()} +{mat.GetStat()}"))} )");
                }
            }
            else
                ImGui.Text(Localize("Empty", "Empty"));

        }
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
            ImGuiComboFlags.None, (i, search) => i.Name.RawString.Contains(search, System.StringComparison.InvariantCultureIgnoreCase),
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
        for (int i = 0; i < item.Materia.Count; i++)
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser,
                $"Delete{slot}mat{i}", Localize("Remove this materia", "Remove this materia")))
            {
                item.Materia.RemoveAt(i);
                i--;
                continue;
            }
            float buttonWidth = ImGui.GetItemRectSize().X;
            ImGui.SameLine();
            var mat = item.Materia[i];
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
                item.Materia[i] = new(cat, mat.Level);
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
                item.Materia[i] = new(mat.Category, (byte)level);
            }

        }
        if (item.Materia.Count < (item.Item?.IsAdvancedMeldingPermitted ?? false ? 5 : item.Item?.MateriaSlotCount))
        {
            MateriaLevel leveltoAdd = (MateriaLevel)(item.Materia.Count > item.Item?.MateriaSlotCount ? (byte)(maxMatLevel - 1) : maxMatLevel);
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}addmat", Localize("Select materia", "Select materia")))
            {
                parent.AddChild(new SelectMateriaWindow(x => item.Materia.Add(x), (x) => { }, (byte)leveltoAdd));
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
                item.Materia.Add(new(cat, leveltoAdd));
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
    [Flags]
    public enum StatTableCompareMode
    {
        None = 0,
        DoCompare = 1,
        DiffLeftToRight = 2,
        DiffRightToLeft = 4,
        Default = DoCompare | DiffLeftToRight,
    }
    public static void DrawStatTable(PlayableClass curClass, IReadOnlyGearSet left, IReadOnlyGearSet right, string leftHeader, string diffHeader, string rightHeader,
        StatTableCompareMode compareMode = StatTableCompareMode.Default)
    {
        bool doCompare = compareMode.HasFlag(StatTableCompareMode.DoCompare);
        var curJob = curClass.Job;
        var curRole = curJob.GetRole();
        var mainStat = curJob.MainStat();
        var weaponStat = curRole == Role.Healer || curRole == Role.Caster ? StatType.MagicalDamage : StatType.PhysicalDamage;
        BeginAndSetupTable("MainStats", Localize("MainStats", "Main Stats"));
        DrawStatRow(weaponStat);
        DrawStatRow(StatType.Vitality);
        DrawStatRow(mainStat);
        DrawStatRow(StatType.Defense);
        DrawStatRow(StatType.MagicDefense);
        ImGui.EndTable();
        ImGui.NewLine();
        BeginAndSetupTable("SecondaryStats", Localize("SecondaryStats", "Secondary Stats"));
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
                    negative = diff > 0;
                return negative ? new(0.85f, 0.17f, 0.17f, 1f) : new(0.17f, 0.85f, 0.17f, 1f);
            }
            int numEvals = 1;
            if (type == StatType.CriticalHit || type == StatType.Tenacity || type == StatType.SpellSpeed || type == StatType.SkillSpeed)
                numEvals++;
            int leftStat = curClass.GetStat(type, left);
            int rightStat = curClass.GetStat(type, right);
            double[] leftEvalStat = new double[numEvals];
            double[] rightEvalStat = new double[numEvals];
            for (int i = 0; i < numEvals; i++)
            {
                leftEvalStat[i] = AllaganLibrary.EvaluateStat(type, curClass, left, i);
                rightEvalStat[i] = AllaganLibrary.EvaluateStat(type, curClass, right, i);
            }
            ImGui.TableNextColumn();
            ImGui.Text(type.FriendlyName());
            if (type == StatType.CriticalHit)
                ImGui.Text(Localize("Critical Damage", "Critical Damage"));
            if (type is StatType.SkillSpeed or StatType.SpellSpeed)
                ImGui.Text(Localize("SpeedMultiplierName", "AA / DoT multiplier"));

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
                var (Val, Unit) = AllaganLibrary.FormatStatValue(leftEvalStat[i], type, i);
                units[i] = Unit;
                ImGui.Text(Val);
            }
            if (type == weaponStat)
                ImGuiHelper.AddTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
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
                ImGuiHelper.AddTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
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
                ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.RowBg);
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
