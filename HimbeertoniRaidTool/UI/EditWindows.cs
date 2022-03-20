using ColorHelper;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    internal class EditPlayerWindow : HrtUI
    {
        private readonly Player Player;
        private readonly Player PlayerCopy;
        private readonly AsyncTaskWithUiResult CallBack;
        internal PositionInRaidGroup Pos => Player.Pos;

        internal EditPlayerWindow(out AsyncTaskWithUiResult callBack, RaidGroup group, PositionInRaidGroup pos) : base()
        {
            callBack = CallBack = new();
            Player = group[pos];
            PlayerCopy = new();
            if (!Player.Filled && Helper.Target is not null)
            {
                PlayerCopy.MainChar.Name = Helper.Target.Name.TextValue;
                PlayerCopy.MainChar.MainClassType = Helper.Target.GetClass();
            }
            else if (Player.Filled)
            {
                PlayerCopy = Player.Clone();
            }
            Show();
        }

        public override void Draw()
        {
            if (!Visible)
                return;
            ImGui.SetNextWindowSize(new Vector2(500, 250), ImGuiCond.Always);
            if (ImGui.Begin(Localize("Edit Player ", "Edit Player ") + Player.Pos,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.InputText(Localize("Player Name", "Player Name"), ref PlayerCopy.NickName, 50);
                ImGui.InputText(Localize("Character Name", "Character Name"), ref PlayerCopy.MainChar.Name, 50);

                int mainClass = (int)PlayerCopy.MainChar.MainClassType;
                if (ImGui.Combo(Localize("Main Class", "Main Class"), ref mainClass, Enum.GetNames(typeof(AvailableClasses)), Enum.GetNames(typeof(AvailableClasses)).Length))
                {
                    PlayerCopy.MainChar.MainClassType = (AvailableClasses)mainClass;
                }

                AvailableClasses? curClass = null;
                if (PlayerCopy.MainChar.Name.Equals(Helper.Target?.Name.TextValue))
                {
                    if (Enum.TryParse(Helper.Target!.ClassJob.GameData!.Abbreviation, false, out AvailableClasses parsed))
                        curClass = parsed;
                }
                else if (PlayerCopy.MainChar.Name.Equals(Services.ClientState.LocalPlayer?.Name.TextValue))
                {
                    if (Enum.TryParse(Services.ClientState.LocalPlayer!.ClassJob.GameData!.Abbreviation, false, out AvailableClasses parsed))
                        curClass = parsed;
                }
                if (curClass is not null)
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
                if (ImGui.Button(Localize("Save", "Save")))
                {
                    List<(AvailableClasses, Func<bool>)> bisUpdates = new();
                    Player.NickName = PlayerCopy.NickName;
                    Player.MainChar.Name = PlayerCopy.MainChar.Name;
                    Player.MainChar.MainClassType = PlayerCopy.MainChar.MainClassType;
                    foreach (PlayableClass c in PlayerCopy.MainChar.Classes)
                    {
                        PlayableClass target = Player.MainChar.GetClass(c.ClassType);
                        if (target.BIS.EtroID.Equals(c.BIS.EtroID))
                            continue;
                        target.BIS.EtroID = c.BIS.EtroID;
                        if (target.BIS.EtroID.Equals(""))
                        {
                            target.BIS.Clear();
                        }
                        else
                        {
                            bisUpdates.Add((target.ClassType, () => EtroConnector.GetGearSet(target.BIS)));
                        }
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
                                            $"BIS for Chracter {Player.MainChar.Name} on classes ({success[0..^1]}) succesfully updated");

                                if (error.Length > 0)
                                    ImGui.TextColored(HRTColorConversions.Vec4(ColorName.Green),
                                            $"BIS update for Chracter {Player.MainChar.Name} on classes ({error[0..^1]}) failedd");
                            };
                        CallBack.Task = Task.Run(() => bisUpdates.ConvertAll((x) => (x.Item1, x.Item2.Invoke())));
                    }
                    Hide();
                }
                ImGui.SameLine();
                if (ImGui.Button(Localize("Cancel", "Cancel")))
                    Hide();
            }
            ImGui.End();
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