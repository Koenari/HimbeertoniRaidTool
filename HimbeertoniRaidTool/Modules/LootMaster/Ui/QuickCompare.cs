using System.Numerics;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class QuickCompareWindow : HrtWindowWithModalChild
{
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
        /*
         * Current gear
         */
        {
            void DrawSlot(GearItem i)
            {
                LmUiHelpers.DrawSlot(_currentConfig, i, SlotDrawFlags.DetailedSingle);
            }
            ImGui.BeginTable("GearCompareCurrent", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn(Localize("Gear", "Gear"));
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            DrawSlot(CurGear[GearSetSlot.MainHand]);
            DrawSlot(CurGear[GearSetSlot.OffHand]);
            DrawSlot(CurGear[GearSetSlot.Head]);
            DrawSlot(CurGear[GearSetSlot.Ear]);
            DrawSlot(CurGear[GearSetSlot.Body]);
            DrawSlot(CurGear[GearSetSlot.Neck]);
            DrawSlot(CurGear[GearSetSlot.Hands]);
            DrawSlot(CurGear[GearSetSlot.Wrist]);
            DrawSlot(CurGear[GearSetSlot.Legs]);
            DrawSlot(CurGear[GearSetSlot.Ring1]);
            DrawSlot(CurGear[GearSetSlot.Feet]);
            DrawSlot(CurGear[GearSetSlot.Ring2]);
            ImGui.EndTable();
        }
        /*
         * Stat Table
         */
        ImGui.NextColumn();
        LmUiHelpers.DrawStatTable(_curClass, CurGear, _newGear,
            Localize("Current", "Current"), Localize("QuickCompareStatGain", "Gain"), Localize("New Gear", "New Gear"));
        /*
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