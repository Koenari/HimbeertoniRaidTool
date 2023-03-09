using System.Collections.Generic;
using HimbeertoniRaidTool.Common.Calculations;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;
internal class QuickCompareWindow : HrtWindow
{
    private readonly LootmasterUI _lmui;
    private readonly PlayableClass CurClass;
    private IReadOnlyGearSet CurGear => CurClass.Gear;
    private readonly Dictionary<GearSetSlot, GearItem> ItemOverrides;
    private IReadOnlyGearSet? NewGearCache = null;
    private IReadOnlyGearSet NewGear
    {
        get
        {
            if (NewGearCache != null)
                return NewGearCache;
            IReadOnlyGearSet result = CurClass.Gear;
            foreach ((var slot, var item) in ItemOverrides)
            {
                result = result.With(item, slot);
            }
            return NewGearCache = result;
        }
    }
    internal QuickCompareWindow(LootmasterUI lmui, PlayableClass job) : base()
    {
        CurClass = job;
        ItemOverrides = new();
        _lmui = lmui;
        Title = $"Compare";
        OpenCentered = true;
        (Size, SizeCondition) = (new(1600, 600), ImGuiCond.Appearing);

    }
    public override void Draw()
    {
        ImGui.BeginChild("SoloView");
        ImGui.Columns(3);
        /**
         * Current gear
         */
        {
            ImGui.BeginTable("GearCompareCurrenn", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn(Localize("Gear", "Gear"));
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            _lmui.DrawSlot(CurGear[GearSetSlot.MainHand], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.OffHand], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Head], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Ear], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Body], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Neck], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Hands], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Wrist], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Legs], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Ring1], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Feet], true);
            _lmui.DrawSlot(CurGear[GearSetSlot.Ring2], true);
            ImGui.EndTable();
        }
        /**
         * Stat Table
         */
        ImGui.NextColumn();

        var curRole = CurClass.Job.GetRole();
        var mainStat = CurClass.Job.MainStat();
        var weaponStat = curRole == Role.Healer || curRole == Role.Caster ? StatType.MagicalDamage : StatType.PhysicalDamage;
        var potencyStat = curRole == Role.Healer || curRole == Role.Caster ? StatType.AttackMagicPotency : StatType.AttackPower;
        ImGui.TextColored(Colors.Red, Localize("StatsUnfinished",
            "Stats are under development and only work correctly for level 70/80/90 jobs"));
        ImGui.BeginTable("MainStats", 5, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn(Localize("MainStats", "Main Stats"));
        ImGui.TableSetupColumn(Localize("Current", "Current"));
        ImGui.TableSetupColumn("");
        ImGui.TableSetupColumn(Localize("New Gear", "New Gear"));
        ImGui.TableSetupColumn("");
        ImGui.TableHeadersRow();
        DrawStatRow(weaponStat);
        DrawStatRow(StatType.Vitality);
        DrawStatRow(mainStat);
        DrawStatRow(StatType.Defense);
        DrawStatRow(StatType.MagicDefense);
        ImGui.EndTable();
        ImGui.NewLine();
        ImGui.BeginTable("SecondaryStats", 5, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV | ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn(Localize("SecondaryStats", "Secondary Stats"));
        ImGui.TableSetupColumn(Localize("Current", "Current"));
        ImGui.TableSetupColumn("");
        ImGui.TableSetupColumn(Localize("New Gear", "New Gear"));
        ImGui.TableSetupColumn("");
        ImGui.TableHeadersRow();
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
            int numEvals = 1;
            if (type == StatType.CriticalHit || type == StatType.Tenacity || type == StatType.SpellSpeed || type == StatType.SkillSpeed)
                numEvals++;
            ImGui.TableNextColumn();
            ImGui.Text(type.FriendlyName());
            if (type == StatType.CriticalHit)
                ImGui.Text(Localize("Critical Damage", "Critical Damage"));
            if (type is StatType.SkillSpeed or StatType.SpellSpeed)
                ImGui.Text(Localize("SpeedMultiplierName", "AA / DoT multiplier"));
            //Current
            ImGui.TableNextColumn();
            ImGui.Text(CurClass.GetStat(type, CurGear).ToString());
            ImGui.TableNextColumn();
            for (int i = 0; i < numEvals; i++)
                ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, CurClass, false, i, CurGear));
            if (type == weaponStat)
                ImGuiHelper.AddTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
            //New
            ImGui.TableNextColumn();
            ImGui.Text(CurClass.GetStat(type, NewGear).ToString());
            ImGui.TableNextColumn();
            for (int i = 0; i < numEvals; i++)
                ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, CurClass, true, i, NewGear));
            if (type == weaponStat)
                ImGuiHelper.AddTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
        }

        /**
         * New Gear
         */
        {
            ImGui.NextColumn();
            ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn(Localize("New Gear", "New Gear"));
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();

            DrawEditSlot(GearSetSlot.MainHand);
            DrawEditSlot(GearSetSlot.OffHand);
            DrawEditSlot(GearSetSlot.Head);
            DrawEditSlot(GearSetSlot.Ear);
            DrawEditSlot(GearSetSlot.Body);
            DrawEditSlot(GearSetSlot.Neck);
            DrawEditSlot(GearSetSlot.Hands);
            DrawEditSlot(GearSetSlot.Wrist);
            DrawEditSlot(GearSetSlot.Legs);
            DrawEditSlot(GearSetSlot.Ring1);
            DrawEditSlot(GearSetSlot.Feet);
            DrawEditSlot(GearSetSlot.Ring2);
            ImGui.EndTable();
            ImGui.EndChild();
        }
    }
    private void DrawEditSlot(GearSetSlot slot)
    {
        ImGui.TableNextColumn();
        ImGui.Text(NewGear[slot].Name);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            NewGear[slot].Draw();
            ImGui.EndTooltip();
        }
    }
}
