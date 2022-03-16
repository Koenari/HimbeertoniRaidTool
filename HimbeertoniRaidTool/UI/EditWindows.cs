using ColorHelper;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
using System;
using System.Numerics;
using System.Threading.Tasks;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    internal class EditPlayerWindow : HrtUI
    {
        private readonly Player Player;
        private readonly Player PlayerCopy;
        private bool BISChanged = false;
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
                PlayerCopy.MainChar.MainClassType = Helper.TargetClass!;
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
                if (ImGui.InputText(Localize("BIS", "BIS"), ref Player.MainChar.MainClass.BIS.EtroID, 100))
                {
                    BISChanged = true;
                }
                ImGui.SameLine();
                if (ImGui.Button(Localize("Default", "Default") + "##BIS"))
                {
                    if (!PlayerCopy.MainChar.MainClass.BIS.EtroID.Equals(HRTPlugin.Configuration.DefaultBIS[Player.MainChar.MainClass.ClassType]))
                    {
                        BISChanged = true;
                        PlayerCopy.MainChar.MainClass.BIS.EtroID = HRTPlugin.Configuration.DefaultBIS[Player.MainChar.MainClass.ClassType];
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(Localize("Reset", "Reset") + "##BIS"))
                {
                    if (!PlayerCopy.MainChar.MainClass.BIS.EtroID.Equals(""))
                    {
                        PlayerCopy.MainChar.MainClass.BIS.EtroID = "";
                        BISChanged = false;
                        PlayerCopy.MainChar.MainClass.BIS.Clear();
                    }
                }
                if (ImGui.Button(Localize("Save", "Save")))
                {
                    Player.NickName = PlayerCopy.NickName;
                    Player.MainChar.Name = PlayerCopy.MainChar.Name;
                    Player.MainChar.MainClassType = PlayerCopy.MainChar.MainClassType;
                    if (BISChanged)
                    {
                        CallBack.Action =
                            (t) =>
                            {
                                if (((Task<bool>)t).Result)
                                    ImGui.TextColored(HRTColorConversions.Vec4(ColorName.Green),
                                        $"BIS for {Player.MainChar.Name} ({Player.MainChar.MainClassType}) succesfully updated");
                                else
                                    ImGui.TextColored(HRTColorConversions.Vec4(ColorName.Red),
                                        $"BIS update for {Player.MainChar.Name} ({Player.MainChar.MainClassType}) failed");
                            };
                        CallBack.Task = Task.Run(() => EtroConnector.GetGearSet(Player.MainChar.MainClass.BIS));
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