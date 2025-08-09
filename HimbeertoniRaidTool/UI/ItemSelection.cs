using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using Lumina.Excel;

namespace HimbeertoniRaidTool.Plugin.UI;

internal abstract class SelectItemWindow<T>(IUiSystem uiSystem, Action<T> onSave, Action<T?> onCancel) : HrtWindow(
    uiSystem,
    null, ImGuiWindowFlags.NoCollapse)
    where T : Item
{
    // ReSharper disable once StaticMemberInGenericType
    protected ExcelSheet<LuminaItem> Sheet =>
        UiSystem.GetExcelSheet<LuminaItem>();
    protected T? Item;
    protected virtual bool CanSave { get; set; } = true;


    public override void Draw()
    {
        if (CanSave && ImGuiHelper.SaveButton())
            Save();
        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
            Cancel();
        DrawItemSelection();
    }

    protected void Save(T? item = null)
    {
        if (item != null)
            Item = item;
        if (Item != null)
            onSave(Item);
        else
            onCancel(Item);
        Hide();
    }

    protected void Cancel()
    {
        onCancel(Item);
        Hide();
    }

    protected abstract void DrawItemSelection();
}

internal class SelectFoodItemWindow : SelectItemWindow<FoodItem>
{
    private int _maxILvl;
    private int _minILvl;
    public SelectFoodItemWindow(IUiSystem uiSystem, Action<FoodItem> onSave, Action<FoodItem?> onCancel,
                                FoodItem? currentItem = null,
                                int minItemLevel = 0) : base(uiSystem, onSave, onCancel)
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
                                              && item.IsFood()))
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
            using (ImRaii.Group())
            {
                ImGui.Image(UiSystem.GetIcon(item.Icon, item.CanBeHq).Handle, new Vector2(32f, 32f));
                ImGui.SameLine();
                ImGui.Text($"{item.Name.ExtractText()} (IL {item.LevelItem.RowId})");
            }
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                item.Draw();
            }
        }
    }
}

internal class SelectGearItemWindow : SelectItemWindow<GearItem>
{
    private readonly bool _lockJob;
    private readonly bool _lockSlot;
    private List<LuminaItem> _items;
    private Job? _job;
    private int _maxILvl;
    private int _minILvl;
    private IEnumerable<GearSetSlot> _slots;

    public SelectGearItemWindow(IUiSystem uiSystem, Action<GearItem> onSave, Action<GearItem?> onCancel,
                                GearItem? currentItem = null,
                                GearSetSlot? slot = null, Job? job = null, int maxItemLevel = 0) : base(uiSystem,
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
        using (ImRaii.Disabled(_lockJob))
        {
            if (ImGuiHelper.Combo("##job", ref _job, job => job.HasValue ? job.Value.ToString() : "-"))
                ReevaluateItems();
        }
        ImGui.SameLine();

        ImGui.SetNextItemWidth(125f * ScaleFactor);
        using (ImRaii.Disabled(_lockSlot))
        {
            var slot = _slots.FirstOrDefault(GearSetSlot.None);
            if (ImGuiHelper.Combo("##slot", ref slot, t => t.FriendlyName()))
            {
                _slots = [slot];
                ReevaluateItems();
            }
        }
        ImGui.SameLine();

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
            using (ImRaii.PushColor(ImGuiCol.Button, Colors.RedWood, isCurrentItem))
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{item.RowId}", GeneralLoc.SelectItemUi_btn_tt_useThis,
                                       true,
                                       new Vector2(32f, 32f)))
                {
                    if (isCurrentItem)
                        Cancel();
                    else
                        Save(new GearItem(item.RowId) { IsHq = item.CanBeHq });
                }
            }
            ImGui.SameLine();

            using (ImRaii.Group())
            {
                var icon = UiSystem.GetIcon(item.Icon, item.CanBeHq);
                ImGui.Image(icon.Handle, new Vector2(32f, 32f));
                ImGui.SameLine();
                ImGui.Text($"{item.Name.ExtractText()} (IL {item.LevelItem.RowId})");
            }

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                item.Draw();
            }
        }
    }

    private List<LuminaItem> ReevaluateItems()
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

    public SelectMateriaWindow(IUiSystem uiSystem, Action<MateriaItem> onSave, Action<MateriaItem?> onCancel,
                               MateriaLevel maxMatLvl,
                               MateriaLevel? matLevel = null) : base(uiSystem, onSave, onCancel)
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
            if (ImGui.ImageButton(UiSystem.GetIcon(mat).Handle, new Vector2(32)))
                Save(mat);
            else if (ImGuiHelper.Button(mat.Name, null))
            {
                Save(mat);
            }
            if (!ImGui.IsItemHovered())
                return;

            using var tooltip = ImRaii.Tooltip();
            mat.Draw();
        }
    }


}