using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ColorHelper;
using Dalamud.Interface;
using HimbeertoniRaidTool.Connectors;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
using Lumina.Excel.Extensions;
using Lumina.Excel.GeneratedSheets;
using static ColorHelper.HRTColorConversions;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    internal class EditPlayerWindow : HrtUI
    {
        private readonly RaidGroup RaidGroup;
        private readonly Player Player;
        private readonly Player PlayerCopy;
        private readonly AsyncTaskWithUiResult CallBack;
        private readonly bool IsNew;
        private static readonly string[] Worlds;
        private static readonly uint[] WorldIDs;
        private int newJob = 0;

        internal PositionInRaidGroup Pos => Player.Pos;

        static EditPlayerWindow()
        {
            List<(uint, string)> WorldList = new();
            for (uint i = 21; i < (Services.DataManager.GetExcelSheet<World>()?.RowCount ?? 0); i++)
            {
                string? worldName = Services.DataManager.GetExcelSheet<World>()?.GetRow(i)?.Name ?? "";
                if (!worldName.Equals("") && !worldName.Contains('-') && !worldName.Contains('_') && !worldName.Contains("contents"))
                    WorldList.Add((i, worldName));
            }
            Worlds = new string[WorldList.Count + 1];
            WorldIDs = new uint[WorldList.Count + 1];
            Worlds[0] = "";
            WorldIDs[0] = 0;
            for (int i = 0; i < WorldList.Count; i++)
                (WorldIDs[i + 1], Worlds[i + 1]) = WorldList[i];
        }
        internal EditPlayerWindow(out AsyncTaskWithUiResult callBack, RaidGroup group, PositionInRaidGroup pos) : base()
        {
            RaidGroup = group;
            callBack = CallBack = new();
            Player = group[pos];
            PlayerCopy = new();
            var target = Helper.TargetChar;
            IsNew = !Player.Filled;
            if (IsNew && target is not null)
            {
                PlayerCopy.MainChar.Name = target.Name.TextValue;
                PlayerCopy.MainChar.HomeWorldID = target.HomeWorld.Id;
                PlayerCopy.MainChar.MainJob = target.GetJob();
                //Ensure Main class is created if applicable
                var c = PlayerCopy.MainChar.MainClass;
            }
            else if (Player.Filled)
            {
                PlayerCopy = Player.Clone();
            }
            (Size, SizingCondition) = (new Vector2(480, 210 + (27 * PlayerCopy.MainChar.Classes.Count)), ImGuiCond.Always);
            Title = $"{Localize("Edit Player ", "Edit Player ")} {Player.NickName} ({RaidGroup.Name})##{Player.Pos}";
            WindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        protected override void Draw()
        {
            //Player Data
            ImGui.InputText(Localize("Player Name", "Player Name"), ref PlayerCopy.NickName, 50);
            //Character Data
            if (ImGui.InputText(Localize("Character Name", "Character Name"), ref PlayerCopy.MainChar.Name, 50))
                PlayerCopy.MainChar.HomeWorldID = 0;
            int worldID = Array.IndexOf(WorldIDs, PlayerCopy.MainChar.HomeWorldID);
            if (worldID < 0 || worldID >= WorldIDs.Length)
                worldID = 0;
            if (ImGui.Combo(Localize("HomeWorld", "Home World"), ref worldID, Worlds, Worlds.Length))
                PlayerCopy.MainChar.HomeWorldID = WorldIDs[worldID] < 0 ? 0 : WorldIDs[worldID];
            if (Helper.TryGetChar(PlayerCopy.MainChar.Name) is not null)
            {
                ImGui.SameLine();
                if (ImGuiHelper.Button(Localize("Get", "Get"), Localize("FetchHomeworldTooltip", "Fetch home world information based on character name (from local players)")))
                    PlayerCopy.MainChar.HomeWorld = Helper.TryGetChar(PlayerCopy.MainChar.Name)?.HomeWorld.GameData;
            }
            //Class Data
            if (PlayerCopy.MainChar.Classes.Count > 0)
            {
                int mainClass = PlayerCopy.MainChar.Classes.FindIndex(x => x.Job == PlayerCopy.MainChar.MainJob);
                if (mainClass < 0 || mainClass >= PlayerCopy.MainChar.Classes.Count)
                {
                    mainClass = 0;
                    PlayerCopy.MainChar.MainJob = PlayerCopy.MainChar.Classes[mainClass].Job;
                }
                if (ImGui.Combo(Localize("Main Job", "Main Job"), ref mainClass, PlayerCopy.MainChar.Classes.ConvertAll(x => x.Job.ToString()).ToArray(), PlayerCopy.MainChar.Classes.Count))
                    PlayerCopy.MainChar.MainJob = PlayerCopy.MainChar.Classes[mainClass].Job;
            }
            else
            {
                PlayerCopy.MainChar.MainJob = null;
                ImGui.NewLine();
                ImGui.Text(Localize("NoClasses", "Character does not have any classes created"));
            }
            ImGui.Columns(2, "Classes", false);
            ImGui.SetColumnWidth(0, 70f);
            ImGui.SetColumnWidth(1, 400f);
            Job? toDelete = null;
            foreach (PlayableClass c in PlayerCopy.MainChar.Classes)
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"delete{c.Job}", $"Delete all data for {c.Job}"))
                    toDelete = c.Job;
                ImGui.SameLine();
                ImGui.Text($"{c.Job}  ");
                ImGui.NextColumn();
                ImGui.SetNextItemWidth(250f);
                ImGui.InputText($"{Localize("BIS", "BIS")}##{c.Job}", ref c.BIS.EtroID, 50);
                ImGui.SameLine();
                if (ImGuiHelper.Button($"{Localize("Default", "Default")}##BIS#{c.Job}", Localize("DefaultBiSTooltip", "Fetch default BiS from configuration")))
                    c.BIS.EtroID = Modules.LootMaster.LootMasterModule.Instance.Configuration.Data.GetDefaultBiS(c.Job);
                ImGui.SameLine();
                if (ImGuiHelper.Button($"{Localize("Reset", "Reset")}##BIS#{c.Job}", Localize("ResetBisTooltip", "Empty out BiS gear")))
                    c.BIS.EtroID = "";
                ImGui.NextColumn();
            }
            if (toDelete is not null)
                PlayerCopy.MainChar.Classes.RemoveAll(x => x.Job == toDelete);
            ImGui.Columns(1);

            var jobsNotUsed = new List<Job>(Enum.GetValues<Job>());
            jobsNotUsed.RemoveAll(x => PlayerCopy.MainChar.Classes.Exists(y => y.Job == x));
            string[] newJobs = jobsNotUsed.ConvertAll(x => x.ToString()).ToArray();
            ImGui.Combo(Localize("Add Job", "Add Job"), ref newJob, newJobs, newJobs.Length);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddJob", "Add job"))
            {
                PlayerCopy.MainChar.Classes.Add(new PlayableClass(jobsNotUsed[newJob], PlayerCopy.MainChar));
            }

            //Buttons
            if (ImGuiHelper.SaveButton(Localize("Save Player", "Save Player")))
            {
                SavePlayer();
                Hide();
            }
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
                Hide();
            Size = new Vector2(480, 210 + (27 * PlayerCopy.MainChar.Classes.Count));
        }
        private void SavePlayer()
        {
            List<(Job, Func<bool>)> bisUpdates = new();
            Player.NickName = PlayerCopy.NickName;
            if (IsNew)
            {
                Character c = new Character(PlayerCopy.MainChar.Name, PlayerCopy.MainChar.HomeWorldID);
                Services.HrtDataManager.GetManagedCharacter(ref c);
                Player.MainChar = c;
                if (c.Classes.Count > 0)
                    return;
            }
            if (Player.MainChar.Name != PlayerCopy.MainChar.Name || Player.MainChar.HomeWorldID != PlayerCopy.MainChar.HomeWorldID)
            {
                uint oldWorld = Player.MainChar.HomeWorldID;
                string oldName = Player.MainChar.Name;
                Player.MainChar.Name = PlayerCopy.MainChar.Name;
                Player.MainChar.HomeWorldID = PlayerCopy.MainChar.HomeWorldID;
                Character c = Player.MainChar;
                Services.HrtDataManager.RearrangeCharacter(oldWorld, oldName, ref c);
                Player.MainChar = c;
            }
            Player.MainChar.MainJob = PlayerCopy.MainChar.MainJob;
            for (int i = 0; i < Player.MainChar.Classes.Count; i++)
            {
                PlayableClass c = Player.MainChar.Classes[i];
                if (!PlayerCopy.MainChar.Classes.Exists(x => x.Job == c.Job))
                {
                    Player.MainChar.Classes.Remove(c);
                    i--;
                }
            }
            foreach (PlayableClass c in PlayerCopy.MainChar.Classes)
            {
                PlayableClass target = Player.MainChar.GetClass(c.Job);
                if (target.BIS.EtroID.Equals(c.BIS.EtroID))
                    continue;
                GearSet set = new()
                {
                    ManagedBy = GearSetManager.Etro,
                    EtroID = c.BIS.EtroID
                };
                if (!set.EtroID.Equals(""))
                {
                    Services.HrtDataManager.GetManagedGearSet(ref set);
                    bisUpdates.Add((target.Job, () => EtroConnector.GetGearSet(target.BIS)));
                }
                target.BIS = set;
            }
            if (bisUpdates.Count > 0)
            {
                CallBack.Action =
                    (t) =>
                    {
                        string success = "";
                        string error = "";
                        List<(Job, bool)> results = ((Task<List<(Job, bool)>>)t).Result;
                        foreach (var result in results)
                        {
                            if (result.Item2)
                                success += result.Item1 + ",";
                            else
                                error += result.Item1 + ",";
                        }
                        if (success.Length > 0)
                            ImGui.TextColored(HRTColorConversions.Vec4(ColorName.Green),
                                    $"BIS for Character {Player.MainChar.Name} on classes ({success[0..^1]}) succesfully updated");

                        if (error.Length > 0)
                            ImGui.TextColored(HRTColorConversions.Vec4(ColorName.Green),
                                    $"BIS update for Character {Player.MainChar.Name} on classes ({error[0..^1]}) failed");
                    };
                CallBack.Task = Task.Run(() => bisUpdates.ConvertAll((x) => (x.Item1, x.Item2.Invoke())));

            }
        }
        public override bool Equals(object? obj)
        {
            if (!(obj?.GetType().IsAssignableTo(GetType()) ?? false))
                return false;
            return Equals((EditPlayerWindow)obj);
        }
        public bool Equals(EditPlayerWindow other)
        {
            if (!RaidGroup.Equals(other.RaidGroup))
                return false;
            if (Pos != other.Pos)
                return false;

            return true;
        }
        public override int GetHashCode()
        {
            return RaidGroup.GetHashCode() << 3 + (int)Pos;
        }
    }
    internal class EditGroupWindow : HrtUI
    {
        private readonly RaidGroup Group;
        private readonly RaidGroup GroupCopy;
        private readonly System.Action OnSave;
        private readonly System.Action OnCancel;

        internal EditGroupWindow(RaidGroup group, System.Action? onSave = null, System.Action? onCancel = null)
        {
            Group = group;
            OnSave = onSave ?? (() => { });
            OnCancel = onCancel ?? (() => { });
            GroupCopy = Group.Clone();
            Show();
            (Size, SizingCondition) = (new Vector2(500, 250), ImGuiCond.Always);
            Title = Localize("Edit Group ", "Edit Group ") + Group.Name;
            WindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        protected override void Draw()
        {
            ImGui.InputText(Localize("Group Name", "Group Name"), ref GroupCopy.Name, 100);
            int groupType = (int)GroupCopy.Type;
            if (ImGui.Combo(Localize("Group Type", "Group Type"), ref groupType, Enum.GetNames(typeof(GroupType)), Enum.GetNames(typeof(GroupType)).Length))
            {
                GroupCopy.Type = (GroupType)groupType;
            }
            if (ImGuiHelper.SaveButton())
            {
                Group.Name = GroupCopy.Name;
                Group.Type = GroupCopy.Type;
                OnSave();
                Hide();
            }
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
            {
                OnCancel();
                Hide();
            }
        }
    }
    internal class EditGearSetWindow : HrtUI
    {
        private readonly GearSet _gearSet;
        private readonly GearSet _gearSetCopy;
        private readonly Job _job;
        private readonly RaidTier? _currentRaidTier;
        private bool CanHaveShield => _job is Job.PLD or Job.THM or Job.GLA;

        internal EditGearSetWindow(GearSet original, Job job, RaidTier? raidTier = null) : base()
        {
            _currentRaidTier = raidTier;
            _job = job;
            _gearSet = original;
            _gearSetCopy = original.Clone();
            Title = $"{Localize("Edit", "Edit")} {(_gearSet.ManagedBy == GearSetManager.HRT ? _gearSet.HrtID : _gearSet.EtroID)}";
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
        }

        protected override void Draw()
        {
            if (ImGuiHelper.SaveButton())
                Save();
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
                Hide();
            ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn("Gear");
            ImGui.TableSetupColumn("Gear");
            ImGui.TableHeadersRow();
            DrawSlot(GearSetSlot.MainHand);
            if (CanHaveShield)
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
            ImGui.End();
        }
        private void DrawSlot(GearSetSlot slot)
        {
            ImGui.TableNextColumn();
            if (!_gearSetCopy[slot].Filled)
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, $"{slot}changeitem", Localize("Select item", "Select item")))
                    AddChild(new SelectGearItemWindow(x => { _gearSetCopy[slot] = x; }, (x) => { }, _gearSetCopy[slot], slot, _job,
                        slot is GearSetSlot.MainHand or GearSetSlot.OffHand ? _currentRaidTier?.WeaponItemLevel ?? 0 : _currentRaidTier?.ArmorItemLevel ?? 0));
            }
            else
            {
                ImGui.BeginGroup();
                ImGui.Text(_gearSetCopy[slot].Item?.Name.RawString);
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    _gearSetCopy[slot].Draw();
                    ImGui.EndTooltip();
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{_gearSetCopy[slot].Slot}changeitem", Localize("Select item", "Select item")))
                    AddChild(new SelectGearItemWindow(x => { _gearSetCopy[slot] = x; }, (x) => { }, _gearSetCopy[slot], _gearSetCopy[slot].Slot, _job,
                        slot is GearSetSlot.MainHand or GearSetSlot.OffHand ? _currentRaidTier?.WeaponItemLevel ?? 0 : _currentRaidTier?.ArmorItemLevel ?? 0));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.WindowClose, $"delete{_gearSetCopy[slot].Slot}", Localize("Delete", "Delete")))
                    _gearSetCopy[_gearSetCopy[slot].Slot] = new();
                for (int i = 0; i < _gearSetCopy[slot].Materia.Count; i++)
                {
                    if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"Delete{_gearSetCopy[slot].Slot}mat{i}", Localize("Remove this materia", "Remove this materia"), i == _gearSetCopy[slot].Materia.Count - 1))
                    {
                        _gearSetCopy[slot].Materia.RemoveAt(i);
                        i--;
                        continue;
                    }
                    ImGui.SameLine();
                    ImGui.Text(_gearSetCopy[slot].Materia[i].Item?.Name.RawString);
                }
                if (_gearSetCopy[slot].Materia.Count < (_gearSetCopy[slot].Item?.IsAdvancedMeldingPermitted ?? false ? 5 : _gearSetCopy[slot].Item?.MateriaSlotCount))
                    if (ImGuiHelper.Button(FontAwesomeIcon.Plus, $"{slot}addmat", Localize("Select materia", "Select materia")))
                        AddChild(new SelectMateriaWindow(x => _gearSetCopy[slot].Materia.Add(x), (x) => { }, _currentRaidTier?.MaxMateriaLevel ?? 0));

                ImGui.EndGroup();
            }
        }
        private void Save()
        {
            _gearSetCopy.TimeStamp = DateTime.Now;
            _gearSet.CopyFrom(_gearSetCopy);
            Hide();
        }
    }
    internal abstract class SelectItemWindow<T> : HrtUI where T : HrtItem
    {
        protected static readonly Lumina.Excel.ExcelSheet<Item> Sheet = Services.DataManager.GetExcelSheet<Item>()!;
        protected T? Item = null;
        private readonly Action<T> OnSave;
        private readonly Action<T?> OnCancel;
        protected virtual bool CanSave { get; set; } = true;
        internal SelectItemWindow(Action<T> onSave, Action<T?> onCancel)
        {
            (OnSave, OnCancel) = (onSave, onCancel);
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;
        }


        protected override void Draw()
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
        private GearSetSlot? Slot;
        private Job? Job;
        private uint minILvl;
        private uint maxILvl;
        private List<Item> _items;
        protected override bool CanSave => false;
        public SelectGearItemWindow(Action<GearItem> onSave, Action<GearItem?> onCancel, GearItem? curentItem = null, GearSetSlot? slot = null, Job? job = null, uint maxItemLevel = 0) : base(onSave, onCancel)
        {
            Item = curentItem;
            Slot = slot;
            Job = job;
            Title = $"{Localize("Get item for", "Get item for")} {Slot}";
            maxILvl = maxItemLevel;
            minILvl = maxILvl > 30 ? maxILvl - 30 : 0;
            _items = reevaluateItems();
        }

        protected override void DrawItemSelection()
        {
            //Draw selection bar
            ImGui.SetNextItemWidth(65f);
            if (ImGuiHelper.Combo("##job", ref Job))
                reevaluateItems();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125f);
            if (ImGuiHelper.Combo("##slot", ref Slot))
                reevaluateItems();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            int min = (int)minILvl;
            if (ImGui.InputInt("-##min", ref min, 5))
            {
                minILvl = (uint)min;
                reevaluateItems();
            }
            ImGui.SameLine();
            int max = (int)maxILvl;
            ImGui.SetNextItemWidth(100f);
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
                    ImGui.PushStyleColor(ImGuiCol.Button, Vec4(ColorName.Redwood.ToHsv().Value(0.75f)));
                if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{item.RowId}", Localize("Use this item", "Use this item"), true, new Vector2(32f, 32f)))
                    if (isCurrentItem)
                        Cancel();
                    else
                        Save(new(item.RowId));
                if (isCurrentItem)
                    ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.BeginGroup();
                if (item?.Icon != null)
                    ImGui.Image(Services.IconCache[item.Icon].ImGuiHandle, new Vector2(32f, 32f));
                ImGui.SameLine();
                ImGui.Text($"{item?.Name.RawString} (IL {item?.LevelItem.Row})");
                ImGui.EndGroup();
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    item?.Draw();
                    ImGui.EndTooltip();
                }
            }
        }
        private List<Item> reevaluateItems()
        {
            _items = Sheet.Where(x =>
                   (Slot == null || x.EquipSlotCategory.Value?.ToSlot() == Slot)
                && (maxILvl == 0 || x.LevelItem.Row <= maxILvl)
                && x.LevelItem.Row >= minILvl
                && x.ClassJobCategory.Value.Contains(Job)
                ).ToList();
            _items.Sort((x, y) => (int)y.LevelItem.Row - (int)x.LevelItem.Row);
            return _items;
        }
    }
    internal class SelectMateriaWindow : SelectItemWindow<HrtMateria>
    {
        private MateriaCategory Cat;
        private byte MateriaLevel;
        private readonly int _numMatLevels;
        protected override bool CanSave => Cat != MateriaCategory.None;
        public SelectMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel, byte matLevel = 0) : base(onSave, onCancel)
        {
            Cat = MateriaCategory.None;
            MateriaLevel = matLevel;
            _numMatLevels = MateriaLevel + 1;
            Title = Localize("Select Materia", "Select Materia");
        }

        protected override void DrawItemSelection()
        {
            int catSlot = Array.IndexOf(Enum.GetValues<MateriaCategory>(), Cat);
            if (ImGui.Combo($"{Localize("Type", "Type")}##Category", ref catSlot, Enum.GetNames<MateriaCategory>(), Enum.GetValues<MateriaCategory>().Length))
            {
                Cat = Enum.GetValues<MateriaCategory>()[catSlot];
                if (Cat != MateriaCategory.None)
                    Item = new(Cat, MateriaLevel);
            }

            int level = MateriaLevel;
            if (ImGui.Combo($"{Localize("Tier", "Tier")}##Level", ref level, Array.ConvertAll(Enumerable.Range(1, _numMatLevels).ToArray(), x => x.ToString()), _numMatLevels))
            {
                MateriaLevel = (byte)level;
                if (Cat != MateriaCategory.None)
                    Item = new(Cat, MateriaLevel);
            }

        }
    }
}
