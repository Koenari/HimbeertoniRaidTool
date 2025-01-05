using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class QuickCompareWindow : HrtWindowWithModalChild
{
    private readonly PlayableClass _curClass;
    private readonly Tribe? _curTribe;
    private readonly GearSet _newGear;
    private readonly Action<GearItem, SlotDrawFlags> _drawFunction;
    internal QuickCompareWindow(IUiSystem uiSystem, Action<GearItem, SlotDrawFlags> itemDraw, PlayableClass job,
                                Tribe? tribe) : base(uiSystem)
    {
        _curClass = job;
        _curTribe = tribe;
        _drawFunction = itemDraw;
        _newGear = new GearSet(_curClass.CurGear);
        Title = LootmasterLoc.QuickCompareUi_Title;
        OpenCentered = true;
        (Size, SizeCondition) = (new Vector2(1600, 650), ImGuiCond.Appearing);

    }
    private GearSet CurGear => _curClass.CurGear;
    public override void Draw()
    {
        using var child = ImRaii.Child("##SoloView");
        ImGui.Columns(3);
        /*
         * Current gear
         */
        using (ImRaii.Table("##GearCompareCurrent", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Gear);
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            ImGui.TableNextColumn();
            const SlotDrawFlags slotDrawFlags = SlotDrawFlags.DetailedSingle;
            _drawFunction(CurGear[GearSetSlot.MainHand], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.OffHand], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Head], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Ear], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Body], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Neck], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Hands], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Wrist], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Legs], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Ring1], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Feet], slotDrawFlags);
            ImGui.TableNextColumn();
            _drawFunction(CurGear[GearSetSlot.Ring2], slotDrawFlags);
        }
        using (ImRaii.Table("##GearCompareFoodCurrent", 1, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Food.Capitalized());
            ImGui.TableHeadersRow();
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawFood(CurGear.Food);
        }
        /*
         * Stat Table
         */
        ImGui.NextColumn();
        UiHelpers.DrawStatTable(_curClass, _curTribe, CurGear, _newGear,
                                LootmasterLoc.CurrentGear,
                                LootmasterLoc.QuickCompare_StatGain,
                                LootmasterLoc.QuickCompareUi_hdg_NewGear);
        /*
         * New Gear
         */
        ImGui.NextColumn();
        using (ImRaii.Table("##GearCompareNew", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn(LootmasterLoc.QuickCompareUi_hdg_NewGear);
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.MainHand, _newGear[GearSetSlot.MainHand],
                                          ItemChangeCallback(GearSetSlot.MainHand), _curClass.Job);
            if (_curClass.Job.CanHaveShield())
            {
                ImGui.TableNextColumn();
                UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.OffHand, _newGear[GearSetSlot.OffHand],
                                              ItemChangeCallback(GearSetSlot.OffHand), _curClass.Job);
            }
            else
                ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Head, _newGear[GearSetSlot.Head],
                                          ItemChangeCallback(GearSetSlot.Head), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Ear, _newGear[GearSetSlot.Ear],
                                          ItemChangeCallback(GearSetSlot.Ear), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Body, _newGear[GearSetSlot.Body],
                                          ItemChangeCallback(GearSetSlot.Body), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Neck, _newGear[GearSetSlot.Neck],
                                          ItemChangeCallback(GearSetSlot.Neck), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Hands, _newGear[GearSetSlot.Hands],
                                          ItemChangeCallback(GearSetSlot.Hands), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Wrist, _newGear[GearSetSlot.Wrist],
                                          ItemChangeCallback(GearSetSlot.Wrist), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Legs, _newGear[GearSetSlot.Legs],
                                          ItemChangeCallback(GearSetSlot.Legs), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Ring1, _newGear[GearSetSlot.Ring1],
                                          ItemChangeCallback(GearSetSlot.Ring1), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Feet, _newGear[GearSetSlot.Feet],
                                          ItemChangeCallback(GearSetSlot.Feet), _curClass.Job);
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawGearEdit(this, GearSetSlot.Ring2, _newGear[GearSetSlot.Ring2],
                                          ItemChangeCallback(GearSetSlot.Ring2), _curClass.Job);
        }
        using (ImRaii.Table("##newFood", 1, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Food);
            ImGui.TableHeadersRow();
            ImGui.TableNextColumn();
            UiSystem.Helpers.DrawFoodEdit(this, _newGear.Food, f => _newGear.Food = f);
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