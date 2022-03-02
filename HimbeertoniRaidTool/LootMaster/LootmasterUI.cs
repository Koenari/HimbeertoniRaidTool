using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using static HimbeertoniRaidTool.LootMaster.Helper;
using static HimbeertoniRaidTool.Services;
using static HimbeertoniRaidTool.UI.Helper;

namespace HimbeertoniRaidTool.LootMaster
{
    public class LootmasterUI : HrtUI
    {
        private readonly RaidGroup Group;
        private readonly List<AsyncTaskWithUiResult> Tasks = new();
        private readonly List<HrtUI> Childs = new();
        public LootmasterUI(RaidGroup group) : base() => Group = group;
        public override void Dispose()
        {
            foreach (AsyncTaskWithUiResult t in Tasks)
                t.Dispose();
            Tasks.Clear();
            foreach (HrtUI win in Childs)
                win.Dispose();
            Childs.Clear();
        }
        public override void Draw()
        {
            if (!Visible)
                return;
            Childs.ForEach(x => { if (!x.IsVisible) x.Dispose(); });
            Childs.RemoveAll(x => !x.IsVisible);
            DrawMainWindow();
        }
        public static Color ILevelColor(GearItem item)
        {
            if (item.ItemLevel >= 600)
            {
                return Color.Green;
            }
            else if (item.ItemLevel >= 590)
            {
                return Color.Aquamarine;
            }
            else if (item.ItemLevel >= 580)
            {
                return Color.Yellow;
            }
            else
            {
                return Color.Red;
            }
        }

        private void HandleAsync()
        {
            Tasks.RemoveAll(t => t.FinishedShowing);
            foreach (AsyncTaskWithUiResult t in Tasks)
            {
                t.DrawResult();
            }
        }
        private void DrawMainWindow()
        {

            ImGui.SetNextWindowSize(new Vector2(1000, 800), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Loot Master", ref Visible, ImGuiWindowFlags.NoScrollbar))
            {
                HandleAsync();
                //DrawLootHandlerButtons();
                if (ImGui.BeginTable("RaidGruppe", 14, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
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
                    foreach (Player player in Group.Players)
                    {
                        DrawPlayer(player);
                    }
                    ImGui.EndTable();
                }
                if (ImGui.Button("Close"))
                {
                    Hide();
                }
            }
            ImGui.End();
        }
        private void DrawLootHandlerButtons()
        {
            if (ImGui.Button("Loot Boss 1 (Erichthonios)"))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 1, Group);
                Childs.Add(lui);
                lui.Show();
            }
            ImGui.SameLine();
            if (ImGui.Button("Loot Boss 2 (Hippokampos)"))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 2, Group);
                Childs.Add(lui);
                lui.Show();
            }
            ImGui.SameLine();
            if (ImGui.Button("Loot Boss 3 (Phoinix)"))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 3, Group);
                Childs.Add(lui);
                lui.Show();
            }
            ImGui.SameLine();
            if (ImGui.Button("Loot Boss 4 (Hesperos)"))
            {
                LootUi lui = new(CuratedData.AsphodelosSavage, 4, Group);
                Childs.Add(lui);
                lui.Show();

            }
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
                ImGui.Text(gear.ItemLevel.ToString());
                ImGui.Text("+ " + (bis.ItemLevel - gear.ItemLevel));
                ImGui.Text(bis.ItemLevel.ToString());
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
                if (ImGui.Button("x##" + player.Pos))
                {
                    player.Reset();
                }
            }
            else
            {

                ImGui.TableNextColumn();
                ImGui.Text(player.Pos.ToString());
                ImGui.Text("No Player");
                for (int i = 0; i < 12; i++)
                {
                    ImGui.TableNextColumn();
                }
                ImGui.TableNextColumn();
                EditPlayerButton();
            }
            void EditPlayerButton()
            {
                if (ImGui.Button((PlayerExists ? "Edit" : "Add") + "## " + player.Pos))
                {
                    if (Childs.Exists(x => (x.GetType() == typeof(EditPlayerWindow)) && ((EditPlayerWindow)x).Pos == player.Pos))
                        return;
                    this.Childs.Add(new EditPlayerWindow(this, player.Pos));

                }
            }
            void DrawItem(GearItem item, GearItem bis)
            {
                ImGui.TableNextColumn();
                if (item.Valid)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new ColorHSVA(ILevelColor(item)) { S = 0.75f, V = 0.75f }.ToVec4);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new ColorHSVA(ILevelColor(item)) { S = 0.5f, V = 0.5f }.ToVec4);
                    ImGui.PushStyleColor(ImGuiCol.Text, Vec4(Color.Black));
                    if (ImGui.Button(item.Item.Name))
                    {
                        Childs.Add(new ShowItemWindow(item));
                    }
                    ImGui.PopStyleColor(3);
                }
                else
                {
                    ImGui.Text("Empty");
                }
                ImGui.NewLine();
                if (bis.Valid)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new ColorARGB(Color.White) { A = 0.7f }.ToVec4);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new ColorARGB(Color.White) { A = 0.9f }.ToVec4);
                    ImGui.PushStyleColor(ImGuiCol.Text, Vec4(Color.Black));
                    if (ImGui.Button(bis.Item.Name))
                    {
                        Childs.Add(new ShowItemWindow(bis));
                    }
                    ImGui.PopStyleColor(3);

                }
                else
                {
                    ImGui.Text("Empty");
                }
            }
        }
        class EditPlayerWindow : HrtUI
        {
            private readonly LootmasterUI LmUi;
            private readonly Player PlayerToAdd;
            private bool BISChanged = false;
            internal PositionInRaidGroup Pos => PlayerToAdd.Pos;

            internal EditPlayerWindow(LootmasterUI lmui, PositionInRaidGroup pos) : base()
            {
                this.LmUi = lmui;
                PlayerToAdd = this.LmUi.Group[pos];
                if (!PlayerToAdd.Filled && Helper.Target is not null)
                {
                    PlayerToAdd.MainChar.Name = Helper.Target.Name.TextValue;
                    PlayerToAdd.MainChar.MainClassType = Helper.TargetClass!;

                }
                Show();
            }

            public override void Draw()
            {
                if (!this.Visible)
                    return;
                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);
                if (ImGui.Begin("Edit Player " + PlayerToAdd.Pos, ref this.Visible,
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.InputText("Player Name", ref PlayerToAdd.NickName, 50);
                    ImGui.InputText("Character Name", ref PlayerToAdd.MainChar.Name, 50);

                    int mainClass = (int)PlayerToAdd.MainChar.MainClassType;
                    if (ImGui.Combo("Main Class", ref mainClass, Enum.GetNames(typeof(AvailableClasses)), Enum.GetNames(typeof(AvailableClasses)).Length))
                    {
                        this.PlayerToAdd.MainChar.MainClassType = (AvailableClasses)mainClass;
                    }

                    AvailableClasses? curClass = null;
                    if (PlayerToAdd.MainChar.Name.Equals(Target?.Name.TextValue))
                    {
                        if (Enum.TryParse(Target!.ClassJob.GameData!.Abbreviation, false, out AvailableClasses parsed))
                            curClass = parsed;
                    }
                    else if (PlayerToAdd.MainChar.Name.Equals(ClientState.LocalPlayer?.Name.TextValue))
                    {
                        if (Enum.TryParse(ClientState.LocalPlayer!.ClassJob.GameData!.Abbreviation, false, out AvailableClasses parsed))
                            curClass = parsed;
                    }
                    if (curClass is not null)
                    {
                        ImGui.SameLine();
                        if (ImGui.Button("Current"))
                        {
                            PlayerToAdd.MainChar.MainClassType = (AvailableClasses)curClass;
                        }
                    }
                    if (ImGui.InputText("BIS", ref PlayerToAdd.MainChar.MainClass.BIS.EtroID, 100))
                    {
                        BISChanged = true;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Default##BIS"))
                    {
                        if (!PlayerToAdd.MainChar.MainClass.BIS.EtroID.Equals(HRTPlugin.Configuration.DefaultBIS[PlayerToAdd.MainChar.MainClass.ClassType]))
                        {
                            BISChanged = true;
                            PlayerToAdd.MainChar.MainClass.BIS.EtroID = HRTPlugin.Configuration.DefaultBIS[PlayerToAdd.MainChar.MainClass.ClassType];
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Reset##BIS"))
                    {
                        if (!PlayerToAdd.MainChar.MainClass.BIS.EtroID.Equals(""))
                        {
                            PlayerToAdd.MainChar.MainClass.BIS.EtroID = "";
                            BISChanged = false;
                            PlayerToAdd.MainChar.MainClass.BIS.Clear();
                        }
                    }
                    if (ImGui.Button("Save"))
                    {
                        if (BISChanged)
                        {

                            LmUi.Tasks.Add(new((t) =>
                            {
                                Task<bool> task = (Task<bool>)t;
                                if (task.Result)
                                {
                                    ImGui.TextColored(Vec4(Color.Green), "BIS for " + PlayerToAdd.MainChar.Name + " succesfully updated");
                                }
                                else
                                {
                                    ImGui.TextColored(Vec4(Color.Red), "BIS update for " + PlayerToAdd.MainChar.Name + " failed");
                                }
                            }
                                , Task.Run(() => EtroConnector.GetGearSet(PlayerToAdd.MainChar.MainClass.BIS))));
                        }
                        this.Hide();
                    }
                }
                ImGui.End();
            }
        }
    }
    class LootUi : HrtUI
    {
        private readonly RaidGroup Group;
        private readonly RaidTier RaidTier;
        private readonly int Boss;
        private readonly (GearItem,int )[] Loot;
        private LootRuling LootRuling => HRTPlugin.Configuration.LootRuling;
        private List<HrtUI> Children = new();

        internal LootUi(RaidTier raidTier, int boss, RaidGroup group)
        {
            RaidTier = raidTier;
            Boss = boss;
            Group = group;
            Loot = LootDB.GetPossibleLoot(RaidTier, Boss).ConvertAll(x => (x, 0)).ToArray();
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var child in Children)
            {
                child.Hide();
                child.Dispose();
            }
            Children.Clear();
        }

        public override void Draw()
        {
            if (!Visible)
                return;
            if(ImGui.Begin("Loot for "+ RaidTier.Name +" Boss "+ Boss, ref Visible, ImGuiWindowFlags.NoCollapse)){
                for (int i = 0; i <Loot.Length; i++)
                {
                    if (Loot[i].Item1.Valid)
                    {
                        ImGui.Text(Loot[i].Item1.Item.Name);
                        ImGui.SameLine();
                        ImGui.InputInt("##"+Loot[i].Item1.Item.Name, ref Loot[i].Item2);

                    }
                }
                if (ImGui.Button("Distribute"))
                {
                    foreach((GearItem, int ) lootItem in Loot)
                    {
                        List<Player> alreadyLooted = new();
                        for (int j = 0; j < lootItem.Item2; j++)
                        {
                            var evalLoot = LootRuling.Evaluate(Group, lootItem.Item1.Slot, alreadyLooted);
                            alreadyLooted.Add(evalLoot[0].Item1);
                            var lootWindow = new LootResultWindow(lootItem.Item1, evalLoot);
                            Children.Add(lootWindow);
                            lootWindow.Show();
                        }
                    }
                }
                if (ImGui.Button("Close"))
                {
                    Hide();
                }
                ImGui.End();
            }
        }
        public override void Hide()
        {
            base.Hide();
            foreach (var win in Children)
                win.Hide();
        }
        class LootResultWindow : HrtUI
        {
            public LootResultWindow(GearItem item1, List<(Player, string)> looters)
            {
                Item = item1;
                Looters = looters;
            }

            public GearItem Item { get; }
            public List<(Player, string)> Looters { get; }

            private LootRuling LootRuling => HRTPlugin.Configuration.LootRuling;
            public override void Draw()
            {
                if (!Visible)
                    return;
                if(ImGui.Begin("Loot Results for: " + Item.Name, ref Visible))
                {
                    ImGui.Text("Loot Results for: " + Item.Name);
                    ImGui.Text("LootRuling used:");
                    foreach (LootRule rule in LootRuling.RuleSet)
                        ImGui.BulletText(rule.ToString());
                    int place = 1;
                    foreach ((Player, string) looter in Looters)
                    {
                        ImGui.Text("Priority " + place + " for Player " + looter.Item1.NickName + " won by rule " + looter.Item2);
                        place++;
                    }
                    ImGui.End();
                }
                
                
            }
        }
    }
    public class ShowItemWindow : HrtUI
    {
        private readonly GearItem Item;
        public ShowItemWindow(GearItem item) : base() => (Item, Visible) = (item, true);
        public override void Draw()
        {
            if (!Visible)
                return;
            ImGui.SetNextWindowSize(new Vector2(250, 250), ImGuiCond.Always);
            if (ImGui.Begin(Item.Item.Name, ref Visible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
            {

                if (ImGui.BeginTable("ItemTable", 2, ImGuiTableFlags.Borders))
                {
                    ImGui.TableSetupColumn("Header");
                    ImGui.TableSetupColumn("Value");
                    DrawRow("Name", Item.Item.Name);
                    DrawRow("Item Level", Item.ItemLevel.ToString());
                    DrawRow("Item Source", Item.Source.ToString());

                    ImGui.EndTable();
                }
            }
            static void DrawRow(string label, string value)
            {

                ImGui.TableNextColumn();
                ImGui.Text(label);
                ImGui.PushStyleColor(ImGuiCol.TableRowBg, new ColorHSVA(Color.White) { A = 0.5f }.ToVec4);
                ImGui.TableNextColumn();
                ImGui.Text(value);
                ImGui.PopStyleColor();
            }
        }
    }
}
