using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

internal class EditPlayerWindow : HrtWindow
{
    private readonly Player _player;
    private readonly Player _playerCopy;
    private readonly Action<HrtUiMessage> _callBack;
    private readonly bool _isNew;
    private Job _newJob = Job.ADV;
    private const int CLASS_HEIGHT = 27 * 2 + 4;

    internal EditPlayerWindow(Action<HrtUiMessage> callBack, Player p)
        : base()
    {
        _callBack = callBack;
        _player = p;
        _playerCopy = new Player();
        var target = ServiceManager.TargetManager.Target as PlayerCharacter;
        _isNew = !_player.Filled;
        if (_isNew && target is not null)
        {
            _playerCopy.MainChar.Name = target.Name.TextValue;
            _playerCopy.MainChar.HomeWorldId = target.HomeWorld.Id;
            _playerCopy.MainChar.MainJob = target.GetJob();
        }
        else if (_player.Filled)
        {
            _playerCopy = _player.Clone();
        }

        Size = new Vector2(750, 330 + CLASS_HEIGHT * _playerCopy.MainChar.Classes.Count());
        SizeCondition = ImGuiCond.Appearing;
        Title = $"{Localize("Edit Player", "Edit Player")} {_player.NickName}";
    }

    public override void Draw()
    {
        //Buttons
        if (ImGuiHelper.SaveButton(Localize("Save Player", "Save Player")))
        {
            SavePlayer();
            Hide();
        }

        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
            Hide();
        //Player Data
        ImGui.SetCursorPosX(
            (ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Player Data", "Player Data")).X) / 2f);
        ImGui.Text(Localize("Player Data", "Player Data"));
        ImGui.InputText(Localize("Player Name", "Player Name"), ref _playerCopy.NickName, 50);
        //Character Data
        ImGui.SetCursorPosX(
            (ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Character Data", "Character Data")).X) / 2f);
        ImGui.Text(Localize("Character Data", "Character Data"));
        if (ImGui.InputText(Localize("Character Name", "Character Name"), ref _playerCopy.MainChar.Name, 50)
            && ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? pc, _playerCopy.MainChar.Name))
            _playerCopy.MainChar.HomeWorld ??= pc?.HomeWorld.GameData;
        if (ImGuiHelper.ExcelSheetCombo(Localize("Home World", "Home World") + "##" + Title, out World? w,
                x => _playerCopy.MainChar.HomeWorld?.Name.RawString ?? "",
                x => x.Name.RawString, x => x.IsPublic))
            _playerCopy.MainChar.HomeWorld = w;

        //ImGuiHelper.Combo(Localize("Gender", "Gender"), ref PlayerCopy.MainChar.Gender);
        string GetGenderedTribeName(Tribe? t)
        {
            return (_playerCopy.MainChar.Gender == Gender.Male ? t?.Masculine.RawString : t?.Feminine.RawString) ??
                   string.Empty;
        }

        if (ImGuiHelper.ExcelSheetCombo(Localize("Tribe", "Tribe") + "##" + Title, out Tribe? t,
                _ => GetGenderedTribeName(_playerCopy.MainChar.Tribe), GetGenderedTribeName))
            _playerCopy.MainChar.TribeId = t?.RowId ?? 0;
        //Class Data
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Job Data", "Job Data")).X) / 2f);
        ImGui.Text(Localize("Job Data", "Job Data"));
        if (_playerCopy.MainChar.Classes.Any())
        {
            if (_playerCopy.MainChar.Classes.All(x => x.Job != _playerCopy.MainChar.MainJob))
                _playerCopy.MainChar.MainJob = _playerCopy.MainChar.Classes.First().Job;
            if (ImGui.BeginCombo(Localize("Main Job", "Main Job"), _playerCopy.MainChar.MainJob.ToString()))
                foreach (PlayableClass curJob in _playerCopy.MainChar)
                    if (ImGui.Selectable(curJob.Job.ToString()))
                        _playerCopy.MainChar.MainJob = curJob.Job;
        }
        else
        {
            _playerCopy.MainChar.MainJob = null;
            ImGui.NewLine();
            ImGui.Text(Localize("NoClasses", "Character does not have any classes created"));
        }

        ImGui.Columns(2, "Classes", false);
        ImGui.SetColumnWidth(0, 70f * ScaleFactor);
        //ImGui.SetColumnWidth(1, 480f * ScaleFactor);
        ImGui.Separator();
        Job? toDelete = null;
        foreach (PlayableClass c in _playerCopy.MainChar.Classes)
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"delete{c.Job}", $"Delete all data for {c.Job}"))
                toDelete = c.Job;
            ImGui.SameLine();
            ImGui.Text($"{c.Job}  ");
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(250f * ScaleFactor);
            bool localBis = c.Bis is { IsEmpty: false, ManagedBy: GearSetManager.Hrt };
            ImGui.BeginDisabled(localBis);
            if (ImGui.InputText($"{Localize("BIS", "BIS")}##{c.Job}", ref c.Bis.EtroId, 60))
                c.Bis.EtroId = c.Bis.EtroId.Replace(EtroConnector.GEARSET_WEB_BASE_URL, "");

            if (localBis)
                ImGuiHelper.AddTooltip(Localize("PlayerEdit:Tooltip:LocalBis",
                    "BiS set for this class is locally managed.\nDelete local set to use a set from etro.gg"));
            foreach ((string etroId, string name) in ServiceManager.ConnectorPool.EtroConnector.GetBiS(c.Job))
            {
                ImGui.SameLine();
                if (ImGuiHelper.Button($"{Localize("character_bis_set_to", "Set to")} {name}##BIS#{c.Job}",
                        $"{etroId}", c.Bis.EtroId != etroId))
                    c.Bis.EtroId = etroId;
            }

            ImGui.EndDisabled();
            ImGui.NextColumn();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(150f * ScaleFactor);
            if (ImGui.InputInt($"{Localize("Level", "Level")}##{c.Job}", ref c.Level))
                c.Level = Math.Clamp(c.Level, 1, Common.Services.ServiceManager.GameInfo.CurrentExpansion.MaxLevel);

            ImGui.Separator();
            ImGui.NextColumn();
        }

        if (toDelete is not null) _playerCopy.MainChar.RemoveClass(toDelete.Value);

        ImGui.Columns(1);
        if (ImGuiHelper.SearchableCombo(Localize("Add Job", "Add Job"), out Job job, _newJob.ToString(),
                Enum.GetValues<Job>(), j => j.ToString(),
                (j, s) => j.ToString().Contains(s, StringComparison.CurrentCultureIgnoreCase),
                (j) => _playerCopy.MainChar.Classes.All(y => y.Job != j)))
            _newJob = job;
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddJob", "Add job"))
        {
            PlayableClass? newClass = _playerCopy.MainChar[_newJob];
            if (newClass == null)
            {
                newClass = _playerCopy.MainChar.AddClass(_newJob);
                ServiceManager.HrtDataManager.GearDb.AddSet(newClass.Gear);
                ServiceManager.HrtDataManager.GearDb.AddSet(newClass.Bis);
            }

            newClass.Bis.ManagedBy = GearSetManager.Etro;
            newClass.Bis.EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(_newJob);
        }
    }

    private void SavePlayer()
    {
        List<(Job, Func<bool>)> bisUpdates = new();
        //Player Data
        _player.NickName = _playerCopy.NickName;
        //Character Data
        if (_isNew)
        {
            if (!ServiceManager.HrtDataManager.CharDb.SearchCharacter(
                    _playerCopy.MainChar.HomeWorldId, _playerCopy.MainChar.Name, out Character? c))
            {
                c = new Character(_playerCopy.MainChar.Name, _playerCopy.MainChar.HomeWorldId);
                if (!ServiceManager.HrtDataManager.CharDb.TryAddCharacter(c))
                    return;
            }

            _player.MainChar = c;
            //Do not silently override existing characters
            if (c.Classes.Any())
                return;
        }

        //Copy safe data
        if (_player.MainChar.Name != _playerCopy.MainChar.Name ||
            _player.MainChar.HomeWorldId != _playerCopy.MainChar.HomeWorldId)
        {
            _player.MainChar.Name = _playerCopy.MainChar.Name;
            _player.MainChar.HomeWorldId = _playerCopy.MainChar.HomeWorldId;
            ServiceManager.HrtDataManager.CharDb.ReindexCharacter(_player.MainChar.LocalId);
        }

        _player.MainChar.TribeId = _playerCopy.MainChar.TribeId;
        _player.MainChar.MainJob = _playerCopy.MainChar.MainJob;
        //Remove classes that were removed in Ui
        for (int i = 0; i < _player.MainChar.Classes.Count(); i++)
        {
            PlayableClass c = _player.MainChar.Classes.ElementAt(i);
            if (_playerCopy.MainChar.Classes.Any(x => x.Job == c.Job)) continue;
            _player.MainChar.RemoveClass(c.Job);
            i--;
        }

        //Add missing classes and update Bis/Level for existing ones
        foreach (PlayableClass c in _playerCopy.MainChar.Classes)
        {
            PlayableClass? target = _player.MainChar[c.Job];
            GearDb gearSetDb = ServiceManager.HrtDataManager.GearDb;

            if (target == null)
            {
                target = _player.MainChar.AddClass(c.Job);
                gearSetDb.AddSet(target.Gear);
                gearSetDb.AddSet(target.Bis);
            }

            target.Level = c.Level;
            if (target.Bis.EtroId.Equals(c.Bis.EtroId))
                continue;
            if (!c.Bis.EtroId.Equals(""))
            {
                if (!gearSetDb.TryGetSetByEtroId(c.Bis.EtroId, out GearSet? etroSet))
                {
                    etroSet = new GearSet
                    {
                        ManagedBy = GearSetManager.Etro,
                        EtroId = c.Bis.EtroId,
                    };
                    gearSetDb.AddSet(etroSet);
                    ServiceManager.TaskManager.RegisterTask(
                        new HrtTask(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(etroSet), _callBack,
                            $"Update {etroSet.Name} ({etroSet.EtroId}) from etro"));
                }

                target.Bis = etroSet;
            }
            else if (!target.Bis.EtroId.IsNullOrEmpty())
            {
                GearSet bis = new()
                {
                    ManagedBy = GearSetManager.Hrt,
                };
                if (!gearSetDb.AddSet(bis))
                    ServiceManager.PluginLog.Debug("BiS not saved!");
                target.Bis = bis;
            }
        }

        if (_player.MainChar.Classes.All(c => c.Job != _player.MainChar.MainJob))
            _player.MainChar.MainJob = _player.MainChar.Classes.FirstOrDefault()?.Job;
        ServiceManager.HrtDataManager.Save();
    }
}

internal class EditGroupWindow : HrtWindow
{
    private readonly RaidGroup _group;
    private readonly RaidGroup _groupCopy;
    private readonly Action<RaidGroup> _onSave;
    private readonly Action<RaidGroup> _onCancel;

    internal EditGroupWindow(RaidGroup group, Action<RaidGroup>? onSave = null, Action<RaidGroup>? onCancel = null)
    {
        _group = group;
        _onSave = onSave ?? ((g) => { });
        _onCancel = onCancel ?? ((g) => { });
        _groupCopy = _group.Clone();
        Size = new Vector2(500, 150 + (group.RolePriority != null ? 180 : 0));
        Title = $"{Localize("Edit Group", "Edit Group")} {_group.Name}";
    }

    public override void Draw()
    {
        //Buttons
        if (ImGuiHelper.SaveButton())
        {
            _group.Name = _groupCopy.Name;
            _group.Type = _groupCopy.Type;
            _group.RolePriority = _groupCopy.RolePriority;
            _onSave(_group);
            Hide();
        }

        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
        {
            _onCancel(_group);
            Hide();
        }

        //Name + Type
        ImGui.InputText(Localize("Group Name", "Group Name"), ref _groupCopy.Name, 100);
        int groupType = (int)_groupCopy.Type;
        ImGui.BeginDisabled(_group.TypeLocked);
        if (ImGui.Combo(Localize("Group Type", "Group Type"), ref groupType, Enum.GetNames(typeof(GroupType)),
                Enum.GetNames(typeof(GroupType)).Length)) _groupCopy.Type = (GroupType)groupType;
        ImGui.EndDisabled();
        //Role priority
        bool overrideRolePriority = _groupCopy.RolePriority != null;
        if (ImGui.Checkbox(Localize("Override role priority", "Override role priority"), ref overrideRolePriority))
        {
            _groupCopy.RolePriority = overrideRolePriority ? new RolePriority() : null;
            if (Size.HasValue)
                Size = new Vector2(Size.Value.X, Size.Value.Y + (overrideRolePriority ? 1 : -1) * 180f * ScaleFactor);
        }

        if (overrideRolePriority)
        {
            ImGui.Text(Localize("ConfigRolePriority", "Priority to loot for each role (smaller is higher priority)"));
            ImGui.Text($"{Localize("Current priority", "Current priority")}: {_groupCopy.RolePriority}");
            _groupCopy.RolePriority!.DrawEdit(ImGui.InputInt);
        }
    }
}

internal class EditGearSetWindow : HrtWindowWithModalChild
{
    private GearSet _gearSet;
    private GearSet _gearSetCopy;
    private readonly Job _job;
    private readonly Action<GearSet> _onSave;

    internal EditGearSetWindow(GearSet original, Job job, Action<GearSet> onSave) : base()
    {
        _job = job;
        _gearSet = original;
        _gearSetCopy = original.Clone();
        _onSave = onSave;
        Title = $"{Localize("Edit", "Edit")} {original.Name}";
        MinSize = new Vector2(550, 300);
    }

    public override void Draw()
    {
        //Save/Cancel
        if (ImGuiHelper.SaveButton())
            Save();
        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
            Hide();
        bool isFromEtro = _gearSet.ManagedBy == GearSetManager.Etro;
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Trash, "deleteSet",
                Localize("GearsetEdit:Delete", "Remove set from this character (Hold Shift)"),
                ImGui.IsKeyDown(ImGuiKey.ModShift), new Vector2(50, 25)))
        {
            GearSet newSet = new();
            if (ServiceManager.HrtDataManager.GearDb.AddSet(newSet))
            {
                _gearSetCopy = newSet;
                Save();
            }
        }
        //Other infos

        if (isFromEtro)
        {
            ImGui.Text($"{Localize("GearSetEdit:Source", "Source")}: {Localize("GearSet:ManagedBy:Etro", "etro.gg")}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(Localize("GearSetEdit:MakeLocal", "Create local copy"),
                    Localize("GearSetEdit:MakeLocal:tooltip",
                        "Create a copy of this set in the local database. Set will not be updated from etro.gg afterwards\n" +
                        "Only local Gear Sets can be edited")))
            {
                _gearSet = _gearSet.Clone();
                _gearSet.LocalId = HrtId.Empty;
                _gearSet.RemoteIDs = new List<HrtId>();
                _gearSet.ManagedBy = GearSetManager.Hrt;
                _gearSet.EtroId = string.Empty;
                ServiceManager.HrtDataManager.GearDb.AddSet(_gearSet);
                _onSave(_gearSet);
                _gearSetCopy = _gearSet.Clone();
            }

            ImGui.Text($"{Localize("Etro ID", "Etro ID")}: {_gearSetCopy.EtroId}");
            ImGui.Text(
                $"{Localize("GearSetEdit:EtroLastDownload", "Last update check")}: {_gearSetCopy.EtroFetchDate}");
        }
        else
        {
            ImGui.Text(
                $"{Localize("GearSetEdit:Source", "Source")}: {Localize("GearSet:ManagedBy:HrtLocal", "Local Database")}");
            ImGui.Text($"{Localize("Local ID", "Local ID")}: {_gearSetCopy.LocalId}");
        }

        ImGui.Text($"{Localize("GearSetEdit:LastChanged", "Last Change")}: {_gearSetCopy.TimeStamp}");
        ImGui.BeginDisabled(isFromEtro);
        ImGui.Text($"{Localize("GearSetEdit:Name", "Name")}: ");
        ImGui.SameLine();
        ImGui.InputText("", ref _gearSetCopy.Name, 100);
        //Gear slots
        if (ImGui.BeginTable("GearEditTable", 2, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("GearL");
            ImGui.TableSetupColumn("GearR");
            DrawSlot(GearSetSlot.MainHand);
            if (_job.CanHaveShield())
                DrawSlot(GearSetSlot.OffHand);
            else
                ImGui.TableNextColumn();
            DrawSlot(GearSetSlot.Head);
            DrawSlot(GearSetSlot.Ear);
            DrawSlot(GearSetSlot.Body);
            DrawSlot(GearSetSlot.Neck);
            DrawSlot(GearSetSlot.Hands);
            DrawSlot(GearSetSlot.Wrist);
            DrawSlot(GearSetSlot.Legs);
            DrawSlot(GearSetSlot.Ring1);
            DrawSlot(GearSetSlot.Feet);
            DrawSlot(GearSetSlot.Ring2);
            ImGui.EndTable();
        }

        ImGui.EndDisabled();

        void DrawSlot(GearSetSlot slot)
        {
            ImGui.BeginDisabled(ChildIsOpen);
            ImGui.TableNextColumn();
            UiHelpers.DrawGearEdit(this, slot, _gearSetCopy[slot], i =>
            {
                foreach (HrtMateria mat in _gearSetCopy[slot].Materia)
                    i.AddMateria(mat);
                _gearSetCopy[slot] = i;
            }, _job);
            ImGui.EndDisabled();
        }
    }

    private void Save()
    {
        _gearSetCopy.TimeStamp = DateTime.Now;
        _gearSet.CopyFrom(_gearSetCopy);
        _onSave(_gearSet);
        ServiceManager.HrtDataManager.Save();
        Hide();
    }
}

internal abstract class SelectItemWindow<T> : HrtWindow where T : HrtItem
{
    protected static readonly Lumina.Excel.ExcelSheet<Item> Sheet = ServiceManager.DataManager.GetExcelSheet<Item>()!;
    protected T? Item = null;
    private readonly Action<T> _onSave;
    private readonly Action<T?> _onCancel;
    protected virtual bool CanSave { get; set; } = true;

    internal SelectItemWindow(Action<T> onSave, Action<T?> onCancel)
    {
        (_onSave, _onCancel) = (onSave, onCancel);
        Flags = ImGuiWindowFlags.NoCollapse;
    }


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
    private readonly bool _lockSlot = false;
    private IEnumerable<GearSetSlot> _slots;
    private readonly bool _lockJob = false;
    private Job? _job;
    private uint _minILvl;
    private uint _maxILvl;
    private List<Item> _items;
    protected override bool CanSave => false;

    public SelectGearItemWindow(Action<GearItem> onSave, Action<GearItem?> onCancel, GearItem? curentItem = null,
        GearSetSlot? slot = null, Job? job = null, uint maxItemLevel = 0) : base(onSave, onCancel)
    {
        Item = curentItem;
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
        Title = $"{Localize("Get item for", "Get item for")}" +
                $" {string.Join(',', _slots.Select((e, _) => e.FriendlyName()))}";
        _maxILvl = maxItemLevel;
        _minILvl = _maxILvl > 30 ? _maxILvl - 30 : 0;
        _items = ReevaluateItems();
    }

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
        if (ImGuiHelper.Combo("##slot", ref slot))
        {
            _slots = new[] { slot };
            ReevaluateItems();
        }

        ImGui.SameLine();
        ImGui.EndDisabled();
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        int min = (int)_minILvl;
        if (ImGui.InputInt("-##min", ref min, 5))
        {
            _minILvl = (uint)min;
            ReevaluateItems();
        }

        ImGui.SameLine();
        int max = (int)_maxILvl;
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        if (ImGui.InputInt("iLvL##Max", ref max, 5))
        {
            _maxILvl = (uint)max;
            ReevaluateItems();
        }

        //Draw item list
        foreach (Item item in _items)
        {
            bool isCurrentItem = item.RowId == Item?.Id;
            if (isCurrentItem)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{item.RowId}", Localize("Use this item", "Use this item"),
                    true, new Vector2(32f, 32f)))
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
            ImGui.Image(ServiceManager.IconCache.LoadIcon(item.Icon, item.CanBeHq).ImGuiHandle, new Vector2(32f, 32f));
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
                _slots.Any(slot => slot == GearSetSlot.None || x.EquipSlotCategory.Value.Contains(slot)))
            && (_maxILvl == 0 || x.LevelItem.Row <= _maxILvl)
            && x.LevelItem.Row >= _minILvl
            && (_job.GetValueOrDefault(0) == 0 || x.ClassJobCategory.Value.Contains(_job.GetValueOrDefault()))
        ).Take(50).ToList();
        _items.Sort((x, y) => (int)y.LevelItem.Row - (int)x.LevelItem.Row);
        return _items;
    }
}

internal class SelectMateriaWindow : SelectItemWindow<HrtMateria>
{
    private static readonly Dictionary<MateriaLevel, Dictionary<MateriaCategory, HrtMateria>> _allMateria;

    static SelectMateriaWindow()
    {
        _allMateria = new Dictionary<MateriaLevel, Dictionary<MateriaCategory, HrtMateria>>();
        foreach (MateriaLevel lvl in Enum.GetValues<MateriaLevel>())
        {
            Dictionary<MateriaCategory, HrtMateria> mats = new();
            foreach (MateriaCategory cat in Enum.GetValues<MateriaCategory>()) mats[cat] = new HrtMateria(cat, lvl);
            _allMateria[lvl] = mats;
        }
    }

    private readonly MateriaLevel _maxLvl;
    private readonly string _longestName;
    protected override bool CanSave => false;

    public SelectMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel, MateriaLevel maxMatLvl,
        MateriaLevel? matLevel = null) : base(onSave, onCancel)
    {
        _maxLvl = matLevel ?? maxMatLvl;
        Title = Localize("Select Materia", "Select Materia");
        _longestName = Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "";
        Size = new Vector2(ImGui.CalcTextSize(_longestName).X + 200f, 120f);
    }

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