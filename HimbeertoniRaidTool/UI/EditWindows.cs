using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ColorHelper;
using Dalamud.Interface;
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
        private readonly Action<HrtUiMessage> CallBack;
        private readonly bool IsNew;
        private Job newJob = Job.ADV;
        private readonly Func<Job, string> GetBisID;
        private const int ClassHeight = 27 * 2 + 4;
        internal PositionInRaidGroup Pos;
        internal EditPlayerWindow(Action<HrtUiMessage> callBack, RaidGroup group, PositionInRaidGroup pos, Func<Job, string> getBisID)
            : base(true, $"{group.GetHashCode()}##{pos}")
        {
            Pos = pos;
            GetBisID = getBisID;
            RaidGroup = group;
            CallBack = callBack;
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
                var _ = PlayerCopy.MainChar.MainClass;
            }
            else if (Player.Filled)
            {
                PlayerCopy = Player.Clone();
            }
            (Size, SizingCondition) = (new Vector2(450, 330 + (ClassHeight * PlayerCopy.MainChar.Classes.Count)), ImGuiCond.Appearing);
            Title = $"{Localize("Edit Player", "Edit Player")} {Player.NickName} ({RaidGroup.Name})##{Pos}";
        }
        protected override void Draw()
        {
            bool resize = false;
            //Player Data
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Player Data", "Player Data")).X) / 2f);
            ImGui.Text(Localize("Player Data", "Player Data"));
            ImGui.InputText(Localize("Player Name", "Player Name"), ref PlayerCopy.NickName, 50);
            int dps = PlayerCopy.AdditionalData.ManualDPS;
            if (ImGui.InputInt(Localize("manuallySetDPS", "Predicted DPS"), ref dps, 100, 1000))
                PlayerCopy.AdditionalData.ManualDPS = dps;
            //Character Data
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Character Data", "Character Data")).X) / 2f);
            ImGui.Text(Localize("Character Data", "Character Data"));
            if (ImGui.InputText(Localize("Character Name", "Character Name"), ref PlayerCopy.MainChar.Name, 50)
                 && Helper.TryGetChar(out var pc, PlayerCopy.MainChar.Name))
            {
                PlayerCopy.MainChar.HomeWorld ??= pc?.HomeWorld.GameData;
            }
            if (ImGuiHelper.ExcelSheetCombo(Localize("Home World", "Home World") + "##" + Title, out World? w,
                x => PlayerCopy.MainChar.HomeWorld?.Name.RawString ?? "", ImGuiComboFlags.None,
                 (x, y) => x.Name.RawString.Contains(y, StringComparison.CurrentCultureIgnoreCase),
                 x => x.Name.RawString, x => x.IsPublic))
            {
                PlayerCopy.MainChar.HomeWorld = w;
            }
            //ImGuiHelper.Combo(Localize("Gender", "Gender"), ref PlayerCopy.MainChar.Gender);
            string GetGenderedTribeName(Tribe t) => PlayerCopy.MainChar.Gender == Gender.Male ? t.Masculine.RawString : t.Feminine.RawString;
            if (ImGuiHelper.ExcelSheetCombo(Localize("Tribe", "Tribe") + "##" + Title, out Tribe? t,
               x => GetGenderedTribeName(PlayerCopy.MainChar.Tribe), ImGuiComboFlags.None,
                (x, y) => GetGenderedTribeName(x).Contains(y, StringComparison.CurrentCultureIgnoreCase),
                x => GetGenderedTribeName(x)))
            {
                PlayerCopy.MainChar.TribeID = t?.RowId ?? 0;
            }
            //Class Data
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(Localize("Job Data", "Job Data")).X) / 2f);
            ImGui.Text(Localize("Job Data", "Job Data"));
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
            ImGui.SetColumnWidth(0, 70f * ScaleFactor);
            ImGui.SetColumnWidth(1, 400f * ScaleFactor);
            ImGui.Separator();
            Job? toDelete = null;
            foreach (PlayableClass c in PlayerCopy.MainChar.Classes)
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, $"delete{c.Job}", $"Delete all data for {c.Job}"))
                    toDelete = c.Job;
                ImGui.SameLine();
                ImGui.Text($"{c.Job}  ");
                ImGui.NextColumn();
                ImGui.SetNextItemWidth(250f * ScaleFactor);
                ImGui.InputText($"{Localize("BIS", "BIS")}##{c.Job}", ref c.BIS.EtroID, 50);
                if (!c.BIS.EtroID.Equals(GetBisID(c.Job)))
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.Button($"{Localize("Set to default", "Set to default")}##BIS#{c.Job}",
                        Localize("DefaultBiSTooltip", "Fetch default BiS from configuration")))
                        c.BIS.EtroID = GetBisID(c.Job);
                }
                ImGui.NextColumn();
                ImGui.NextColumn();
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                if (ImGui.InputInt($"{Localize("Level", "Level")}##{c.Job}", ref c.Level))
                {
                    c.Level = Math.Clamp(c.Level, 1, CuratedData.MaxLevel);
                }

                ImGui.Separator();
                ImGui.NextColumn();
            }
            if (toDelete is not null)
            {
                PlayerCopy.MainChar.Classes.RemoveAll(x => x.Job == toDelete);
                Size.Y -= ClassHeight;
                resize = true;
            }

            ImGui.Columns(1);
            if (ImGuiHelper.SearchableCombo(Localize("Add Job", "Add Job"), out Job job, newJob.ToString(), ImGuiComboFlags.None,
                Enum.GetValues<Job>(), (j, s) => j.ToString().Contains(s, StringComparison.CurrentCultureIgnoreCase),
                j => j.ToString(), (t) => !PlayerCopy.MainChar.Classes.Exists(y => y.Job == t)))
            {
                newJob = job;
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddJob", "Add job"))
            {
                PlayerCopy.MainChar.GetClass(newJob).BIS.EtroID = GetBisID(newJob);
                Size.Y += ClassHeight;
                resize = true;
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
            SizingCondition = resize ? ImGuiCond.Always : ImGuiCond.Appearing;
        }
        private void SavePlayer()
        {
            List<(Job, Func<bool>)> bisUpdates = new();
            Player.NickName = PlayerCopy.NickName;
            Player.AdditionalData.ManualDPS = PlayerCopy.AdditionalData.ManualDPS;
            if (IsNew)
            {
                Character c = new(PlayerCopy.MainChar.Name, PlayerCopy.MainChar.HomeWorldID);
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
                target.Level = c.Level;
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
                    Services.TaskManager.RegisterTask(CallBack, () => Services.ConnectorPool.EtroConnector.GetGearSet(target.BIS)
                    , $"BIS update for Character {Player.MainChar.Name} ({c.Job}) succeeded"
                    , $"BIS update for Character {Player.MainChar.Name} ({c.Job}) failed");
                }
                target.BIS = set;
            }
            Services.HrtDataManager.Save();
        }
    }
    internal class EditGroupWindow : HrtUI
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
            (Size, SizingCondition) = (new Vector2(500, 150 + (group.RolePriority != null ? 180 : 0)), ImGuiCond.Appearing);
            Title = $"{Localize("Edit Group", "Edit Group")} {Group.Name}";
        }

        protected override void Draw()
        {
            ImGui.InputText(Localize("Group Name", "Group Name"), ref GroupCopy.Name, 100);
            int groupType = (int)GroupCopy.Type;
            if (ImGui.Combo(Localize("Group Type", "Group Type"), ref groupType, Enum.GetNames(typeof(GroupType)), Enum.GetNames(typeof(GroupType)).Length))
            {
                GroupCopy.Type = (GroupType)groupType;
            }
            bool overrrideRolePriority = GroupCopy.RolePriority != null;
            if (ImGui.Checkbox(Localize("Overrride role priority", "Overrride role priority"), ref overrrideRolePriority))
            {
                GroupCopy.RolePriority = overrrideRolePriority ? new RolePriority() : null;
                Size.Y += (overrrideRolePriority ? 1 : -1) * 180f;
            }
            if (overrrideRolePriority)
            {
                ImGui.Text(Localize("ConfigRolePriority", "Priority to loot for each role (smaller is higher priority)"));
                ImGui.Text($"{Localize("Current priority", "Current priority")}: {GroupCopy.RolePriority}");
                GroupCopy.RolePriority!.DrawEdit();
            }

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
        }

        protected override void Draw()
        {
            if (ImGuiHelper.SaveButton())
                Save();
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
                Hide();
            if (ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.Borders))
            {
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
            }
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
                ImGui.SameLine();
                if (_gearSetCopy[slot].Item?.CanBeHq ?? false)
                    ImGui.Checkbox($"{Localize("HQ", "HQ")}##{slot}", ref _gearSetCopy[slot].IsHq);
                ImGui.EndGroup();
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    _gearSetCopy[slot].Draw();
                    ImGui.EndTooltip();
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Search, $"{slot}changeitem", Localize("Select item", "Select item")))
                    AddChild(new SelectGearItemWindow(x => { _gearSetCopy[slot] = x; }, (x) => { }, _gearSetCopy[slot], slot, _job,
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
                    {
                        byte maxMatLevel = _currentRaidTier?.MaxMateriaLevel ?? 0;
                        if (_gearSetCopy[slot].Materia.Count > _gearSetCopy[slot].Item?.MateriaSlotCount)
                            maxMatLevel--;
                        AddChild(new SelectMateriaWindow(x => _gearSetCopy[slot].Materia.Add(x), (x) => { }, maxMatLevel));
                    }
            }
        }
        private void Save()
        {
            _gearSetCopy.TimeStamp = DateTime.Now;
            _gearSet.CopyFrom(_gearSetCopy);
            Services.HrtDataManager.Save();
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
            WindowFlags = ImGuiWindowFlags.NoCollapse;
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
        private readonly bool _lockSlot = false;
        private GearSetSlot? Slot;
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
                Slot = slot.Value;
            }
            else
            {
                Slot = Item?.Slot;
            }
            _lockJob = job.HasValue;
            Job = job;
            Slot = slot;
            Title = $"{Localize("Get item for", "Get item for")} {Slot}";
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
            if (ImGuiHelper.Combo("##slot", ref Slot))
                reevaluateItems();
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
            foreach (Item item in _items)
            {
                bool isCurrentItem = item.RowId == Item?.ID;
                if (isCurrentItem)
                    ImGui.PushStyleColor(ImGuiCol.Button, Vec4(ColorName.Redwood.ToHsv().Value(0.75f)));
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
                ImGui.Image(Services.IconCache.LoadIcon(item.Icon, item.CanBeHq).ImGuiHandle, new Vector2(32f, 32f));
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
                   (Slot == null || (x.EquipSlotCategory.Value?.Contains(Slot) ?? false))
                && (maxILvl == 0 || x.LevelItem.Row <= maxILvl)
                && x.LevelItem.Row >= minILvl
                && (Job == 0 || x.ClassJobCategory.Value.Contains(Job))
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
        private string longestName;
        protected override bool CanSave => Cat != MateriaCategory.None;
        public SelectMateriaWindow(Action<HrtMateria> onSave, Action<HrtMateria?> onCancel, byte maxMatLvl, byte? matLevel = null) : base(onSave, onCancel)
        {
            Cat = MateriaCategory.None;
            MateriaLevel = matLevel ?? maxMatLvl;
            _numMatLevels = maxMatLvl + 1;
            Title = Localize("Select Materia", "Select Materia");
            Size = new(ImGui.CalcTextSize(longestName).X + 200f, 120f);
            longestName = Enum.GetNames<MateriaCategory>().MaxBy(s => ImGui.CalcTextSize(s).X) ?? "";
        }

        protected override void DrawItemSelection()
        {
            int catSlot = Array.IndexOf(Enum.GetValues<MateriaCategory>(), Cat);
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(longestName).X + 40f * ScaleFactor);
            if (ImGui.Combo($"{Localize("Type", "Type")}##Category", ref catSlot, Enum.GetNames<MateriaCategory>(), Enum.GetValues<MateriaCategory>().Length))
            {
                Cat = Enum.GetValues<MateriaCategory>()[catSlot];
                if (Cat != MateriaCategory.None)
                    Item = new(Cat, MateriaLevel);
            }

            int level = MateriaLevel;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(longestName).X + 40f * ScaleFactor);
            if (ImGui.Combo($"{Localize("Tier", "Tier")}##Level", ref level, Array.ConvertAll(Enumerable.Range(1, _numMatLevels).ToArray(), x => x.ToString()), _numMatLevels))
            {
                MateriaLevel = (byte)level;
                if (Cat != MateriaCategory.None)
                    Item = new(Cat, MateriaLevel);
            }

        }
    }
}
