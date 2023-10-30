using System.Numerics;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal class QuickCompareWindow : HrtWindowWithModalChild
{
    //private readonly LootmasterUI _lmui;
    private readonly LootMasterConfiguration.ConfigData _currentConfig;
    private readonly PlayableClass _curClass;
    private IReadOnlyGearSet CurGear => _curClass.Gear;

    private readonly GearSet _newGear;
    internal QuickCompareWindow(LootMasterConfiguration.ConfigData lmConfig, PlayableClass job) : base()
    {
        _currentConfig = lmConfig;
        _curClass = job;
        _newGear = new GearSet();
        _newGear.CopyFrom(_curClass.Gear);
        Title = $"Compare";
        OpenCentered = true;
        (Size, SizeCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);

    }
    public override void Draw()
    {
        ImGui.BeginChild("SoloView");
        ImGui.Columns(3);
        /**
         * Current gear
         */
        {
            var slotDraw = (GearItem i) => LmUiHelpers.DrawSlot(_currentConfig, i, SlotDrawFlags.DetailedSingle);
            ImGui.BeginTable("GearCompareCurrent", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn(Localize("Gear", "Gear"));
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            slotDraw(CurGear[GearSetSlot.MainHand]);
            slotDraw(CurGear[GearSetSlot.OffHand]);
            slotDraw(CurGear[GearSetSlot.Head]);
            slotDraw(CurGear[GearSetSlot.Ear]);
            slotDraw(CurGear[GearSetSlot.Body]);
            slotDraw(CurGear[GearSetSlot.Neck]);
            slotDraw(CurGear[GearSetSlot.Hands]);
            slotDraw(CurGear[GearSetSlot.Wrist]);
            slotDraw(CurGear[GearSetSlot.Legs]);
            slotDraw(CurGear[GearSetSlot.Ring1]);
            slotDraw(CurGear[GearSetSlot.Feet]);
            slotDraw(CurGear[GearSetSlot.Ring2]);
            ImGui.EndTable();
        }
        /**
         * Stat Table
         */
        ImGui.NextColumn();
        LmUiHelpers.DrawStatTable(_curClass, CurGear, _newGear,
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
            if (_curClass.Job.CanHaveShield())
                DrawEditSlot(GearSetSlot.OffHand);
            else
                ImGui.TableNextColumn();
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
            UiHelpers.DrawGearEdit(this, slot, _newGear[slot], ItemChangeCallback(slot), _curClass.Job);
        }
    }
    private Action<GearItem> ItemChangeCallback(GearSetSlot slot)
        => newItem =>
        {
            foreach (HrtMateria? mat in _newGear[slot].Materia)
                newItem.AddMateria(mat);
            _newGear[slot] = newItem;
        };
}