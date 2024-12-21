using System.Numerics;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class QuickCompareWindow : HrtWindowWithModalChild
{
    private readonly PlayableClass _curClass;
    private readonly LootMasterConfiguration.ConfigData _currentConfig;
    private readonly Tribe? _curTribe;

    private readonly GearSet _newGear;
    internal QuickCompareWindow(LootMasterConfiguration.ConfigData lmConfig, PlayableClass job, Tribe? tribe)
    {
        _currentConfig = lmConfig;
        _curClass = job;
        _curTribe = tribe;
        _newGear = new GearSet(_curClass.CurGear);
        Title = LootmasterLoc.QuickCompareUi_Title;
        OpenCentered = true;
        (Size, SizeCondition) = (new Vector2(1600, 650), ImGuiCond.Appearing);

    }
    private IReadOnlyGearSet CurGear => _curClass.CurGear;
    public override void Draw()
    {
        ImGui.BeginChild("##SoloView");
        ImGui.Columns(3);
        /*
         * Current gear
         */
        {
            void DrawSlot(GearItem i)
            {
                LmUiHelpers.DrawSlot(_currentConfig, i, SlotDrawFlags.DetailedSingle);
            }
            ImGui.BeginTable("##GearCompareCurrent", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Gear);
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.MainHand]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.OffHand]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Head]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Ear]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Body]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Neck]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Hands]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Wrist]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Legs]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Ring1]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Feet]);
            ImGui.TableNextColumn();
            DrawSlot(CurGear[GearSetSlot.Ring2]);
            ImGui.EndTable();
        }
        /*
         * Stat Table
         */
        ImGui.NextColumn();
        LmUiHelpers.DrawStatTable(_curClass, _curTribe, CurGear, _newGear,
                                  LootmasterLoc.CurrentGear,
                                  LootmasterLoc.QuickCompare_StatGain,
                                  LootmasterLoc.QuickCompareUi_hdg_NewGear);
        /*
         * New Gear
         */
        {
            ImGui.NextColumn();
            ImGui.BeginTable("##GearCompareNew", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn(LootmasterLoc.QuickCompareUi_hdg_NewGear);
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
        return;
        void DrawEditSlot(GearSetSlot slot)
        {
            ImGui.TableNextColumn();
            UiHelpers.DrawGearEdit(this, slot, _newGear[slot], ItemChangeCallback(slot), _curClass.Job);
        }
    }
    private Action<GearItem> ItemChangeCallback(GearSetSlot slot)
        => newItem =>
        {
            foreach (var mat in _newGear[slot].Materia)
            {
                newItem.AddMateria(mat);
            }
            _newGear[slot] = newItem;
        };
}