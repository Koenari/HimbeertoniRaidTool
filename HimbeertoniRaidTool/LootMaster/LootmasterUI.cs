using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using HimbeertoniRaidTool.Data;
using Dalamud.Logging;
using static HimbeertoniRaidTool.Connectors.EtroConnector;

namespace HimbeertoniRaidTool.LootMaster
{
    public class LootmasterUI : HrtUI
    {
        RaidGroup Group;
        private List<AsyncTask> tasks = new();
        private AsyncTask? FinishedTask;
        private List<EditPlayerWindow> Childs = new();
        private bool modalOpen = false;
        public LootmasterUI(RaidGroup group) : base()
        {
            this.Group = group;
        }
        public override void Dispose()
        {
            foreach (AsyncTask t in tasks)
                t.Dispose();
            foreach (EditPlayerWindow win in Childs)
                win.Dispose();
        }

        public override void Draw()
        {
            if (!this.visible)
                return;
            DrawMainWindow();
        }

        private void HandleAsync()
        {

            if (modalOpen && FinishedTask != null)
            {
                if (ImGui.BeginPopupModal("TaskPopup", ref modalOpen))
                {
                    ImGui.Text(FinishedTask.Message);
                    if (ImGui.Button("Close##TaskPopup"))
                    {
                        FinishedTask.Dispose();
                        modalOpen = false;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
            }
            else if (!modalOpen)
            {
                foreach (AsyncTask t in tasks)
                {
                    if (t.IsCompleted)
                    {
                        PluginLog.LogDebug("Task finished: " + t.Message);
                        modalOpen = true;
                        ImGui.OpenPopup("TaskPopup");
                        FinishedTask = t;
                        tasks.Remove(t);
                        break;
                    }
                }
            }
        }
        private void DrawMainWindow()
        {
            ImGui.SetNextWindowSize(new Vector2(1000, 800), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Loot Master", ref this.visible,ImGuiWindowFlags.NoScrollbar))
            {
                HandleAsync();
                if (ImGui.BeginTable("RaidGruppe", 14, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable, new Vector2(), 5))
                {
                    ImGui.TableSetupColumn("Player");
                    ImGui.TableSetupColumn("iLvl");
                    ImGui.TableSetupColumn("Weapon");
                    ImGui.TableSetupColumn("Head");
                    ImGui.TableSetupColumn("Chest");
                    ImGui.TableSetupColumn("Gloves");
                    ImGui.TableSetupColumn("Legs");
                    ImGui.TableSetupColumn("Feet");
                    ImGui.TableSetupColumn("Ear");
                    ImGui.TableSetupColumn("Neck");
                    ImGui.TableSetupColumn("Bracers");
                    ImGui.TableSetupColumn("Ring 1");
                    ImGui.TableSetupColumn("Ring 2");
                    ImGui.TableHeadersRow();
                    foreach (Player player in Group.GetPlayers())
                    {
                        DrawPlayer(player);
                    }
                    ImGui.EndTable();
                }
                if (ImGui.Button("Close"))
                {
                    this.Hide();
                }
            }
            ImGui.End();
        }
        private void DrawPlayer(Player player)
        {
            bool PlayerExists = player.Filled && player.MainChar.Filled;
            if (PlayerExists)
            {
                GearSet gear = player.MainChar.MainClass.Gear;
                GearSet bis = player.MainChar.MainClass.BIS;
                ImGui.TableNextColumn();
                ImGui.Text(player.Pos + "\n" + player.NickName + "\n" + player.MainChar.Name + " (" + player.MainChar.MainClassType + ")");
                ImGui.TableNextColumn();
                ImGui.Text(gear.GetItemLevel().ToString());
                ImGui.NewLine();
                ImGui.Text(bis.GetItemLevel().ToString());
                DrawItem(gear.MainHand, bis.MainHand);
                DrawItem(gear.Head, bis.Head);
                DrawItem(gear.Body, bis.Body);
                DrawItem(gear.Hands, bis.Hands);
                DrawItem(gear.Legs, bis.Legs);
                DrawItem(gear.Feet, bis.Feet);
                DrawItem(gear.Ear, bis.Ear);
                DrawItem(gear.Neck, bis.Neck);
                DrawItem(gear.Wrist, bis.Wrist);
                DrawItem(gear.Ring1, bis.Ring1);
                DrawItem(gear.Ring2, bis.Ring2);
                ImGui.TableNextColumn();
                EditPlayerButton();
                ImGui.SameLine();
                if (ImGui.Button("x##"+ player.Pos))
                {
                    player.Reset();
                }
            }
            else
            {

                ImGui.TableNextColumn();
                ImGui.Text("No Player");
                for(int i = 0; i < 12; i++)
                {
                    ImGui.TableNextColumn();
                }
                ImGui.TableNextColumn();
                EditPlayerButton();
            }
            void EditPlayerButton()
            {
                if (ImGui.Button(PlayerExists ?  "Edit": "Add" + "## " + player.Pos))
                {
                    EditPlayerWindow win = new(this, player.Pos);
                    win.Show();
                    this.Childs.Add(win);

                }
            }
            void DrawItem(GearItem item, GearItem bis)
            {
                ImGui.TableNextColumn();
                ImGui.Text(item.Valid ? item.Name : "Empty");
                ImGui.NewLine();
                ImGui.Text(bis.Valid ? bis.Name : "Empty");
            }

        }
        bool removeChild(EditPlayerWindow item)
        {
            return this.Childs.Remove(item);
        }
        class EditPlayerWindow : HrtUI
        {
            private readonly LootmasterUI LmUi;
            private readonly Player PlayerToAdd;
            private bool BISChanged = false;

            internal EditPlayerWindow(LootmasterUI lmui, Player.Position pos) : base()
            {
                this.LmUi = lmui;
                if (this.LmUi.Group.GetPlayer(pos) == null)
                    this.LmUi.Group.SetPlayer(pos, new(pos));
                PlayerToAdd = this.LmUi.Group.GetPlayer(pos);
            }

            public override void Dispose() { }

            public override void Draw()
            {
                if (!this.visible)
                    return;
                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);
                if (ImGui.Begin("Edit Player " + PlayerToAdd.Pos, ref this.visible, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.InputText("Player Name", ref PlayerToAdd.NickName, 50);
                    ImGui.InputText("Character Name", ref PlayerToAdd.MainChar.Name, 50);
                    
                    int mainClass = (int) PlayerToAdd.MainChar.MainClassType;
                    if(ImGui.Combo("Main Class", ref mainClass, Enum.GetNames(typeof(AvailableClasses)), Enum.GetNames(typeof(AvailableClasses)).Length))
                    {
                        this.PlayerToAdd.MainChar.MainClassType = (AvailableClasses) mainClass;
                    }
                    if(ImGui.InputText("BIS", ref PlayerToAdd.MainChar.MainClass.BIS.EtroID, 100))
                    {
                        BISChanged = true;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Default##BIS##EditPlayerWindow##" + PlayerToAdd.Pos))
                    {
                        if (!PlayerToAdd.MainChar.MainClass.BIS.EtroID.Equals(HRTPlugin.Plugin.Configuration.DefaultBIS[PlayerToAdd.MainChar.MainClass.ClassType]))
                        {
                            BISChanged = true;
                            PlayerToAdd.MainChar.MainClass.BIS.EtroID = HRTPlugin.Plugin.Configuration.DefaultBIS[PlayerToAdd.MainChar.MainClass.ClassType];
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Reset##BIS##EditPlayerWindow##"+PlayerToAdd.Pos))
                    {
                        if (!PlayerToAdd.MainChar.MainClass.BIS.EtroID.Equals("")){
                            PlayerToAdd.MainChar.MainClass.BIS.EtroID = "";
                            BISChanged = false;
                            PlayerToAdd.MainChar.MainClass.BIS.Clear();
                        }
                    }
                    if (ImGui.Button("Save##EditPlayerWindow##" + PlayerToAdd.Pos))
                    {
                        if (BISChanged)
                        {
                            LmUi.tasks.Add(new("BIS for " + PlayerToAdd.MainChar.Name + " succesfully updated", "BIS update for " + PlayerToAdd.MainChar.Name + " failed", Task<bool>.Run(() => GetGearSet(PlayerToAdd.MainChar.MainClass.BIS))));
                            LmUi.modalOpen = false;
                        }
                        this.Hide();
                    }
                }
                ImGui.End();
            }
            public override void Hide()
            {
                base.Hide();
                LmUi.removeChild(this);
            }
        }
        class AsyncTask : IDisposable
        {
            private string SuccessMessage;
            private string FailMessage;
            private Task<bool> Task;
            public string Message => Task.Result ? SuccessMessage : FailMessage;
            public bool IsCompleted => Task.IsCompleted;

            internal AsyncTask(string sm, string fm, Task<bool> t)
            {
                SuccessMessage = sm;
                FailMessage = fm;
                Task = t;
            }
            public void Dispose()
            {
                Task.Wait();
                Task.Dispose();
            }
        }
    }
}
