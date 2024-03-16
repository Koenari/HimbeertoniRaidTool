using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.UI;

internal abstract class SelectItemWindow<T> : HrtWindow where T : HrtItem
{
    // ReSharper disable once StaticMemberInGenericType
    protected static readonly ExcelSheet<Item> Sheet = ServiceManager.DataManager.GetExcelSheet<Item>()!;
    private readonly Action<T?> _onCancel;
    private readonly Action<T> _onSave;
    protected T? Item;

    internal SelectItemWindow(Action<T> onSave, Action<T?> onCancel)
    {
        (_onSave, _onCancel) = (onSave, onCancel);
        Flags = ImGuiWindowFlags.NoCollapse;
    }
    protected virtual bool CanSave { get; set; } = true;


    public override void Draw()
    {
        if (CanSave && ImGuiHelper.SaveButton())
            Save();
        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
            Cancel();
        DrawItemSelection();
        ImGui.End();
    }

    protected void Save(T? item = null)
    {
        if (item != null)
            Item = item;
        if (Item != null)
            _onSave(Item);
        else
            _onCancel(Item);
        Hide();
    }

    protected void Cancel()
    {
        _onCancel(Item);
        Hide();
    }

    protected abstract void DrawItemSelection();
}

internal class SelectGearItemWindow : SelectItemWindow<GearItem>
{
    private readonly bool _lockJob;
    private readonly bool _lockSlot;
    private List<Item> _items;
    private Job? _job;
    private int _maxILvl;
    private int _minILvl;
    private IEnumerable<GearSetSlot> _slots;

    public SelectGearItemWindow(Action<GearItem> onSave, Action<GearItem?> onCancel, GearItem? currentItem = null,
                                GearSetSlot? slot = null, Job? job = null, int maxItemLevel = 0) : base(
        onSave, onCancel)
    {
        Item = currentItem;
        if (slot.HasValue)
        {
            _lockSlot = true;
            _slots = new[] { slot.Value };
        }
        else
        {
            _slots = Item?.Slots ?? Array.Empty<GearSetSlot>();
        }

        _lockJob = job.HasValue;
        _job = job;
        Title = GeneralLoc.SelectGearItemui_Title
              + $"{string.Join(',', _slots.Select((e, _) => e.FriendlyName()))}";
        _maxILvl = maxItemLevel;
        _minILvl = _maxILvl > 30 ? _maxILvl - 30 : 0;
        _items = ReevaluateItems();
    }
    protected override bool CanSave => false;

    protected override void DrawItemSelection()
    {
        //Draw selection bar
        ImGui.SetNextItemWidth(65f * ScaleFactor);
        ImGui.BeginDisabled(_lockJob);
        if (ImGuiHelper.Combo("##job", ref _job))
            ReevaluateItems();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(125f * ScaleFactor);
        ImGui.BeginDisabled(_lockSlot);
        GearSetSlot slot = _slots.FirstOrDefault(GearSetSlot.None);
        if (ImGuiHelper.Combo("##slot", ref slot, t => t.FriendlyName()))
        {
            _slots = new[] { slot };
            ReevaluateItems();
        }

        ImGui.SameLine();
        ImGui.EndDisabled();
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        int min = _minILvl;
        if (ImGui.InputInt("-##min", ref min, 5))
        {
            _minILvl = min;
            ReevaluateItems();
        }

        ImGui.SameLine();
        int max = _maxILvl;
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        if (ImGui.InputInt("iLvL##Max", ref max, 5))
        {
            _maxILvl = max;
            ReevaluateItems();
        }

        //Draw item list
        foreach (Item item in _items)
        {
            bool isCurrentItem = item.RowId == Item?.Id;
            if (isCurrentItem)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{item.RowId}", GeneralLoc.SelectItemUi_btn_tt_useThis,
                                   true,
                                   new Vector2(32f, 32f)))
            {
                if (isCurrentItem)
                    Cancel();
                else
                    Save(new GearItem(item.RowId) { IsHq = item.CanBeHq });
            }

            if (isCurrentItem)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Image(ServiceManager.IconCache.LoadIcon(item.Icon, item.CanBeHq).ImGuiHandle,
                        new Vector2(32f, 32f));
            ImGui.SameLine();
            ImGui.Text($"{item.Name.RawString} (IL {item.LevelItem.Row})");
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
        }
    }

    private List<Item> ReevaluateItems()
    {
        _items = Sheet.Where(x =>
                                 x.ClassJobCategory.Row != 0
                              && (!_slots.Any() ||
                                  _slots.Any(slot => slot == GearSetSlot.None
                                                  || x.EquipSlotCategory.Value.Contains(slot)))
                              && (_maxILvl == 0 || x.LevelItem.Row <= _maxILvl)
                              && x.LevelItem.Row >= _minILvl
                              && (_job.GetValueOrDefault(0) == 0
                               || x.ClassJobCategory.Value.Contains(_job.GetValueOrDefault()))
        ).Take(50).ToList();
        _items.Sort((x, y) => (int)y.LevelItem.Row - (int)x.LevelItem.Row);
        return _items;
    }
}

internal class SelectMateriaWindow : SelectItemWindow<HrtMateria>
{
    private static readonly Dictionary<MateriaLevel, Dictionary<MateriaCategory, HrtMateria>> _allMateria;

    private readonly MateriaLevel _maxLvl;

    static SelectMateriaWindow()
    {
        _allMateria = new Dictionary<MateriaLevel, Dictionary<MateriaCategory, HrtMateria>>();
        foreach (MateriaLevel lvl in Enum.GetValues<MateriaLevel>())
        {
            Dictionary<MateriaCategory, HrtMateria> mats = new();
            foreach (MateriaCategory cat in Enum.GetValues<MateriaCategory>())
            {
                mats[cat] = new HrtMateria(cat, lvl);
            }
            _allMateria[lvl] = mats;
        }
    }

    public SelectMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel, MateriaLevel maxMatLvl,
                               MateriaLevel? matLevel = null) : base(onSave, onCancel)
    {
        _maxLvl = matLevel ?? maxMatLvl;
        Title = GeneralLoc.SelectMateriaUi_Title;
        string longestName = Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "";
        Size = new Vector2(ImGui.CalcTextSize(longestName).X + 200f, 120f);
    }
    protected override bool CanSave => false;

    protected override void DrawItemSelection()
    {
        //1 Row per Level
        for (MateriaLevel lvl = _maxLvl; lvl != MateriaLevel.None; --lvl)
        {
            ImGui.Text($"{lvl}");
            foreach (MateriaCategory cat in Enum.GetValues<MateriaCategory>())
            {
                if (cat == MateriaCategory.None) continue;
                DrawButton(cat, lvl);
                ImGui.SameLine();
            }

            ImGui.NewLine();
            ImGui.Separator();
        }

        void DrawButton(MateriaCategory cat, MateriaLevel lvl)
        {
            HrtMateria mat = _allMateria[lvl][cat];
            if (ImGui.ImageButton(ServiceManager.IconCache[mat.Icon].ImGuiHandle, new Vector2(32)))
                Save(mat);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                mat.Draw();
                ImGui.EndTooltip();
            }
        }
    }


}