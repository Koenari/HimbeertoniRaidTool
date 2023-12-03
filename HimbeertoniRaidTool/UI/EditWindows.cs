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

internal abstract class EditWindow<T> : HrtWindowWithModalChild where T : IHasHrtId
{
    private static HrtDataManager DataManager => ServiceManager.HrtDataManager;
    public static EditWindow<S>? GetEditWindow<S>(HrtId id, Action<S>? onSave = null, Action<S>? onCancel = null)
        where S : IHasHrtId
    {
        Type type = typeof(S);
        switch (type)
        {
            case not null when type == typeof(RaidGroup):
                if (DataManager.RaidGroupDb.TryGet(id, out RaidGroup? group))
                    return new EditGroupWindow(group, onSave as Action<RaidGroup>, onCancel as Action<RaidGroup>) as
                        EditWindow<S>;
                break;
            case not null when type == typeof(Player):
                if (DataManager.PlayerDb.TryGet(id, out Player? player))
                    return new EditPlayerWindow(player, onSave as Action<Player>, onCancel as Action<Player>) as
                        EditWindow<S>;
                break;
            case not null when type == typeof(Character):
                if (DataManager.CharDb.TryGet(id, out Character? character))
                    return new EditCharacterWindow(character, onSave as Action<Character>,
                        onCancel as Action<Character>) as EditWindow<S>;
                break;
            case not null when type == typeof(GearSet):
                if (DataManager.GearDb.TryGet(id, out GearSet? gearSet))
                    return new EditGearSetWindow(gearSet, null, onSave as Action<GearSet>, onCancel as Action<GearSet>)
                        as EditWindow<S>;
                break;
            default:
                return null;
        }
        return null;
    }

    private T _original;
    protected T DataCopy;

    private readonly Action<T>? _onSave;
    private readonly Action<T>? _onCancel;

    protected EditWindow(T original, Action<T>? onSave, Action<T>? onCancel)
    {
        _original = original;
        DataCopy = _original.Clone();
        _onCancel = onCancel;
        _onSave = onSave;
    }
    public override sealed void Draw()
    {
        ImGui.Text($"{Localize("EditWindow:LocalId", "Local ID")}: {_original.LocalId}");
        //Buttons
        if (ImGuiHelper.SaveButton())
        {
            Save(_original);
            _onSave?.Invoke(_original);
            Hide();
        }

        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
        {
            Cancel();
            _onCancel?.Invoke(_original);
            Hide();
        }
        DrawContent();
    }
    protected abstract void DrawContent();
    protected void Save() => Save(_original);

    protected void ReplaceOriginal(T newOrg)
    {
        _original = newOrg;
        DataCopy = _original.Clone();
    }

    protected abstract void Save(T destination);
    protected abstract void Cancel();
}

internal class EditGroupWindow : EditWindow<RaidGroup>
{

    internal EditGroupWindow(RaidGroup group, Action<RaidGroup>? onSave = null, Action<RaidGroup>? onCancel = null) :
        base(group, onSave, onCancel)
    {
        Size = new Vector2(500, 170 + (group.RolePriority != null ? 180 : 0));
        SizeCondition = ImGuiCond.Appearing;
        Title = $"{Localize("Edit Group", "Edit Group")} {DataCopy.Name}";
    }

    protected override void Save(RaidGroup destination)
    {
        destination.Name = DataCopy.Name;
        destination.Type = DataCopy.Type;
        destination.RolePriority = DataCopy.RolePriority;
    }
    protected override void Cancel() { }

    protected override void DrawContent()
    {
        //Name + Type
        ImGui.InputText(Localize("Group Name", "Group Name"), ref DataCopy.Name, 100);
        int groupType = (int)DataCopy.Type;
        ImGui.BeginDisabled(DataCopy.TypeLocked);
        if (ImGui.Combo(Localize("Group Type", "Group Type"), ref groupType, Enum.GetNames<GroupType>(),
                Enum.GetNames<GroupType>().Length)) DataCopy.Type = (GroupType)groupType;
        ImGui.EndDisabled();
        //Role priority
        bool overrideRolePriority = DataCopy.RolePriority != null;
        if (ImGui.Checkbox(Localize("Override role priority", "Override role priority"), ref overrideRolePriority))
        {
            DataCopy.RolePriority = overrideRolePriority ? new RolePriority() : null;
            Vector2 curSize = ImGui.GetWindowSize();
            Resize(curSize with
            {
                Y = curSize.Y + (overrideRolePriority ? 1 : -1) * 180f * ScaleFactor,
            });
        }

        if (overrideRolePriority)
        {
            ImGui.Text(Localize("ConfigRolePriority", "Priority to loot for each role (smaller is higher priority)"));
            ImGui.Text($"{Localize("Current priority", "Current priority")}: {DataCopy.RolePriority}");
            DataCopy.RolePriority!.DrawEdit(ImGui.InputInt);
        }
    }
}

internal class EditPlayerWindow : EditWindow<Player>
{
    internal EditPlayerWindow(Player p, Action<Player>? onSave = null, Action<Player>? onCancel = null)
        : base(p, onSave, onCancel)
    {
        Size = new Vector2(450, 250);
        SizeCondition = ImGuiCond.Appearing;
        Title = $"{Localize("Edit Player", "Edit Player")} {DataCopy.NickName}";
    }

    protected override void Cancel() { }

    protected override void DrawContent()
    {
        //Player Data
        ImGui.SetCursorPosX(
            (ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Player Data", "Player Data")).X) / 2f);
        ImGui.Text(Localize("Player Data", "Player Data"));
        ImGui.InputText(Localize("Player Name", "Player Name"), ref DataCopy.NickName, 50);
        //Character Data
        ImGui.SetCursorPosX(
            (ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Character Data", "Character Data")).X) / 2f);
        ImGui.Text(Localize("Character Data", "Character Data"));
        Character mainChar = DataCopy.MainChar;
        foreach (Character character in DataCopy.Characters)
        {
            ImGui.PushID(character.LocalId.ToString());
            if (ImGuiHelper.Button(FontAwesomeIcon.Trash, "delete",
                    $"{Localize("Remove")} {character.Name} from {DataCopy.NickName}",
                    ImGui.IsKeyDown(ImGuiKey.ModShift)))
                DataCopy.RemoveCharacter(character);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "edit",
                    $"{Localize("Edit")} {character.Name} @ {character.HomeWorld?.Name}"))
            {
                var window = GetEditWindow<Character>(character.LocalId);
                if (window != null)
                    AddChild(window);
            }
            ImGui.SameLine();
            bool isMain = mainChar.Equals(character);
            if (isMain) ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            if (ImGuiHelper.Button(
                    isMain ? Localize("edit:player:button:charIsMain", "Is Main")
                        : Localize("edit:player:button:charMakeMain", "Make Main"), null))
                DataCopy.MainChar = character;
            if (isMain) ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text($"{character.Name} @ {character.HomeWorld?.Name}");
            ImGui.PopID();
        }
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "addEmpty",
                Localize("edit:player:button:addEmptyChar:tooltip", "Add custom character")))
        {
            var character = new Character();
            AddChild(new EditCharacterWindow(character, c =>
            {
                if (ServiceManager.HrtDataManager.CharDb.TryAdd(c))
                    DataCopy.AddCharacter(c);
            }));
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Search, "addFromDb",
                Localize("edit:player:button:addCharFRomDb:tooltip", "Add existing character")))
        {
            AddChild(ServiceManager.HrtDataManager.CharDb.OpenSearchWindow(c => DataCopy.AddCharacter(c)));
        }
    }

    protected override void Save(Player destination)
    {
        if (destination.LocalId.IsEmpty)
        {
            ServiceManager.HrtDataManager.PlayerDb.TryAdd(destination);
        }
        //Player Data
        destination.NickName = DataCopy.NickName;
        //Characters
        var toRemove = destination.Characters.Where(c => !DataCopy.Characters.Contains(c));
        foreach (Character c in toRemove)
            destination.RemoveCharacter(c);
        var toAdd = DataCopy.Characters.Where(c => !destination.Characters.Contains(c));
        foreach (Character c in toAdd)
            destination.AddCharacter(c);
        destination.MainChar = DataCopy.MainChar;
    }
}

internal class EditCharacterWindow : EditWindow<Character>
{
    private Job _newJob = Job.ADV;
    private const int CLASS_HEIGHT = 27 * 2 + 4;
    internal EditCharacterWindow(Character character, Action<Character>? onSave = null,
        Action<Character>? onCancel = null) : base(character, onSave, onCancel)
    {
        Size = new Vector2(750, 270 + CLASS_HEIGHT * DataCopy.Classes.Count());
        SizeCondition = ImGuiCond.Appearing;
        Title = $"{Localize("Edit")} {Localize("character")} {DataCopy.Name}";
    }

    protected override void Cancel() { }

    protected override void DrawContent()
    {
        ImGui.SetCursorPosX(
            (ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Character Data", "Character Data")).X) / 2f);
        ImGui.Text(Localize("Character Data", "Character Data"));
        if (ImGui.InputText(Localize("Character Name", "Character Name"), ref DataCopy.Name, 50)
            && ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? pc, DataCopy.Name))
            DataCopy.HomeWorld ??= pc.HomeWorld.GameData;
        if (ImGuiHelper.ExcelSheetCombo(Localize("Home World", "Home World") + "##" + Title, out World? w,
                _ => DataCopy.HomeWorld?.Name.RawString ?? "",
                x => x.Name.RawString, x => x.IsPublic))
            DataCopy.HomeWorld = w;

        //ImGuiHelper.Combo(Localize("Gender", "Gender"), ref PlayerCopy.MainChar.Gender);
        string GetGenderedTribeName(Tribe? tribe)
        {
            return (DataCopy.Gender == Gender.Male ? tribe?.Masculine.RawString : tribe?.Feminine.RawString) ??
                   string.Empty;
        }

        if (ImGuiHelper.ExcelSheetCombo(Localize("Tribe", "Tribe") + "##" + Title, out Tribe? t,
                _ => GetGenderedTribeName(DataCopy.Tribe), GetGenderedTribeName))
            DataCopy.TribeId = t.RowId;
        //Class Data
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Job Data", "Job Data")).X) / 2f);
        ImGui.Text(Localize("Job Data", "Job Data"));
        if (DataCopy.Classes.Any())
        {
            if (DataCopy.Classes.All(x => x.Job != DataCopy.MainJob))
                DataCopy.MainJob = DataCopy.Classes.First().Job;
            if (ImGui.BeginCombo(Localize("Main Job", "Main Job"), DataCopy.MainJob.ToString()))
                foreach (PlayableClass curJob in DataCopy)
                    if (ImGui.Selectable(curJob.Job.ToString()))
                        DataCopy.MainJob = curJob.Job;
        }
        else
        {
            DataCopy.MainJob = null;
            ImGui.NewLine();
            ImGui.Text(Localize("NoClasses", "Character does not have any classes created"));
        }

        ImGui.Columns(2, "Classes", false);
        ImGui.SetColumnWidth(0, 70f * ScaleFactor);
        ImGui.Separator();
        Job? toDelete = null;
        foreach (PlayableClass c in DataCopy.Classes)
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

        if (toDelete is not null) DataCopy.RemoveClass(toDelete.Value);

        ImGui.Columns(1);
        if (ImGuiHelper.SearchableCombo(Localize("Add Job", "Add Job"), out Job job, _newJob.ToString(),
                Enum.GetValues<Job>(), j => j.ToString(),
                (j, s) => j.ToString().Contains(s, StringComparison.CurrentCultureIgnoreCase),
                (j) => DataCopy.Classes.All(y => y.Job != j)))
            _newJob = job;
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddJob", "Add job"))
        {
            PlayableClass? newClass = DataCopy[_newJob];
            if (newClass == null)
            {
                newClass = DataCopy.AddClass(_newJob);
                ServiceManager.HrtDataManager.GearDb.TryAdd(newClass.Gear);
                ServiceManager.HrtDataManager.GearDb.TryAdd(newClass.Bis);
            }

            newClass.Bis.ManagedBy = GearSetManager.Etro;
            newClass.Bis.EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(_newJob);
        }
    }

    protected override void Save(Character destination)
    {
        //Copy safe data
        if (destination.Name != DataCopy.Name ||
            destination.HomeWorldId != DataCopy.HomeWorldId)
        {
            destination.Name = DataCopy.Name;
            destination.HomeWorldId = DataCopy.HomeWorldId;
            ServiceManager.HrtDataManager.CharDb.ReindexCharacter(destination.LocalId);
        }

        destination.TribeId = DataCopy.TribeId;
        destination.MainJob = DataCopy.MainJob;
        //Remove classes that were removed in Ui
        for (int i = 0; i < destination.Classes.Count(); i++)
        {
            PlayableClass c = destination.Classes.ElementAt(i);
            if (DataCopy.Classes.Any(x => x.Job == c.Job)) continue;
            destination.RemoveClass(c.Job);
            i--;
        }

        //Add missing classes and update Bis/Level for existing ones
        foreach (PlayableClass c in DataCopy.Classes)
        {
            PlayableClass? target = destination[c.Job];
            GearDb gearSetDb = ServiceManager.HrtDataManager.GearDb;

            if (target == null)
            {
                target = destination.AddClass(c.Job);
                gearSetDb.TryAdd(target.Gear);
                gearSetDb.TryAdd(target.Bis);
            }

            target.Level = c.Level;
            if (target.Bis.EtroId.Equals(c.Bis.EtroId))
                continue;
            if (!c.Bis.EtroId.Equals(""))
            {
                if (!gearSetDb.TryGetSetByEtroId(c.Bis.EtroId, out GearSet? etroSet))
                {
                    etroSet = new GearSet(GearSetManager.Etro)
                    {
                        EtroId = c.Bis.EtroId,
                    };
                    gearSetDb.TryAdd(etroSet);
                    ServiceManager.TaskManager.RegisterTask(
                        new HrtTask(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(etroSet), _ => { },
                            $"Update {etroSet.Name} ({etroSet.EtroId}) from etro"));
                }

                target.Bis = etroSet;
            }
            else if (!target.Bis.EtroId.IsNullOrEmpty())
            {
                GearSet bis = new(GearSetManager.Hrt, "BiS");
                if (!gearSetDb.TryAdd(bis))
                    ServiceManager.PluginLog.Debug("BiS not saved!");
                target.Bis = bis;
            }
        }

        if (destination.Classes.All(c => c.Job != destination.MainJob))
            destination.MainJob = destination.Classes.FirstOrDefault()?.Job;
        ServiceManager.HrtDataManager.Save();
    }

}

internal class EditGearSetWindow : EditWindow<GearSet>
{
    private readonly Job _job;

    internal EditGearSetWindow(GearSet original, Job? job = null, Action<GearSet>? onSave = null,
        Action<GearSet>? onCancel = null) :
        base(original, onSave, onCancel)
    {
        _job = job ?? original[GearSetSlot.MainHand].Jobs.First();
        Title = $"{Localize("GearsetEdit:Title", "Edit gear set")}: {original.Name}";
        MinSize = new Vector2(550, 300);
    }

    protected override void Cancel() { }

    protected override void DrawContent()
    {
        bool isFromEtro = DataCopy.ManagedBy == GearSetManager.Etro;
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Trash, "deleteSet",
                Localize("GearsetEdit:Delete", "Remove set from this character (Hold Shift)"),
                ImGui.IsKeyDown(ImGuiKey.ModShift), new Vector2(50, 25)))
        {
            GearSet newSet = new();
            if (ServiceManager.HrtDataManager.GearDb.TryAdd(newSet))
            {
                DataCopy = newSet;
                Save();
                Hide();
            }
        }
        //Other infos

        if (isFromEtro)
        {
            ImGui.Text($"{Localize("GearSetEdit:Source", "Source")}: {Localize("GearSet:ManagedBy:Etro", "etro.gg")}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(Localize("GearSetEdit:MakeLocal", "Create local copy"),
                    Localize("GearSetEdit:MakeLocal:tooltip",
                        "Create a copy of this set in the local database. Set will not be updated from etro.gg afterwards\n"
                        +
                        "Only local Gear Sets can be edited")))
            {
                GearSet newSet = DataCopy.Clone();
                newSet.LocalId = HrtId.Empty;
                newSet.RemoteIDs = new List<HrtId>();
                newSet.ManagedBy = GearSetManager.Hrt;
                newSet.EtroId = string.Empty;
                ServiceManager.HrtDataManager.GearDb.TryAdd(newSet);
                ReplaceOriginal(newSet);
            }

            ImGui.Text($"{Localize("Etro ID", "Etro ID")}: {DataCopy.EtroId}");
            ImGui.Text(
                $"{Localize("GearSetEdit:EtroLastDownload", "Last update check")}: {DataCopy.EtroFetchDate}");
        }
        else
        {
            ImGui.Text(
                $"{Localize("GearSetEdit:Source", "Source")}: {Localize("GearSet:ManagedBy:HrtLocal", "Local Database")}");
            ImGui.Text($"{Localize("Local ID", "Local ID")}: {DataCopy.LocalId}");
        }

        ImGui.Text($"{Localize("GearSetEdit:LastChanged", "Last Change")}: {DataCopy.TimeStamp}");
        ImGui.BeginDisabled(isFromEtro);
        ImGui.Text($"{Localize("GearSetEdit:Name", "Name")}: ");
        ImGui.SameLine();
        ImGui.InputText("", ref DataCopy.Name, 100);
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
            UiHelpers.DrawGearEdit(this, slot, DataCopy[slot], i =>
            {
                foreach (HrtMateria mat in DataCopy[slot].Materia)
                    i.AddMateria(mat);
                DataCopy[slot] = i;
            }, _job);
            ImGui.EndDisabled();
        }
    }

    protected override void Save(GearSet destination)
    {
        DataCopy.TimeStamp = DateTime.Now;
        destination.CopyFrom(DataCopy);
        ServiceManager.HrtDataManager.Save();
    }
}

internal abstract class SelectItemWindow<T> : HrtWindow where T : HrtItem
{
    // ReSharper disable once StaticMemberInGenericType
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
    protected override bool CanSave => false;

    public SelectMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel, MateriaLevel maxMatLvl,
        MateriaLevel? matLevel = null) : base(onSave, onCancel)
    {
        _maxLvl = matLevel ?? maxMatLvl;
        Title = Localize("Select Materia", "Select Materia");
        string longestName = Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "";
        Size = new Vector2(ImGui.CalcTextSize(longestName).X + 200f, 120f);
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