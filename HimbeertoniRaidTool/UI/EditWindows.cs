using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ColorHelper;
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
        private static string[]? Worlds;
        internal PositionInRaidGroup Pos => Player.Pos;

        internal EditPlayerWindow(out AsyncTaskWithUiResult callBack, RaidGroup group, PositionInRaidGroup pos, bool openHidden = false) : base()
        {
            if (Worlds == null)
            {
                Worlds = new string[100];
                for (uint i = 0; i < Worlds.Length; i++)
                    if (i >= 21 && i < 100)
                        Worlds[i] = Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(i)?.Name ?? "";
                    else
                        Worlds[i] = "";
            }
            RaidGroup = group;
            callBack = CallBack = new();
            Player = group[pos];
            PlayerCopy = new();
            var target = Helper.TargetChar;
            if (!Player.Filled && target is not null)
            {
                PlayerCopy.MainChar.Name = target.Name.TextValue;
                PlayerCopy.MainChar.HomeWorldID = target.HomeWorld.Id;
                PlayerCopy.MainChar.MainClassType = target.GetClass() ?? AvailableClasses.AST;
            }
            else if (Player.Filled)
            {
                PlayerCopy = Player.Clone();
            }
            if (!openHidden)
                Show();
        }

        public override void Draw()
        {
            if (!Visible)
                return;
            ImGui.SetNextWindowSize(new Vector2(500, 250), ImGuiCond.Always);
            if (ImGui.Begin($"{Localize("Edit Player ", "Edit Player ")} {Player.NickName} ({RaidGroup.Name})##{Player.Pos}",
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.InputText(Localize("Player Name", "Player Name"), ref PlayerCopy.NickName, 50);
                if (ImGui.InputText(Localize("Character Name", "Character Name"), ref PlayerCopy.MainChar.Name, 50))
                    PlayerCopy.MainChar.HomeWorldID = 0;
                int worldID = (int)PlayerCopy.MainChar.HomeWorldID;
                if (ImGui.Combo(Localize("HomeWorld", "Home World"), ref worldID, Worlds, Worlds!.Length))
                    PlayerCopy.MainChar.HomeWorldID = (uint)(worldID < 0 ? 0 : worldID);
                if (Helper.TryGetChar(PlayerCopy.MainChar.Name) is not null)
                {
                    ImGui.SameLine();
                    if (ImGui.Button(Localize("Get", "Get")))
                        PlayerCopy.MainChar.HomeWorld = Helper.TryGetChar(PlayerCopy.MainChar.Name)?.HomeWorld.GameData;
                }
                int mainClass = (int)PlayerCopy.MainChar.MainClassType;
                if (ImGui.Combo(Localize("Main Class", "Main Class"), ref mainClass, Enum.GetNames(typeof(AvailableClasses)), Enum.GetNames(typeof(AvailableClasses)).Length))
                    PlayerCopy.MainChar.MainClassType = (AvailableClasses)mainClass;
                AvailableClasses? curClass = Helper.TryGetChar(PlayerCopy.MainChar.Name)?.GetClass();
                if (curClass is not null && curClass != PlayerCopy.MainChar.MainClassType)
                {
                    ImGui.SameLine();
                    if (ImGui.Button(Localize("Current", "Current")))
                    {
                        PlayerCopy.MainChar.MainClassType = (AvailableClasses)curClass;
                    }
                }
                ImGui.InputText(Localize("BIS", "BIS"), ref PlayerCopy.MainChar.MainClass.BIS.EtroID, 100);
                ImGui.SameLine();
                if (ImGui.Button(Localize("Default", "Default") + "##BIS"))
                    PlayerCopy.BIS.EtroID = HRTPlugin.Configuration.DefaultBIS[PlayerCopy.MainChar.MainClass.ClassType];
                ImGui.SameLine();
                if (ImGui.Button(Localize("Reset", "Reset") + "##BIS"))
                    PlayerCopy.MainChar.MainClass.BIS.EtroID = "";
                if (ImGuiHelper.Button(Dalamud.Interface.FontAwesomeIcon.Save, "Save", Localize("Save", "Save Player")))
                {
                    SavePlayer();
                    Hide();
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(Dalamud.Interface.FontAwesomeIcon.WindowClose, "Cancel", Localize("Cancel", "Cancel")))
                    Hide();
            }
            ImGui.End();
        }
        private void SavePlayer()
        {
            List<(AvailableClasses, Func<bool>)> bisUpdates = new();
            Player.NickName = PlayerCopy.NickName;
            if (Player.MainChar.Name != PlayerCopy.MainChar.Name || Player.MainChar.HomeWorldID != PlayerCopy.MainChar.HomeWorldID)
            {
                uint oldWorld = Player.MainChar.HomeWorldID;
                string oldName = Player.MainChar.Name;
                Player.MainChar.Name = PlayerCopy.MainChar.Name;
                Player.MainChar.HomeWorldID = PlayerCopy.MainChar.HomeWorldID;
                Character c = Player.MainChar;
                DataManagement.DataManager.RearrangeCharacter(oldWorld, oldName, ref c);
            }
            Player.MainChar.MainClassType = PlayerCopy.MainChar.MainClassType;
            foreach (PlayableClass c in PlayerCopy.MainChar.Classes)
            {
                PlayableClass target = Player.MainChar.GetClass(c.ClassType);
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
                    bisUpdates.Add((target.ClassType, () => EtroConnector.GetGearSet(target.BIS)));
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
                        List<(AvailableClasses, bool)> results = ((Task<List<(AvailableClasses, bool)>>)t).Result;
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
        public bool Equals(EditPlayerWindow other)
        {
            if (!RaidGroup.Equals(other.RaidGroup))
                return false;
            if (Pos != other.Pos)
                return false;

            return true;
        }
    }

    internal class EditGroupWindow : HrtUI
    {
        private readonly RaidGroup Group;
        private readonly RaidGroup GroupCopy;
        private readonly Action OnSave;
        private readonly Action OnCancel;

        Action doNothing = () => { };
        internal EditGroupWindow(ref RaidGroup group, Action? onSave = null, Action? onCancel = null)
        {
            Group = group;
            OnSave = onSave ?? (() => { });
            OnCancel = onCancel ?? (() => { });
            GroupCopy = Group.Clone();
            Show();
        }

        public override void Draw()
        {
            if (!Visible)
                return;
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
                if (ImGui.Button(Localize("Save", "Save")))
                {
                    Group.Name = GroupCopy.Name;
                    Group.Type = GroupCopy.Type;
                    OnSave();
                    Hide();
                }
                ImGui.SameLine();
                if (ImGui.Button(Localize("Cancel", "Cancel")))
                {
                    OnCancel();
                    Hide();
                }
                ImGui.End();
            }
        }
    }
}
