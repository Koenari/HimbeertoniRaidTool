using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using ColorHelper;
using Dalamud.Game.ClientState.Party;
using Dalamud.Interface;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.DataManagement;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using static ColorHelper.HRTColorConversions;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.LootMaster
{
    public class LootmasterUI : HrtUI
    {
        private int _CurrenGroupIndex;
        private RaidGroup CurrentGroup => LootMaster.RaidGroups[_CurrenGroupIndex];
        private readonly List<AsyncTaskWithUiResult> Tasks = new();
        //private readonly List<HrtUI> Childs = new();
        public LootmasterUI() : base(false)
        {
            _CurrenGroupIndex = HRTPlugin.Configuration.LootmasterUiLastIndex;
            OnConfigChange();
            HRTPlugin.Configuration.ConfigurationChanged += OnConfigChange;

        }
        public void OnConfigChange()
        {
            HideInBattle = HRTPlugin.Configuration.LootMasterHideInBattle;
        }
        protected override void BeforeDispose()
        {
            HRTPlugin.Configuration.ConfigurationChanged -= OnConfigChange;
            HRTPlugin.Configuration.LootmasterUiLastIndex = _CurrenGroupIndex;
            HRTPlugin.Configuration.Save();
            foreach (AsyncTaskWithUiResult t in Tasks)
                t.Dispose();
            Tasks.Clear();
        }
        protected override void Draw()
        {
            if (!Services.ClientState.IsLoggedIn)
                return;
            DrawMainWindow();
        }

        private void HandleAsync()
        {
            Tasks.RemoveAll(t => t.FinishedShowing);
            foreach (AsyncTaskWithUiResult t in Tasks)
            {
                t.DrawResult();
            }
        }
        private void DrawDetailedPlayer(Player p)
        {
            ImGui.BeginChild("SoloView");
            ImGui.Columns(3);
            ImGui.Text($"{p.NickName} : {p.MainChar.Name} @ {p.MainChar.HomeWorld?.Name ?? "n.A"}");
            ImGui.SameLine();

            var playerChar = Helper.TryGetChar(p.MainChar.Name, p.MainChar.HomeWorld);
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, p.Pos.ToString(),
                    Localize("Inspect", "Update Gear"), playerChar is not null))
            {
                GearRefresherOnExamine.RefreshGearInfos(playerChar);
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "Solo", Localize("Edit", "Edit")))
            {
                var window = new EditPlayerWindow(out AsyncTaskWithUiResult callBack, CurrentGroup, PositionInRaidGroup.Tank1, true);
                if (AddChild(window))
                {
                    Tasks.Add(callBack);
                    window.Show();
                }
            }
            foreach (PlayableClass playableClass in p.MainChar.Classes)
            {
                if (ImGui.Button(playableClass.ClassType.ToString() + (p.MainChar.MainClassType == playableClass.ClassType ? " *" : "")))
                    p.MainChar.MainClassType = playableClass.ClassType;

                ImGui.SameLine();
                ImGui.Text("Level: " + playableClass.Level);
                ImGui.SameLine();
                ImGui.Text(Localize("iLvl", "iLvL: ") + playableClass.Gear.ItemLevel);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Redo, playableClass.BIS.EtroID,
                    string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), playableClass.BIS.Name), playableClass.BIS.EtroID.Length > 0))
                {
                    Tasks.Add(new(
                        (t) =>
                        {
                            if (((Task<bool>)t).Result)
                                ImGui.TextColored(Vec4(ColorName.Green),
                                        $"BIS for Character { p.MainChar.Name} ({playableClass.ClassType}) succesfully updated");
                            else
                                ImGui.TextColored(Vec4(ColorName.Red),
                                        $"BIS for Character { p.MainChar.Name} ({playableClass.ClassType}) failed");
                        },
                        Task.Run(() => EtroConnector.GetGearSet(playableClass.BIS))));
                }
            }

            ImGui.NextColumn();
            {
                var playerRole = p.MainChar.MainClass.ClassType.GetRole();
                var mainStat = p.MainChar.MainClass.ClassType.MainStat();
                var weaponStat = (p.MainChar.MainClassType.GetRole() == Role.Healer || p.MainChar.MainClassType.GetRole() == Role.Caster) ?
                    StatType.MagicalDamage : StatType.PhysicalDamage;
                ImGui.TextColored(Vec4(ColorName.RedCrayola.ToRgb()),
                    Localize("StatsUnfinished", "Stats are under development and may not include adjustments for class, race, etc."));

                ImGui.BeginTable("MainStats", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV);
                ImGui.TableSetupColumn(Localize("MainStats", "Main Stats"));
                ImGui.TableSetupColumn(Localize("Value", "Value"));
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();
                DrawStatRow(p.Gear, weaponStat);
                DrawStatRow(p.Gear, StatType.Vitality);
                DrawStatRow(p.Gear, mainStat);
                DrawStatRow(p.Gear, StatType.Defense);
                DrawStatRow(p.Gear, StatType.MagicDefense);
                ImGui.EndTable();
                ImGui.NewLine();
                ImGui.BeginTable("SecondaryStats", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV);
                ImGui.TableSetupColumn(Localize("SecondaryStats", "Secondary Stats"));
                ImGui.TableSetupColumn(Localize("Value", "Value"));
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();
                DrawStatRow(p.Gear, StatType.CriticalHit);
                DrawStatRow(p.Gear, StatType.Determination);
                DrawStatRow(p.Gear, StatType.DirectHitRate);
                if (playerRole == Role.Healer || playerRole == Role.Caster)
                {
                    DrawStatRow(p.Gear, StatType.SpellSpeed);
                    if (playerRole == Role.Healer)
                        DrawStatRow(p.Gear, StatType.Piety);
                    //Thease two are Magic to me -> Need Allagan Studies
                    //DrawStatRow(p.Gear, StatType.AttackMagicPotency);
                    //if (playerRole == Role.Healer)
                    //    DrawStatRow(p.Gear, StatType.HealingMagicPotency);
                }
                else
                {
                    DrawStatRow(p.Gear, StatType.SkillSpeed);
                    //See AMP and HMP
                    //DrawStatRow(p.Gear, StatType.AttackPower);
                    if (playerRole == Role.Tank)
                        DrawStatRow(p.Gear, StatType.Tenacity);
                }
                ImGui.EndTable();
                ImGui.NewLine();
                void DrawStatRow(GearSet gear, StatType type)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(type.FriendlyName());
                    ImGui.TableNextColumn();
                    ImGui.Text(Stat().ToString());
                    ImGui.TableNextColumn();
                    float evaluatedStat = AllaganLibraryMock.EvaluateStat(type, Stat());
                    if (!float.IsNaN(evaluatedStat))
                        ImGui.Text(evaluatedStat.ToString());
                    else
                        ImGui.Text("n.A.");

                    int Stat() => gear.GetStat(type) + AllaganLibraryMock.GetBaseStatAt90(type);
                }
            }
            ImGui.NextColumn();
            ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn("Gear");
            ImGui.TableSetupColumn("Gear");
            ImGui.TableHeadersRow();
            DrawSlot(p.Gear.MainHand, p.BIS.MainHand, true);
            DrawSlot(p.Gear.OffHand, p.BIS.OffHand, true);
            DrawSlot(p.Gear.Head, p.BIS.Head, true);
            DrawSlot(p.Gear.Ear, p.BIS.Ear, true);
            DrawSlot(p.Gear.Body, p.BIS.Body, true);
            DrawSlot(p.Gear.Neck, p.BIS.Neck, true);
            DrawSlot(p.Gear.Hands, p.BIS.Hands, true);
            DrawSlot(p.Gear.Wrist, p.BIS.Wrist, true);
            DrawSlot(p.Gear.Legs, p.BIS.Legs, true);
            DrawSlot(p.Gear.Ring1, p.BIS.Ring1, true);
            DrawSlot(p.Gear.Feet, p.BIS.Feet, true);
            DrawSlot(p.Gear.Ring2, p.BIS.Ring2, true);
            ImGui.EndTable();
            ImGui.EndChild();
        }
        private void DrawMainWindow()
        {
            if (_CurrenGroupIndex > LootMaster.RaidGroups.Count - 1 || _CurrenGroupIndex < 0)
                _CurrenGroupIndex = 0;
            ImGui.SetNextWindowSize(new Vector2(1600, 670), ImGuiCond.Appearing);
            if (ImGui.Begin(Localize("LootMasterWindowTitle", "Loot Master"), ref Visible,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize))
            {
                HandleAsync();
                DrawLootHandlerButtons();
                DrawRaidGroupSwitchBar();
                if (CurrentGroup.Type == GroupType.Solo)
                {
                    if (CurrentGroup.Tank1.MainChar.Filled)
                        DrawDetailedPlayer(CurrentGroup.Tank1);
                    else
                    {
                        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "Solo"))
                        {
                            var window = new EditPlayerWindow(out AsyncTaskWithUiResult callBack, CurrentGroup, PositionInRaidGroup.Tank1, true);
                            if (AddChild(window))
                            {
                                Tasks.Add(callBack);
                                window.Show();
                            }

                        }
                    }
                }
                else if (CurrentGroup.Type == GroupType.Raid || CurrentGroup.Type == GroupType.Group)
                {
                    if (ImGui.BeginTable("RaidGroup", 14,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
                    {
                        ImGui.TableSetupColumn(Localize("Player", "Player"));
                        ImGui.TableSetupColumn(Localize("itemLevelShort", "iLvl"));
                        ImGui.TableSetupColumn(Localize("Weapon", "Weapon"));
                        ImGui.TableSetupColumn(Localize("HeadGear", "Head"));
                        ImGui.TableSetupColumn(Localize("ChestGear", "Chest"));
                        ImGui.TableSetupColumn(Localize("Gloves", "Gloves"));
                        ImGui.TableSetupColumn(Localize("LegGear", "Legs"));
                        ImGui.TableSetupColumn(Localize("FeetGear", "Feet"));
                        ImGui.TableSetupColumn(Localize("Earrings", "Earrings"));
                        ImGui.TableSetupColumn(Localize("NeckGear", "Necklace"));
                        ImGui.TableSetupColumn(Localize("WristGear", "Bracelet"));
                        ImGui.TableSetupColumn(Localize("LeftRing", "Ring L"));
                        ImGui.TableSetupColumn(Localize("RightRing", "Ring R"));
                        ImGui.TableSetupColumn(Localize("Options", "Options"));
                        ImGui.TableHeadersRow();
                        foreach (Player player in CurrentGroup.Players)
                            DrawPlayer(player);
                        ImGui.EndTable();
                    }
                }
                else
                {
                    ImGui.TextColored(Vec4(ColorName.Red.ToRgb()), $"Gui for group type ({CurrentGroup.Type.FriendlyName()}) not yet implemented");
                }
            }
            ImGui.End();
        }

        private void DrawRaidGroupSwitchBar()
        {
            ImGui.BeginTabBar("RaidGroupSwichtBar");

            for (int tabBarIdx = 0; tabBarIdx < LootMaster.RaidGroups.Count; tabBarIdx++)
            {
                bool colorPushed = false;
                RaidGroup g = LootMaster.RaidGroups[tabBarIdx];
                if (tabBarIdx == _CurrenGroupIndex)
                {
                    ImGui.PushStyleColor(ImGuiCol.Tab, Vec4(ColorName.Redwood.ToHsv().Value(0.6f)));
                    colorPushed = true;
                }
                if (ImGui.TabItemButton($"{g.Name}##{tabBarIdx}"))
                    _CurrenGroupIndex = tabBarIdx;
                //0 is reserved for Solo on current Character (non editable)
                if (tabBarIdx > 0)
                {
                    if (ImGui.BeginPopupContextItem($"{g.Name}##{tabBarIdx}"))
                    {
                        if (ImGui.Button(Localize("Edit", "Edit")))
                        {
                            AddChild(new EditGroupWindow(g));
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SameLine();
                        if (ImGui.Button(Localize("Delete", "Delete")))
                        {
                            AddChild(new ConfimationDialog(
                                () => LootMaster.RaidGroups.Remove(g),
                                $"{Localize("DeleteRaidGroup", "Do you really want to delete following group:")} {g.Name}"));
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();

                    }
                }
                if (colorPushed)
                    ImGui.PopStyleColor();
            }
            const string newGroupContextMenuID = "NewGroupContextMenu";
            bool isInGroup = true;//Services.ClientState.LocalPlayer?.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.PartyMember) ?? false;
            if (ImGui.TabItemButton("+"))
            {
                if (isInGroup)
                    ImGui.OpenPopup(newGroupContextMenuID);
                else
                    AddGroup();

            }
            if (ImGui.BeginPopupContextItem(newGroupContextMenuID))
            {
                if (ImGuiHelper.Button(Localize("From current Group", "From current Group"), null, isInGroup))
                    AddGroup(true);
                if (ImGui.Button(Localize("From scratch", "From scratch")))
                    AddGroup();
                ImGui.EndPopup();
            }
            ImGui.EndTabBar();
        }
        private void AddGroup(bool getGroupInfos = false)
        {
            RaidGroup group = new();
            bool canceled = false;
            EditGroupWindow groupWindow = new EditGroupWindow(group, () => canceled = false, () => canceled = true);
            if (!canceled)
            {
                LootMaster.RaidGroups.Add(group);
                if (getGroupInfos)
                {
                    List<PartyMember> players = new();
                    for (int i = 0; i < Services.PartyList.Length; i++)
                    {
                        PartyMember? p = Services.PartyList[i];
                        if (p != null)
                            players.Add(p);
                    }
                    foreach (PartyMember p in players)
                    {

                    }
                }

            }
        }
        private void DrawPlayer(Player player)
        {
            bool PlayerExists = player.Filled && player.MainChar.Filled;
            if (PlayerExists)
            {

                ImGui.TableNextColumn();
                ImGui.Text($"{player.Pos}  { player.NickName}");
                ImGui.Text($"{ player.MainChar.Name} @ {player.MainChar.HomeWorld?.Name ?? "n.A."}");
                Character c = player.MainChar;

                if (player.MainChar.Classes.Count > 1)
                {
                    int playerClass = player.MainChar.Classes.FindIndex(x => x.ClassType == player.MainChar.MainClassType);
                    if (ImGui.Combo($"##Class{player.Pos}", ref playerClass, player.MainChar.Classes.ConvertAll(x => x.ClassType.ToString()).ToArray(),
                        player.MainChar.Classes.Count))
                        player.MainChar.MainClassType = player.MainChar.Classes[playerClass].ClassType;
                }
                else
                    ImGui.Text(player.MainChar.MainClassType.ToString());
                ImGui.SameLine();
                ImGui.Text(string.Format(Localize("LvLShort", "Lvl: {0}"), player.MainChar.MainClass.Level));
                GearSet gear = player.MainChar.MainClass.Gear;
                GearSet bis = player.MainChar.MainClass.BIS;
                ImGui.TableNextColumn();
                ImGui.Text(gear.ItemLevel.ToString());
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(gear.HrtID);
                ImGui.Text($"{bis.ItemLevel - gear.ItemLevel} {Localize("to BIS", "to BIS")}");
                ImGui.Text(bis.ItemLevel.ToString() + " (Etro)");
                if (ImGui.IsItemClicked())
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = EtroConnector.GearsetWebBaseUrl + bis.EtroID,
                        UseShellExecute = true,
                    });
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(EtroConnector.GearsetWebBaseUrl + bis.EtroID);
                DrawSlot(gear.MainHand, bis.MainHand);
                DrawSlot(gear.Head, bis.Head);
                DrawSlot(gear.Body, bis.Body);
                DrawSlot(gear.Hands, bis.Hands);
                DrawSlot(gear.Legs, bis.Legs);
                DrawSlot(gear.Feet, bis.Feet);
                DrawSlot(gear.Ear, bis.Ear);
                DrawSlot(gear.Neck, bis.Neck);
                DrawSlot(gear.Wrist, bis.Wrist);
                if (gear.Ring1.ID == bis.Ring2.ID || gear.Ring2.ID == bis.Ring1.ID)
                {
                    DrawSlot(gear.Ring1, bis.Ring2);
                    DrawSlot(gear.Ring2, bis.Ring1);
                }
                else
                {
                    DrawSlot(gear.Ring1, bis.Ring1);
                    DrawSlot(gear.Ring2, bis.Ring2);
                }
                ImGui.TableNextColumn();
                /**
                 * Start of functional button section
                 */
                {
                    //See if Cahracter is in range to inspect and interact
                    var playerChar = Helper.TryGetChar(player.MainChar.Name, player.MainChar.HomeWorld);
                    //Code to correct characters created before homeworld was tracked (should be obsolete by now)
                    if (c.HomeWorldID == 0 && playerChar is null)
                    {
                        playerChar = Helper.TryGetChar(player.MainChar.Name);
                        if (playerChar is not null && !DataManager.CharacterExists(playerChar.HomeWorld.Id, player.MainChar.Name))
                        {
                            uint worldID = player.MainChar.HomeWorldID;
                            player.MainChar.HomeWorldID = playerChar.HomeWorld.Id;
                            DataManager.RearrangeCharacter(worldID, player.MainChar.Name, ref c);
                        }
                    }
                    if (ImGuiHelper.Button(FontAwesomeIcon.Search, player.Pos.ToString(),
                        Localize("Inspect", "Update Gear"), playerChar is not null))
                    {
                        GearRefresherOnExamine.RefreshGearInfos(playerChar);
                    }
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.ArrowsAltV, $"Rearrange{player.Pos}", "Swap Position"))
                    {
                        AddChild(new SwapPositionWindow(player.Pos, CurrentGroup));
                    }
                    ImGui.SameLine();


                    if (ImGuiHelper.Button(FontAwesomeIcon.Edit, player.Pos.ToString(),
                        string.Format(Localize("Edit", "Edit {0}"), player.NickName)))
                    {
                        EditPlayerWindow editWindow = new(out AsyncTaskWithUiResult result, CurrentGroup, player.Pos, true);
                        if (AddChild(editWindow))
                        {
                            Tasks.Add(result);
                            editWindow.Show();
                        }
                    }
                    if (ImGuiHelper.Button(FontAwesomeIcon.Redo, player.BIS.EtroID,
                        string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), player.BIS.Name)))
                    {
                        Tasks.Add(new(
                            (t) =>
                            {
                                if (((Task<bool>)t).Result)
                                    ImGui.TextColored(Vec4(ColorName.Green),
                                            $"BIS for Character { player.MainChar.Name} ({player.MainChar.MainClassType}) succesfully updated");
                                else
                                    ImGui.TextColored(Vec4(ColorName.Red),
                                            $"BIS for Character { player.MainChar.Name} ({player.MainChar.MainClassType}) failed");
                            },
                            Task.Run(() => EtroConnector.GetGearSet(player.BIS))));
                    }
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, player.Pos.ToString(),
                        string.Format(Localize("Delete {0}", "Delete {0}"), player.NickName)))
                    {
                        AddChild(new ConfimationDialog(
                            () => player.Reset(),
                            string.Format(Localize("DeletePlayerConfirmation", "Do you really want to delete player:\"{0}\" "), player.NickName)));
                    }

                }
            }
            else
            {

                ImGui.TableNextColumn();
                ImGui.Text(player.Pos.ToString());
                ImGui.Text(Localize("No Player", "No Player"));
                for (int i = 0; i < 12; i++)
                {
                    ImGui.TableNextColumn();
                }
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, player.Pos.ToString(), Localize("Add", "Add")))
                {
                    EditPlayerWindow editWindow = new(out AsyncTaskWithUiResult result, CurrentGroup, player.Pos, true);
                    if (AddChild(editWindow))
                    {
                        Tasks.Add(result);
                        editWindow.Show();
                    }
                }
            }
        }
        private static void DrawSlot(GearItem item, GearItem bis, bool extended = false)
        {
            //Icon drawing still broken
            extended = false;
            ImGui.TableNextColumn();
            if (item.Valid && bis.Valid && item.Equals(bis))
            {
                ImGui.NewLine();
                DrawItem(item, extended);
                ImGui.NewLine();
            }
            else
            {
                DrawItem(item, extended);
                ImGui.NewLine();
                DrawItem(bis, extended);
            }
            static void DrawItem(GearItem item, bool extended)
            {
                if (item.Valid)
                {
                    ImGui.BeginGroup();
                    if (extended && item.Icon != null)
                    {
                        ImGui.Image(item.Icon.ImGuiHandle, new Vector2(32));
                        ImGui.SameLine();
                    }
                    ImGui.TextColored(
                        Vec4(Helper.ILevelColor(item).Saturation(0.8f).Value(0.85f), 1f),
                        $"{item.ItemLevel} {item.Source} {item.Slot.FriendlyName()}");
                    ImGui.EndGroup();
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        item.Draw();
                        ImGui.EndTooltip();
                    }
                }
                else
                    ImGui.Text(Localize("Empty", "Empty"));

            }
        }
        private void DrawLootHandlerButtons()
        {
            LootSource[] currentLootSources = new LootSource[4];
            for (int i = 0; i < currentLootSources.Length; i++)
                currentLootSources[i] = new(CuratedData.CurrentRaidSavage, i + 1);
            foreach (var lootSource in currentLootSources)
            {
                if (ImGui.Button(lootSource.ToString()))
                {
                    LootSessionUI lui = new(lootSource, CurrentGroup);
                    //Childs.Add(lui);
                    lui.Show();
                }
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }
    }
    internal class SwapPositionWindow : HrtUI
    {
        private readonly RaidGroup _group;
        private readonly PositionInRaidGroup _oldPos;
        private int _newPos;
        private PositionInRaidGroup[] possiblePositions;
        private string[] possiblePositionNames;
        internal SwapPositionWindow(PositionInRaidGroup pos, RaidGroup g) : base()
        {
            _group = g;
            _oldPos = pos;
            _newPos = 0;
            List<PositionInRaidGroup> positions = new(Enum.GetValues<PositionInRaidGroup>());
            positions.RemoveAll(x => !x.IsPartOf(_group.Type));
            positions.RemoveAll(x => x == _oldPos);
            possiblePositions = positions.ToArray();
            possiblePositionNames = positions.ConvertAll(position => $"{_group[position].NickName} ({position})").ToArray();
            Show();
        }
        protected override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(170f, _group.Type == GroupType.Raid ? 230f : 150f));
            if (ImGui.Begin("Swap Position of " + _group[_oldPos].NickName, ref Visible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.ListBox("", ref _newPos, possiblePositionNames, possiblePositions.Length);
                if (ImGuiHelper.Button("Swap"))
                {
                    PositionInRaidGroup newPos = possiblePositions[_newPos];
                    (_group[_oldPos], _group[newPos]) = (_group[newPos], _group[_oldPos]);
                    _group[_oldPos].Pos = _oldPos;
                    _group[newPos].Pos = newPos;
                    Hide();
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.WindowClose, "cancel", Localize("Cancel", "Cancel")))
                    Hide();
                ImGui.End();
            }
        }
        public override bool Equals(object? obj)
        {
            if (!(obj?.GetType().IsAssignableTo(GetType()) ?? false))
                return false;
            return Equals((SwapPositionWindow)obj);
        }
        public bool Equals(SwapPositionWindow other)
        {
            if (!_group.Equals(other._group))
                return false;
            if (_oldPos != other._oldPos)
                return false;

            return true;
        }

    }
}
