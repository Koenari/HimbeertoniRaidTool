using System;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;
internal class QuickCompareWindow : HRTWindowWithModalChild
{
    //private readonly LootmasterUI _lmui;
    private readonly LootMasterConfiguration.ConfigData CurrentConfig;
    private readonly PlayableClass CurClass;
    private IReadOnlyGearSet CurGear => CurClass.Gear;

    private readonly GearSet NewGear;
    internal QuickCompareWindow(LootMasterConfiguration.ConfigData _lmConfig, PlayableClass job) : base()
    {
        CurrentConfig = _lmConfig;
        CurClass = job;
        NewGear = new();
        NewGear.CopyFrom(CurClass.Gear);
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
            var SlotDraw = (GearItem i) => UiHelpers.DrawSlot(CurrentConfig, i, SlotDrawFlags.DetailedSingle);
            ImGui.BeginTable("GearCompareCurrent", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn(Localize("Gear", "Gear"));
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            SlotDraw(CurGear[GearSetSlot.MainHand]);
            SlotDraw(CurGear[GearSetSlot.OffHand]);
            SlotDraw(CurGear[GearSetSlot.Head]);
            SlotDraw(CurGear[GearSetSlot.Ear]);
            SlotDraw(CurGear[GearSetSlot.Body]);
            SlotDraw(CurGear[GearSetSlot.Neck]);
            SlotDraw(CurGear[GearSetSlot.Hands]);
            SlotDraw(CurGear[GearSetSlot.Wrist]);
            SlotDraw(CurGear[GearSetSlot.Legs]);
            SlotDraw(CurGear[GearSetSlot.Ring1]);
            SlotDraw(CurGear[GearSetSlot.Feet]);
            SlotDraw(CurGear[GearSetSlot.Ring2]);
            ImGui.EndTable();
        }
        /**
         * Stat Table
         */
        ImGui.NextColumn();
        UiHelpers.DrawStatTable(CurClass, CurGear, NewGear,
            Localize("Current", "Current"), Localize("QuickCompareStatGain", "Gain"), Localize("New Gear", "New Gear"));
        /**
         * New Gear
         */
        {
            ImGui.NextColumn();
            ImGui.BeginTable("GearCompareNew", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
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
        void DrawEditSlot(GearSetSlot slot)
        {
            ImGui.TableNextColumn();
            UiHelpers.DrawGearEdit(this, slot, NewGear[slot], ItemChangeCallback(slot), CurClass.Job);
        }
    }
    private Action<GearItem> ItemChangeCallback(GearSetSlot slot)
        => newItem => NewGear[slot] = newItem;
}
