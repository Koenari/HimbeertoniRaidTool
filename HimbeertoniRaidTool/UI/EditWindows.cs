using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ColorHelper;
using Dalamud.Interface;
using HimbeertoniRaidTool.Connectors;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
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
            for (uint i = 21; i < (Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.RowCount ?? 0); i++)
            {
                string? worldName = Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(i)?.Name ?? "";
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
        internal EditPlayerWindow(out AsyncTaskWithUiResult callBack, RaidGroup group, PositionInRaidGroup pos, bool openHidden = false) : base()
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
            }
            else if (Player.Filled)
            {
                PlayerCopy = Player.Clone();
            }
            if (!openHidden)
                Show();
        }

        protected override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(480, 210 + (27 * PlayerCopy.MainChar.Classes.Count)), ImGuiCond.Always);
            if (ImGui.Begin($"{Localize("Edit Player ", "Edit Player ")} {Player.NickName} ({RaidGroup.Name})##{Player.Pos}",
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
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
                        c.BIS.EtroID = HRTPlugin.Configuration.GetDefaultBiS(c.Job);
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
                var newJobs = jobsNotUsed.ConvertAll(x => x.ToString()).ToArray();
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
            }
            ImGui.End();
        }
        private void SavePlayer()
        {
            List<(Job, Func<bool>)> bisUpdates = new();
            Player.NickName = PlayerCopy.NickName;
            if (IsNew)
            {
                Character c = new Character(PlayerCopy.MainChar.Name, PlayerCopy.MainChar.HomeWorldID);
                DataManagement.DataManager.GetManagedCharacter(ref c);
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
                DataManagement.DataManager.RearrangeCharacter(oldWorld, oldName, ref c);
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
                    DataManagement.DataManager.GearDB.AddOrGetSet(ref set);
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
        private readonly Action OnSave;
        private readonly Action OnCancel;

        internal EditGroupWindow(RaidGroup group, Action? onSave = null, Action? onCancel = null)
        {
            Group = group;
            OnSave = onSave ?? (() => { });
            OnCancel = onCancel ?? (() => { });
            GroupCopy = Group.Clone();
            Show();
        }

        protected override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(500, 250), ImGuiCond.Always);
            if (ImGui.Begin(Localize("Edit Group ", "Edit Group ") + Group.Name,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
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
                ImGui.End();
            }
        }
    }
}
