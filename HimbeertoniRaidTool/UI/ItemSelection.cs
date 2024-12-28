using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel;

namespace HimbeertoniRaidTool.Plugin.UI;

internal abstract class SelectItemWindow<T> : HrtWindow where T : Item
{
    // ReSharper disable once StaticMemberInGenericType
    protected static readonly ExcelSheet<Lumina.Excel.Sheets.Item> Sheet =
        UiSystem.GetExcelSheet<Lumina.Excel.Sheets.Item>();
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

internal class SelectFoodItemWindow : SelectItemWindow<FoodItem>
{
    private int _maxILvl;
    private int _minILvl;
    public SelectFoodItemWindow(Action<FoodItem> onSave, Action<FoodItem?> onCancel, FoodItem? currentItem = null,
                                int minItemLevel = 0) :
        base(onSave, onCancel)
    {
        Item = currentItem;
        _maxILvl = 0;
        _minILvl = minItemLevel;
    }
    protected override void DrawItemSelection()
    {
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        int min = _minILvl;
        if (ImGui.InputInt("-##min", ref min, 5))
        {
            _minILvl = min;
        }

        ImGui.SameLine();
        int max = _maxILvl;
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        if (ImGui.InputInt("iLvL##Max", ref max, 5))
        {
            _maxILvl = max;
        }
        foreach (var item in Sheet.Where(item => (_minILvl == 0 || item.LevelItem.RowId >= _minILvl)
                                              && (_maxILvl == 0 || item.LevelItem.RowId <= _maxILvl)
                                              && item.ItemAction.Value.Type == 845))
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
                    Save(new FoodItem(item.RowId)
                    {
                        IsHq = item.CanBeHq,
                    });
            }

            if (isCurrentItem)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.BeginGroup();
            var icon = UiSystem.GetIcon(item.Icon, item.CanBeHq);
            if (icon is not null)
            {
                ImGui.Image(icon.ImGuiHandle, new Vector2(32f, 32f));
                ImGui.SameLine();
            }
            ImGui.Text($"{item.Name.ExtractText()} (IL {item.LevelItem.RowId})");
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
        }
    }
}

internal class SelectGearItemWindow : SelectItemWindow<GearItem>
{
    private readonly bool _lockJob;
    private readonly bool _lockSlot;
    private List<Lumina.Excel.Sheets.Item> _items;
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
            _slots = [slot.Value];
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
        var slot = _slots.FirstOrDefault(GearSetSlot.None);
        if (ImGuiHelper.Combo("##slot", ref slot, t => t.FriendlyName()))
        {
            _slots = [slot];
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
        foreach (var item in _items)
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
            var icon = UiSystem.GetIcon(item.Icon, item.CanBeHq);
            if (icon is not null)
            {
                ImGui.Image(icon.ImGuiHandle, new Vector2(32f, 32f));
                ImGui.SameLine();
            }
            ImGui.Text($"{item.Name.ExtractText()} (IL {item.LevelItem.RowId})");
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }
        }
    }

    private List<Lumina.Excel.Sheets.Item> ReevaluateItems()
    {
        _items = Sheet.Where(x =>
                                 x.ClassJobCategory.RowId != 0
                              && (!_slots.Any() ||
                                  _slots.Any(slot => slot == GearSetSlot.None
                                                  || x.EquipSlotCategory.Value.Contains(slot)))
                              && (_maxILvl == 0 || x.LevelItem.RowId <= _maxILvl)
                              && x.LevelItem.RowId >= _minILvl
                              && (_job.GetValueOrDefault(0) == 0
                               || x.ClassJobCategory.Value.Contains(_job.GetValueOrDefault()))
        ).Take(50).ToList();
        _items.Sort((x, y) => (int)y.LevelItem.RowId - (int)x.LevelItem.RowId);
        return _items;
    }
}

internal class SelectMateriaWindow : SelectItemWindow<MateriaItem>
{
    private static readonly Dictionary<MateriaLevel, Dictionary<MateriaCategory, MateriaItem>> AllMateria;

    private readonly MateriaLevel _maxLvl;

    static SelectMateriaWindow()
    {
        AllMateria = new Dictionary<MateriaLevel, Dictionary<MateriaCategory, MateriaItem>>();
        foreach (var lvl in Enum.GetValues<MateriaLevel>())
        {
            Dictionary<MateriaCategory, MateriaItem> mats = new();
            foreach (var cat in Enum.GetValues<MateriaCategory>())
            {
                mats[cat] = new MateriaItem(cat, lvl);
            }
            AllMateria[lvl] = mats;
        }
    }

    public SelectMateriaWindow(Action<MateriaItem> onSave, Action<MateriaItem?> onCancel, MateriaLevel maxMatLvl,
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
        for (var lvl = _maxLvl; lvl != MateriaLevel.None; --lvl)
        {
            ImGui.Text($"{lvl}");
            foreach (var cat in Enum.GetValues<MateriaCategory>())
            {
                if (cat == MateriaCategory.None) continue;
                DrawButton(cat, lvl);
                ImGui.SameLine();
            }

            ImGui.NewLine();
            ImGui.Separator();
        }
        return;

        void DrawButton(MateriaCategory cat, MateriaLevel lvl)
        {
            var mat = AllMateria[lvl][cat];
            var icon = UiSystem.GetIcon(mat.Icon);
            if (icon is not null && ImGui.ImageButton(icon.ImGuiHandle, new Vector2(32)))
                Save(mat);
            else if (ImGuiHelper.Button(mat.Name, null))
            {
                Save(mat);
            }
            if (!ImGui.IsItemHovered()) return;
            ImGui.BeginTooltip();
            mat.Draw();
            ImGui.EndTooltip();
        }
    }


}