using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

internal class EditPlayerWindow : HrtWindow
{
    private readonly Player Player;
    private readonly Player PlayerCopy;
    private readonly Action<HrtUiMessage> CallBack;
    private readonly bool IsNew;
    private Job newJob = Job.ADV;
    private readonly Func<Job, string> GetBisID;
    private const int ClassHeight = 27 * 2 + 4;
    internal EditPlayerWindow(Action<HrtUiMessage> callBack, Player p, Func<Job, string> getBisID)
        : base()
    {
        GetBisID = getBisID;
        CallBack = callBack;
        Player = p;
        PlayerCopy = new();
        PlayerCharacter? target = ServiceManager.TargetManager.Target as PlayerCharacter;
        IsNew = !Player.Filled;
        if (IsNew && target is not null)
        {
            PlayerCopy.MainChar.Name = target.Name.TextValue;
            PlayerCopy.MainChar.HomeWorldID = target.HomeWorld.Id;
            PlayerCopy.MainChar.MainJob = target.GetJob();
        }
        else if (Player.Filled)
        {
            PlayerCopy = Player.Clone();
        }
        Size = new Vector2(450, 330 + ClassHeight * PlayerCopy.MainChar.Classes.Count());
        Title = $"{Localize("Edit Player", "Edit Player")} {Player.NickName}";
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
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Player Data", "Player Data")).X) / 2f);
        ImGui.Text(Localize("Player Data", "Player Data"));
        ImGui.InputText(Localize("Player Name", "Player Name"), ref PlayerCopy.NickName, 50);
        //Character Data
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Character Data", "Character Data")).X) / 2f);
        ImGui.Text(Localize("Character Data", "Character Data"));
        if (ImGui.InputText(Localize("Character Name", "Character Name"), ref PlayerCopy.MainChar.Name, 50)
             && ServiceManager.CharacterInfoService.TryGetChar(out var pc, PlayerCopy.MainChar.Name))
        {
            PlayerCopy.MainChar.HomeWorld ??= pc?.HomeWorld.GameData;
        }
        if (ImGuiHelper.ExcelSheetCombo(Localize("Home World", "Home World") + "##" + Title, out World? w,
            x => PlayerCopy.MainChar.HomeWorld?.Name.RawString ?? "",
             x => x.Name.RawString, x => x.IsPublic))
        {
            PlayerCopy.MainChar.HomeWorld = w;
        }
        //ImGuiHelper.Combo(Localize("Gender", "Gender"), ref PlayerCopy.MainChar.Gender);
        string GetGenderedTribeName(Tribe? t) => (PlayerCopy.MainChar.Gender == Gender.Male ? t?.Masculine.RawString : t?.Feminine.RawString) ?? String.Empty;
        if (ImGuiHelper.ExcelSheetCombo(Localize("Tribe", "Tribe") + "##" + Title, out Tribe? t,
           _ => GetGenderedTribeName(PlayerCopy.MainChar.Tribe), GetGenderedTribeName))
        {
            PlayerCopy.MainChar.TribeID = t?.RowId ?? 0;
        }
        //Class Data
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Job Data", "Job Data")).X) / 2f);
        ImGui.Text(Localize("Job Data", "Job Data"));
        if (PlayerCopy.MainChar.Classes.Any())
        {
            if (!PlayerCopy.MainChar.Classes.Any(x => x.Job == PlayerCopy.MainChar.MainJob))
                PlayerCopy.MainChar.MainJob = PlayerCopy.MainChar.Classes.First().Job;
            if (ImGui.BeginCombo(Localize("Main Job", "Main Job"), PlayerCopy.MainChar.MainJob.ToString()))
            {
                foreach (var curJob in PlayerCopy.MainChar)
                {
                    if (ImGui.Selectable(curJob.Job.ToString()))
                        PlayerCopy.MainChar.MainJob = curJob.Job;
                }
            }
        }
        else
        {
            PlayerCopy.MainChar.MainJob = null;
            ImGui.NewLine();
            ImGui.Text(Localize("NoClasses", "Character does not have any classes created"));
        }
        ImGui.Columns(2, "Classes", false);
        ImGui.SetColumnWidth(0, 70f * ScaleFactor);
        ImGui.SetColumnWidth(1, 400f * ScaleFactor);
        ImGui.Separator();
        Job? toDelete = null;
        foreach (var c in PlayerCopy.MainChar.Classes)
        {
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"delete{c.Job}", $"Delete all data for {c.Job}"))
                toDelete = c.Job;
            ImGui.SameLine();
            ImGui.Text($"{c.Job}  ");
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(250f * ScaleFactor);
            bool localBis = !c.BIS.IsEmpty && c.BIS.ManagedBy == GearSetManager.HRT;
            ImGui.BeginDisabled(localBis);
            ImGui.InputText($"{Localize("BIS", "BIS")}##{c.Job}", ref c.BIS.EtroID, 50);
            if (localBis)
                ImGuiHelper.AddTooltip(Localize("PlayerEdit:Tooltip:LocalBis",
                    "BiS set for this class is locally managed.\nDelete local set to use a set from etro.gg"));
            if (!c.BIS.EtroID.Equals(GetBisID(c.Job)))
            {
                ImGui.SameLine();
                if (ImGuiHelper.Button($"{Localize("Set to default", "Set to default")}##BIS#{c.Job}",
                    Localize("DefaultBiSTooltip", "Fetch default BiS from configuration")))
                    c.BIS.EtroID = GetBisID(c.Job);
            }
            ImGui.EndDisabled();
            ImGui.NextColumn();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(150f * ScaleFactor);
            if (ImGui.InputInt($"{Localize("Level", "Level")}##{c.Job}", ref c.Level))
            {
                c.Level = Math.Clamp(c.Level, 1, Common.Services.ServiceManager.GameInfo.CurrentExpansion.MaxLevel);
            }

            ImGui.Separator();
            ImGui.NextColumn();
        }
        if (toDelete is not null)
        {
            PlayerCopy.MainChar.RemoveClass(toDelete.Value);
            if (Size.HasValue)
                Size = new(Size.Value.X, Size.Value.Y - ClassHeight);
        }

        ImGui.Columns(1);
        if (ImGuiHelper.SearchableCombo(Localize("Add Job", "Add Job"), out var job, newJob.ToString(),
            Enum.GetValues<Job>(), j => j.ToString(),
            (j, s) => j.ToString().Contains(s, StringComparison.CurrentCultureIgnoreCase),
            (t) => !PlayerCopy.MainChar.Classes.Any(y => y.Job == t)))
        {
            newJob = job;
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddJob", "Add job"))
        {
            var newClass = PlayerCopy.MainChar[newJob];
            if (newClass == null)
            {
                newClass = PlayerCopy.MainChar.AddClass(newJob);
                ServiceManager.HrtDataManager.GearDB.AddSet(newClass.Gear);
                ServiceManager.HrtDataManager.GearDB.AddSet(newClass.BIS);
            }
            newClass.BIS.EtroID = GetBisID(newJob);
            if (Size.HasValue)
                Size = new(Size.Value.X, Size.Value.Y + ClassHeight);
        }
    }
    private void SavePlayer()
    {
        List<(Job, Func<bool>)> bisUpdates = new();
        //Player Data
        Player.NickName = PlayerCopy.NickName;
        //Character Data
        if (IsNew)
        {
            if (!ServiceManager.HrtDataManager.CharDB.SearchCharacter(
                PlayerCopy.MainChar.HomeWorldID, PlayerCopy.MainChar.Name, out Character? c))
            {
                c = new(PlayerCopy.MainChar.Name, PlayerCopy.MainChar.HomeWorldID);
                if (!ServiceManager.HrtDataManager.CharDB.TryAddCharacter(c))
                    return;
            }
            Player.MainChar = c;
            //Do not silently override existing characters
            if (c.Classes.Any())
                return;
        }
        //Copy safe data
        if (Player.MainChar.Name != PlayerCopy.MainChar.Name ||
            Player.MainChar.HomeWorldID != PlayerCopy.MainChar.HomeWorldID)
        {
            Player.MainChar.Name = PlayerCopy.MainChar.Name;
            Player.MainChar.HomeWorldID = PlayerCopy.MainChar.HomeWorldID;
            ServiceManager.HrtDataManager.CharDB.ReindexCharacter(Player.MainChar.LocalID);
        }
        Player.MainChar.TribeID = PlayerCopy.MainChar.TribeID;
        Player.MainChar.MainJob = PlayerCopy.MainChar.MainJob;
        //Remove classes that were removed in Ui
        for (int i = 0; i < Player.MainChar.Classes.Count(); i++)
        {
            var c = Player.MainChar.Classes.ElementAt(i);
            if (!PlayerCopy.MainChar.Classes.Any(x => x.Job == c.Job))
            {
                Player.MainChar.RemoveClass(c.Job);
                i--;
            }
        }
        //Add missing classes and update Bis/Level for existing ones
        foreach (var c in PlayerCopy.MainChar.Classes)
        {
            var target = Player.MainChar[c.Job];
            var gearSetDB = ServiceManager.HrtDataManager.GearDB;
            if (target == null)
            {
                target = Player.MainChar.AddClass(c.Job);
                gearSetDB.AddSet(target.Gear);
                gearSetDB.AddSet(target.BIS);
            }
            target.Level = c.Level;
            if (target.BIS.EtroID.Equals(c.BIS.EtroID))
                continue;
            if (!c.BIS.EtroID.Equals(""))
            {
                if (!gearSetDB.TryGetSetByEtroID(c.BIS.EtroID, out var etroSet))
                {
                    etroSet = new()
                    {
                        ManagedBy = GearSetManager.Etro,
                        EtroID = c.BIS.EtroID
                    };
                    gearSetDB.AddSet(etroSet);
                    ServiceManager.TaskManager.RegisterTask(new(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(target.BIS), CallBack));
                }
                target.BIS = etroSet;
            }
            else if (!target.BIS.EtroID.IsNullOrEmpty())
            {
                GearSet bis = new()
                {
                    ManagedBy = GearSetManager.HRT
                };
                if (!gearSetDB.AddSet(bis))
                    PluginLog.Debug("BiS not saved!");
                target.BIS = bis;
            }

        }
        if (!Player.MainChar.Classes.Any(c => c.Job == Player.MainChar.MainJob))
            Player.MainChar.MainJob = Player.MainChar.Classes.FirstOrDefault()?.Job;
        ServiceManager.HrtDataManager.Save();
    }
}
internal class EditGroupWindow : HrtWindow
{
    private readonly RaidGroup Group;
    private readonly RaidGroup GroupCopy;
    private readonly Action<RaidGroup> OnSave;
    private readonly Action<RaidGroup> OnCancel;

    internal EditGroupWindow(RaidGroup group, Action<RaidGroup>? onSave = null, Action<RaidGroup>? onCancel = null)
    {
        Group = group;
        OnSave = onSave ?? ((g) => { });
        OnCancel = onCancel ?? ((g) => { });
        GroupCopy = Group.Clone();
        Size = new Vector2(500, 150 + (group.RolePriority != null ? 180 : 0));
        Title = $"{Localize("Edit Group", "Edit Group")} {Group.Name}";
    }

    public override void Draw()
    {
        //Buttons
        if (ImGuiHelper.SaveButton())
        {
            Group.Name = GroupCopy.Name;
            Group.Type = GroupCopy.Type;
            Group.RolePriority = GroupCopy.RolePriority;
            OnSave(Group);
            Hide();
        }
        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
        {
            OnCancel(Group);
            Hide();
        }
        //Name + Type
        ImGui.InputText(Localize("Group Name", "Group Name"), ref GroupCopy.Name, 100);
        int groupType = (int)GroupCopy.Type;
        if (ImGui.Combo(Localize("Group Type", "Group Type"), ref groupType, Enum.GetNames(typeof(GroupType)), Enum.GetNames(typeof(GroupType)).Length))
        {
            GroupCopy.Type = (GroupType)groupType;
        }
        //Role priority
        bool overrideRolePriority = GroupCopy.RolePriority != null;
        if (ImGui.Checkbox(Localize("Override role priority", "Override role priority"), ref overrideRolePriority))
        {
            GroupCopy.RolePriority = overrideRolePriority ? new RolePriority() : null;
            if (Size.HasValue)
                Size = new(Size.Value.X, Size.Value.Y + (overrideRolePriority ? 1 : -1) * 180f * ScaleFactor);
        }
        if (overrideRolePriority)
        {
            ImGui.Text(Localize("ConfigRolePriority", "Priority to loot for each role (smaller is higher priority)"));
            ImGui.Text($"{Localize("Current priority", "Current priority")}: {GroupCopy.RolePriority}");
            GroupCopy.RolePriority!.DrawEdit(ImGui.InputInt);
        }
    }
}
internal class EditGearSetWindow : HRTWindowWithModalChild
{
    private readonly GearSet _gearSet;
    private readonly GearSet _gearSetCopy;
    private readonly Job _job;

    internal EditGearSetWindow(GearSet original, Job job) : base()
    {
        _job = job;
        _gearSet = original;
        _gearSetCopy = original.Clone();
        Title = $"{Localize("Edit", "Edit")} {original.Name}";
        MinSize = new(550, 300);
    }

    public override void Draw()
    {
        //Save/Cancel
        if (ImGuiHelper.SaveButton())
            Save();
        ImGui.SameLine();
        if (ImGuiHelper.CancelButton())
            Hide();
        //OTher infos
        bool isFromEtro = _gearSet.ManagedBy == GearSetManager.Etro;
        ImGui.Text($"{Localize("GearSetEdit:Source", "Source")}: {(isFromEtro ? Localize("GearSet:ManagedBy:Etro", "etro.gg")
            : Localize("GearSet:ManagedBy:HrtLocal", "Local Database"))}");
        if (isFromEtro)
        {
            ImGui.SameLine();
            if (ImGuiHelper.Button(Localize("GearSetEdit:MakeLocal", "Create local copy"),
                Localize("GearSetEdit:MakeLocal:tooltip",
                "Create a copy of this set inthe local databasse. Set will not be updated from etro.gg afterwads\n" +
                "Only local gearsets can be edited")))
            {
                //TODO: actual copy
            }
            ImGui.Text($"{Localize("Etro ID", "Etro ID")}: {_gearSetCopy.EtroID}");
            ImGui.Text($"{Localize("GearSetEdit:EtroLastDownload", "Last update check")}: {_gearSetCopy.EtroFetchDate:d}");
        }
        ImGui.Text($"{Localize("GearSetEdit:LastChanged", "Last Change")}: {_gearSetCopy.TimeStamp:d}");
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
                foreach (var mat in _gearSetCopy[slot].Materia)
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
        ServiceManager.HrtDataManager.Save();
        Hide();
    }
}
internal abstract class SelectItemWindow<T> : HrtWindow where T : HrtItem
{
    protected static readonly Lumina.Excel.ExcelSheet<Item> Sheet = ServiceManager.DataManager.GetExcelSheet<Item>()!;
    protected T? Item = null;
    private readonly Action<T> OnSave;
    private readonly Action<T?> OnCancel;
    protected virtual bool CanSave { get; set; } = true;
    internal SelectItemWindow(Action<T> onSave, Action<T?> onCancel)
    {
        (OnSave, OnCancel) = (onSave, onCancel);
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
            OnSave(Item);
        else
            OnCancel(Item);
        Hide();
    }
    protected void Cancel()
    {
        OnCancel(Item);
        Hide();
    }

    protected abstract void DrawItemSelection();
}
internal class SelectGearItemWindow : SelectItemWindow<GearItem>
{
    private readonly bool _lockSlot = false;
    private IEnumerable<GearSetSlot> Slots;
    private readonly bool _lockJob = false;
    private Job? Job;
    private uint minILvl;
    private uint maxILvl;
    private List<Item> _items;
    protected override bool CanSave => false;
    public SelectGearItemWindow(Action<GearItem> onSave, Action<GearItem?> onCancel, GearItem? curentItem = null, GearSetSlot? slot = null, Job? job = null, uint maxItemLevel = 0) : base(onSave, onCancel)
    {
        Item = curentItem;
        if (slot.HasValue)
        {
            _lockSlot = true;
            Slots = new[] { slot.Value };
        }
        else
        {
            Slots = Item?.Slots ?? Array.Empty<GearSetSlot>();
        }
        _lockJob = job.HasValue;
        Job = job;
        Title = $"{Localize("Get item for", "Get item for")}" +
            $" {string.Join(',', Slots.Select((e, _) => e.FriendlyName()))}";
        maxILvl = maxItemLevel;
        minILvl = maxILvl > 30 ? maxILvl - 30 : 0;
        _items = reevaluateItems();
    }

    protected override void DrawItemSelection()
    {
        //Draw selection bar
        ImGui.SetNextItemWidth(65f * ScaleFactor);
        ImGui.BeginDisabled(_lockJob);
        if (ImGuiHelper.Combo("##job", ref Job))
            reevaluateItems();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(125f * ScaleFactor);
        ImGui.BeginDisabled(_lockSlot);
        var slot = Slots.FirstOrDefault(GearSetSlot.None);
        if (ImGuiHelper.Combo("##slot", ref slot))
        {
            Slots = new[] { slot };
            reevaluateItems();
        }
        ImGui.SameLine();
        ImGui.EndDisabled();
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        int min = (int)minILvl;
        if (ImGui.InputInt("-##min", ref min, 5))
        {
            minILvl = (uint)min;
            reevaluateItems();
        }
        ImGui.SameLine();
        int max = (int)maxILvl;
        ImGui.SetNextItemWidth(100f * ScaleFactor);
        if (ImGui.InputInt("iLvL##Max", ref max, 5))
        {
            maxILvl = (uint)max;
            reevaluateItems();
        }
        //Draw item list
        foreach (var item in _items)
        {
            bool isCurrentItem = item.RowId == Item?.ID;
            if (isCurrentItem)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{item.RowId}", Localize("Use this item", "Use this item"), true, new Vector2(32f, 32f)))
            {
                if (isCurrentItem)
                    Cancel();
                else
                    Save(new(item.RowId) { IsHq = item.CanBeHq });
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
    private List<Item> reevaluateItems()
    {
        _items = Sheet.Where(x =>
            x.ClassJobCategory.Row != 0
            && (!Slots.Any() || Slots.Any(slot => slot == GearSetSlot.None || x.EquipSlotCategory.Value.Contains(slot)))
            && (maxILvl == 0 || x.LevelItem.Row <= maxILvl)
            && x.LevelItem.Row >= minILvl
            && (Job.GetValueOrDefault(0) == 0 || x.ClassJobCategory.Value.Contains(Job.GetValueOrDefault()))
            ).Take(50).ToList();
        _items.Sort((x, y) => (int)y.LevelItem.Row - (int)x.LevelItem.Row);
        return _items;
    }
}
internal class SelectMateriaWindow : SelectItemWindow<HrtMateria>
{
    private static readonly Dictionary<MateriaLevel, Dictionary<MateriaCategory, HrtMateria>> AllMateria;

    static SelectMateriaWindow()
    {
        AllMateria = new();
        foreach (MateriaLevel lvl in Enum.GetValues<MateriaLevel>())
        {
            Dictionary<MateriaCategory, HrtMateria> mats = new();
            foreach (MateriaCategory cat in Enum.GetValues<MateriaCategory>())
            {
                mats[cat] = new HrtMateria(cat, lvl);
            }
            AllMateria[lvl] = mats;
        }
    }
    private readonly MateriaLevel MaxLvl;
    private readonly string longestName;
    protected override bool CanSave => false;
    public SelectMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel, MateriaLevel maxMatLvl, MateriaLevel? matLevel = null) : base(onSave, onCancel)
    {
        MaxLvl = matLevel ?? maxMatLvl;
        Title = Localize("Select Materia", "Select Materia");
        longestName = Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "";
        Size = new(ImGui.CalcTextSize(longestName).X + 200f, 120f);
    }

    protected override void DrawItemSelection()
    {
        //1 Row per Level
        for (MateriaLevel lvl = MaxLvl; lvl != MateriaLevel.None; --lvl)
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
            var mat = AllMateria[lvl][cat];
            var item = mat.Item;
            if (item != null)
            {
                if (ImGui.ImageButton(ServiceManager.IconCache[item.Icon].ImGuiHandle, new(32)))
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
}
