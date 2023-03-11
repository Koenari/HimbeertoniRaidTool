using Dalamud.Interface;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;
internal class QuickCompareWindow : HRTWindowWithModalChild
{
    private readonly LootmasterUI _lmui;
    private readonly PlayableClass CurClass;
    private IReadOnlyGearSet CurGear => CurClass.Gear;

    private readonly GearSet NewGear;
    internal QuickCompareWindow(LootmasterUI lmui, PlayableClass job) : base()
    {
        CurClass = job;
        NewGear = new();
        NewGear.CopyFrom(CurClass.Gear);
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
            ImGui.BeginTable("GearCompareCurrent", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
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
        LootmasterUI.DrawStatTable(CurClass, CurGear, NewGear,
            Localize("Current", "Current"), Localize("New Gear", "New Gear"));
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
    }
    private void DrawEditSlot(GearSetSlot slot)
    {
        var item = NewGear[slot];
        ImGui.TableNextColumn();
        ImGui.Image(Services.IconCache[item.Item?.Icon ?? 0].ImGuiHandle, new(24, 24));
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            NewGear[slot].Draw();
            ImGui.EndTooltip();
        }
        ImGui.SameLine();
        if (ImGuiHelper.ExcelSheetCombo($"##NewGear{slot}", out Item? outItem, x => NewGear[slot].Name,
            ImGuiComboFlags.None, (i, search) => i.Name.RawString.Contains(search, System.StringComparison.InvariantCultureIgnoreCase),
            i => i.Name.ToString(), IsApplicable))
        {
            NewGear[slot] = new(outItem.RowId);
        }
        for (int i = 0; i < item.Materia.Count; i++)
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser,
                $"Delete{slot}mat{i}", Localize("Remove this materia", "Remove this materia"), i == item.Materia.Count - 1))
            {
                item.Materia.RemoveAt(i);
                i--;
                continue;
            }
            ImGui.SameLine();
            ImGui.Text(item.Materia[i].Item?.Name.RawString);
        }
        if (item.Materia.Count < (item.Item?.IsAdvancedMeldingPermitted ?? false ? 5 : item.Item?.MateriaSlotCount))
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, $"{slot}addmat", Localize("Select materia", "Select materia")))
            {
                byte maxMatLevel = ServiceManager.GameInfo.CurrentExpansion.MaxMateriaLevel;
                if (item.Materia.Count > item.Item?.MateriaSlotCount)
                    maxMatLevel--;
                ModalChild = new SelectMateriaWindow(x => item.Materia.Add(x), (x) => { }, maxMatLevel);
            }
        }
        bool IsApplicable(Item item)
        {
            return item.ClassJobCategory.Value.Contains(CurClass.Job)
                && item.EquipSlotCategory.Value.Contains(slot);
        }
    }
}
