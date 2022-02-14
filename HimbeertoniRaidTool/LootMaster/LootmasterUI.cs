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
        public LootmasterUI(HRTPlugin plugin, RaidGroup group) : base(plugin)
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
                
                
                ImGui.EndPopup();
                if (ImGui.Button("Close"))
                {
                    this.Hide();
                }
            }
            ImGui.End();
        }
        private void DrawPlayer(Player player)
        {
            if (player.Filled && player.MainChar.Filled)
            {
                GearSet gear = player.MainChar.MainClass.Gear;
                GearSet bis = player.MainChar.MainClass.BIS;
                ImGui.TableNextColumn();
                ImGui.Text(player.NickName + "\n" + player.MainChar.Name + " (" + player.MainChar.MainClassType + ")");
                ImGui.TableNextColumn();
                ImGui.Text(gear.GetItemLevel().ToString());
                ImGui.NewLine();
                ImGui.Text(bis.GetItemLevel().ToString());
                DrawItem(gear.Weapon, bis.Weapon);
                DrawItem(gear.Head, bis.Head);
                DrawItem(gear.Body, bis.Body);
                DrawItem(gear.Gloves, bis.Gloves);
                DrawItem(gear.Legs, bis.Legs);
                DrawItem(gear.Feet, bis.Feet);
                DrawItem(gear.Earrings, bis.Earrings);
                DrawItem(gear.Necklace, bis.Necklace);
                DrawItem(gear.Bracelet, bis.Bracelet);
                DrawItem(gear.Ring1, bis.Ring1);
                DrawItem(gear.Ring2, bis.Ring2);
                ImGui.TableNextColumn();
                if (ImGui.Button("BIS##Button" + player.Pos))
                {
                    modalOpen = true;
                    ImGui.OpenPopup("BIS##" + player.Pos);
                }
                if (ImGui.Button("x##"+ player.Pos))
                {
                    player.Reset();
                }
                if (ImGui.BeginPopupModal("BIS##" + player.Pos, ref modalOpen))
                {
                    ImGui.InputText("EtroID", ref player.MainChar.MainClass.BIS.EtroID, 100);
                    if(ImGui.Button("Close##BIS"))
                    {
                        this.tasks.Add(new("BIS for " + player.MainChar.Name + "succesfully updated", "BIS update for " + player.MainChar.Name + "failed", Task<bool>.Run(() => GetGearSet(player.MainChar.MainClass.BIS))));
                        modalOpen = false;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
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
                if (ImGui.Button("Add " + player.Pos))
                {
                    EditPlayerWindow win = new(Parent, this, player.Pos);
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
            private LootmasterUI LmUi;
            

            private Player PlayerToAdd;

            internal EditPlayerWindow(HRTPlugin par, LootmasterUI lmui, Player.Position pos) : base(par)
            {
                this.LmUi = lmui;
                if (this.LmUi.Group.GetPlayer(pos) == null)
                    this.LmUi.Group.SetPlayer(pos, new(pos));
                PlayerToAdd = this.LmUi.Group.GetPlayer(pos);
                PlayerToAdd.MainChar = new Character();
            }

            public override void Dispose() { }

            public override void Draw()
            {
                if (!this.visible)
                    return;
                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);
                if (ImGui.Begin("Add Player " + PlayerToAdd.Pos, ref this.visible, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.InputText("Player Name", ref PlayerToAdd.NickName, 50);
                    ImGui.InputText("Character Name", ref PlayerToAdd.MainChar.Name, 50);
                    int mainClass = 0;
                    if(ImGui.ListBox("Main Class", ref mainClass, Enum.GetNames(typeof(AvailableClasses)), Enum.GetNames(typeof(AvailableClasses)).Length, 1))
                    {
                        this.PlayerToAdd.MainChar.MainClassType = (AvailableClasses) mainClass;
                    }
                    if (ImGui.Button("Save"))
                    {
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
