﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using ColorHelper;
using Dalamud.Interface;
using HimbeertoniRaidTool.Connectors;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using static ColorHelper.HRTColorConversions;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    internal class LootmasterUI : HrtUI
    {
        private readonly LootMasterModule _lootMaster;
        private int _CurrenGroupIndex;
        protected override bool HideInBattle => _lootMaster.Configuration.Data.HideInBattle;
        private RaidGroup CurrentGroup => RaidGroups[_CurrenGroupIndex];
        private static List<RaidGroup> RaidGroups => Services.HrtDataManager.Groups;
        private readonly List<AsyncTaskWithUiResult> Tasks = new();
        internal LootmasterUI(LootMasterModule lootMaster) : base(false, "LootMaster")
        {
            _lootMaster = lootMaster;
            _CurrenGroupIndex = 0;
            Size = new Vector2(1600, 670);
            Title = Localize("LootMasterWindowTitle", "Loot Master");
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize;

        }
        protected override void BeforeDispose()
        {
            _lootMaster.Configuration.Data.LastGroupIndex = _CurrenGroupIndex;
            _lootMaster.Configuration.Save();
            foreach (var t in Tasks)
                t.Dispose();
            Tasks.Clear();
        }
        protected override void OnShow()
        {
            _CurrenGroupIndex = _lootMaster.Configuration.Data.LastGroupIndex;
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
            foreach (var t in Tasks)
            {
                t.DrawResult();
            }
        }
        private void DrawDetailedPlayer(Player p)
        {
            var curClass = p.MainChar.MainClass;
            ImGui.BeginChild("SoloView");
            ImGui.Columns(3);
            /**
             * Job Selection
             */
            ImGui.Text($"{p.NickName} : {p.MainChar.Name} @ {p.MainChar.HomeWorld?.Name ?? "n.A"}");
            ImGui.SameLine();

            ImGuiHelper.GearUpdateButton(p);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditPlayer{p.NickName}{p.Pos}", $"{Localize("Edit player", "Edit player")} {p.NickName}"))
            {
                var window = new EditPlayerWindow(out var callBack, CurrentGroup, p.Pos, _lootMaster.Configuration.Data.GetDefaultBiS);
                if (AddChild(window))
                {
                    Tasks.Add(callBack);
                    window.Show();
                }
            }
            foreach (var playableClass in p.MainChar.Classes)
            {
                bool isMainJob = p.MainChar.MainJob == playableClass.Job;

                if (isMainJob)
                    ImGui.PushStyleColor(ImGuiCol.Button, Vec4(ColorName.Redwood.ToHsv().Value(0.6f)));
                if (ImGuiHelper.Button(playableClass.Job.ToString(), null, true, new Vector2(38f, 0f)))
                    p.MainChar.MainJob = playableClass.Job;
                if (isMainJob)
                    ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.Text("Level: " + playableClass.Level);
                //Current Gear
                ImGui.SameLine();
                ImGui.Text($"{Localize("Current", "Current")} {Localize("iLvl", "iLvL: ")}{playableClass.Gear.ItemLevel:D3}");
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditGear{playableClass.Job}", $"{Localize("Edit", "Edit")} {playableClass.Job} {Localize("gear", "gear")}"))
                    AddChild(new EditGearSetWindow(playableClass.Gear, playableClass.Job, _lootMaster.Configuration.Data.SelectedRaidTier), true);
                //BiS
                ImGui.SameLine();
                ImGui.Text($"{Localize("BiS", "BiS")} {Localize("iLvl", "iLvL: ")}{playableClass.BIS.ItemLevel:D3}");
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditBIS{playableClass.Job}", $"{Localize("Edit", "Edit")} {playableClass.BIS.Name}"))
                    AddChild(new EditGearSetWindow(playableClass.BIS, playableClass.Job, _lootMaster.Configuration.Data.SelectedRaidTier), true);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Redo, playableClass.BIS.EtroID,
                    string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), playableClass.BIS.Name), playableClass.BIS.EtroID.Length > 0))
                {
                    Tasks.Add(new(
                        (t) =>
                        {
                            if (((Task<bool>)t).Result)
                                ImGui.TextColored(Vec4(ColorName.Green),
                                        $"BIS for Character {p.MainChar.Name} ({playableClass.Job}) succesfully updated");
                            else
                                ImGui.TextColored(Vec4(ColorName.Red),
                                        $"BIS for Character {p.MainChar.Name} ({playableClass.Job}) failed");
                        },
                        Task.Run(() => EtroConnector.GetGearSet(playableClass.BIS))));
                }
            }
            /**
             * Stat Table
             */
            ImGui.NextColumn();
            if (curClass is not null)
            {
                var curJob = curClass.Job;
                var curRole = curJob.GetRole();
                var mainStat = curJob.MainStat();
                var weaponStat = curRole == Role.Healer || curRole == Role.Caster ? StatType.MagicalDamage : StatType.PhysicalDamage;
                var potencyStat = curRole == Role.Healer || curRole == Role.Caster ? StatType.AttackMagicPotency : StatType.AttackPower;
                ImGui.TextColored(Vec4(ColorName.RedCrayola.ToRgb()),
                    Localize("StatsUnfinished", "Stats are under development and only work corrrectly for level 70/80/90 jobs"));

                ImGui.BeginTable("MainStats", 5, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV);
                ImGui.TableSetupColumn(Localize("MainStats", "Main Stats"));
                ImGui.TableSetupColumn(Localize("Current", "Current"));
                ImGui.TableSetupColumn("");
                ImGui.TableSetupColumn(Localize("BiS", "BiS"));
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();
                DrawStatRow(weaponStat);
                DrawStatRow(StatType.Vitality);
                DrawStatRow(mainStat);
                DrawStatRow(StatType.Defense);
                DrawStatRow(StatType.MagicDefense);
                ImGui.EndTable();
                ImGui.NewLine();
                ImGui.BeginTable("SecondaryStats", 5, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersOuterV);
                ImGui.TableSetupColumn(Localize("SecondaryStats", "Secondary Stats"));
                ImGui.TableSetupColumn(Localize("Current", "Current"));
                ImGui.TableSetupColumn("");
                ImGui.TableSetupColumn(Localize("BiS", "BiS"));
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();
                DrawStatRow(StatType.CriticalHit);
                DrawStatRow(StatType.Determination);
                DrawStatRow(StatType.DirectHitRate);
                if (curRole == Role.Healer || curRole == Role.Caster)
                {
                    DrawStatRow(StatType.SpellSpeed);
                    if (curRole == Role.Healer)
                        DrawStatRow(StatType.Piety);
                }
                else
                {
                    DrawStatRow(StatType.SkillSpeed);
                    if (curRole == Role.Tank)
                        DrawStatRow(StatType.Tenacity);
                }
                ImGui.EndTable();
                ImGui.NewLine();
                void DrawStatRow(StatType type)
                {
                    int numEvals = 1;
                    if (type == StatType.CriticalHit || type == StatType.Tenacity || type == StatType.SpellSpeed || type == StatType.SkillSpeed)
                        numEvals++;
                    ImGui.TableNextColumn();
                    ImGui.Text(type.FriendlyName());
                    if (type == StatType.CriticalHit)
                        ImGui.Text("Critical Damage");
                    //Current
                    ImGui.TableNextColumn();
                    ImGui.Text(curClass.GetCurrentStat(type).ToString());
                    ImGui.TableNextColumn();
                    for (int i = 0; i < numEvals; i++)
                        ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, curClass, false, i));
                    if (type == weaponStat && ImGui.IsItemHovered())
                        ImGui.SetTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
                    //BiS
                    ImGui.TableNextColumn();
                    ImGui.Text(curClass.GetBiSStat(type).ToString());
                    ImGui.TableNextColumn();
                    for (int i = 0; i < numEvals; i++)
                        ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, curClass, true, i));
                    if (type == weaponStat && ImGui.IsItemHovered())
                        ImGui.SetTooltip(Localize("Dmgper100Tooltip", "Average Dmg with a 100 potency skill"));
                }
            }
            /**
             * Show Gear
             */
            ImGui.NextColumn();
            ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
            ImGui.TableSetupColumn("Gear");
            ImGui.TableSetupColumn("Gear");
            ImGui.TableHeadersRow();
            bool ringsSwapped = p.Gear.Ring1.ID == p.BIS.Ring2.ID || p.Gear.Ring2.ID == p.BIS.Ring1.ID;
            DrawSlot(p.Gear.MainHand, p.BIS.MainHand, true);
            DrawSlot(p.Gear.OffHand, p.BIS.OffHand, true);
            DrawSlot(p.Gear.Head, p.BIS.Head, true);
            DrawSlot(p.Gear.Ear, p.BIS.Ear, true);
            DrawSlot(p.Gear.Body, p.BIS.Body, true);
            DrawSlot(p.Gear.Neck, p.BIS.Neck, true);
            DrawSlot(p.Gear.Hands, p.BIS.Hands, true);
            DrawSlot(p.Gear.Wrist, p.BIS.Wrist, true);
            DrawSlot(p.Gear.Legs, p.BIS.Legs, true);
            DrawSlot(p.Gear.Ring1, ringsSwapped ? p.BIS.Ring2 : p.BIS.Ring1, true);
            DrawSlot(p.Gear.Feet, p.BIS.Feet, true);
            DrawSlot(p.Gear.Ring2, ringsSwapped ? p.BIS.Ring1 : p.BIS.Ring2, true);
            ImGui.EndTable();
            ImGui.EndChild();
        }
        private void DrawMainWindow()
        {
            if (_CurrenGroupIndex > RaidGroups.Count - 1 || _CurrenGroupIndex < 0)
                _CurrenGroupIndex = 0;
            HandleAsync();
            DrawLootHandlerButtons();
            DrawRaidGroupSwitchBar();
            if (CurrentGroup.Type == GroupType.Solo)
            {
                if (CurrentGroup.Tank1.MainChar.Filled)
                    DrawDetailedPlayer(CurrentGroup.Tank1);
                else
                {
                    if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "Solo", Localize("Add Player", "Add Player")))
                    {
                        var window = new EditPlayerWindow(out var callBack, CurrentGroup, PositionInRaidGroup.Tank1, _lootMaster.Configuration.Data.GetDefaultBiS);
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
                    foreach (var player in CurrentGroup.Players)
                        DrawPlayer(player);
                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.TextColored(Vec4(ColorName.Red.ToRgb()), $"Gui for group type ({CurrentGroup.Type.FriendlyName()}) not yet implemented");
            }
        }

        private void DrawRaidGroupSwitchBar()
        {
            ImGui.BeginTabBar("RaidGroupSwichtBar");

            for (int tabBarIdx = 0; tabBarIdx < RaidGroups.Count; tabBarIdx++)
            {
                bool colorPushed = false;
                RaidGroup g = RaidGroups[tabBarIdx];
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
                        if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditGroup", Localize("Edit group", "Edit group")))
                        {
                            AddChild(new EditGroupWindow(g));
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SameLine();
                        if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, "DeleteGroup", Localize("Delete group", "Delete group")))
                        {
                            AddChild(new ConfimationDialog(
                                () => RaidGroups.Remove(g),
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
            if (ImGui.TabItemButton("+"))
            {
                ImGui.OpenPopup(newGroupContextMenuID);
            }
            if (ImGui.BeginPopupContextItem(newGroupContextMenuID))
            {
                if (ImGuiHelper.Button(Localize("From current Group", "From current Group"), null))
                {
                    RaidGroup group = new();
                    group.Name = "AutoCreated";
                    _lootMaster.AddGroup(group, true);
                    ImGui.CloseCurrentPopup();
                }
                if (ImGuiHelper.Button(Localize("From scratch", "From scratch"), Localize("Add emtpy group", "Add emtpy group")))
                {
                    RaidGroup group = new();
                    var groupWindow = new EditGroupWindow(group, () => _lootMaster.AddGroup(group, false), () => { });
                }
                ImGui.EndPopup();
            }
            ImGui.EndTabBar();
        }

        private void DrawPlayer(Player player)
        {
            bool playerExists = player.Filled && player.MainChar.Filled;
            bool hasClasses = playerExists && player.MainChar.Classes.Count > 0;
            if (playerExists)
            {

                ImGui.TableNextColumn();
                ImGui.Text($"{player.Pos}:");
                ImGui.SameLine();
                ImGui.SetCursorPosX(60f);
                ImGui.Text($"{player.NickName}");
                ImGui.Text($"{player.MainChar.Name} @ {player.MainChar.HomeWorld?.Name ?? "n.A."}");
                var c = player.MainChar;
                if (hasClasses)
                {
                    if (player.MainChar.Classes.Count > 1)
                    {
                        int playerClass = player.MainChar.Classes.FindIndex(x => x.Job == player.MainChar.MainJob);
                        if (ImGui.Combo($"##Class{player.Pos}", ref playerClass, player.MainChar.Classes.ConvertAll(x => x.Job.ToString()).ToArray(),
                            player.MainChar.Classes.Count))
                            player.MainChar.MainJob = player.MainChar.Classes[playerClass].Job;
                    }
                    else
                    {
                        ImGui.Text(player.MainChar.MainJob.ToString());
                    }
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(110f);
                    ImGui.Text(string.Format(Localize("LvLShort", "Lvl: {0}"), player.MainChar.MainClass?.Level ?? 1));
                    var gear = player.Gear;
                    var bis = player.BIS;
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
                }
                else
                {
                    ImGui.Text("");
                    for (int i = 0; i < 13; i++)
                        ImGui.TableNextColumn();
                }
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
                        if (playerChar is not null && !Services.HrtDataManager.CharacterExists(playerChar.HomeWorld.Id, player.MainChar.Name))
                        {
                            uint worldID = player.MainChar.HomeWorldID;
                            player.MainChar.HomeWorldID = playerChar.HomeWorld.Id;
                            Services.HrtDataManager.RearrangeCharacter(worldID, player.MainChar.Name, ref c);
                        }
                    }
                    ImGuiHelper.GearUpdateButton(player);
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.ArrowsAltV, $"Rearrange{player.Pos}", "Swap Position"))
                    {
                        AddChild(new SwapPositionWindow(player.Pos, CurrentGroup));
                    }
                    ImGui.SameLine();


                    if (ImGuiHelper.Button(FontAwesomeIcon.Edit, player.Pos.ToString(),
                        string.Format(Localize("Edit {0}", "Edit {0}"), player.NickName)))
                    {
                        EditPlayerWindow editWindow = new(out var result, CurrentGroup, player.Pos, _lootMaster.Configuration.Data.GetDefaultBiS);
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
                                            $"BIS for Character {player.MainChar.Name} ({player.MainChar.MainJob}) succesfully updated");
                                else
                                    ImGui.TextColored(Vec4(ColorName.Red),
                                            $"BIS for Character {player.MainChar.Name} ({player.MainChar.MainJob}) failed");
                            },
                            Task.Run(() => EtroConnector.GetGearSet(player.BIS))));
                    }
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, player.Pos.ToString(),
                        string.Format(Localize("PlayerDetails", "Show player details for {0}"), player.NickName)))
                    {
                        AddChild(new PlayerdetailWindow(this, player));
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
                    ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, player.Pos.ToString(), Localize("Add", "Add")))
                    if (AddChild(new EditPlayerWindow(out var result, CurrentGroup, player.Pos, _lootMaster.Configuration.Data.GetDefaultBiS), true))
                        Tasks.Add(result);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Search, player.Pos.ToString(), Localize("Add from DB", "Add from DB")))
                    AddChild(new GetCharacterFromDBWindow(ref player), true);
            }
        }
        private void DrawSlot(GearItem item, GearItem bis, bool extended = false)
        {
            ImGui.TableNextColumn();
            if (item.Filled && bis.Filled && item.Equals(bis))
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
            void DrawItem(GearItem item, bool extended)
            {
                if (item.Filled)
                {
                    ImGui.BeginGroup();
                    ImGui.TextColored(
                        Vec4(Helper.ILevelColor(item, _lootMaster.Configuration.Data.SelectedRaidTier.ArmorItemLevel).Saturation(0.8f).Value(0.85f), 1f),
                        $"{item.ItemLevel} {item.Source} {item.Slot.FriendlyName()}");
                    if (extended)
                    {
                        string materria = "";
                        foreach (var mat in item.Materia)
                            materria += $"{mat.StatType.Abbrev()} +{mat.GetStat()}  ";
                        ImGui.SameLine();
                        ImGui.Text($"(  {materria})");
                    }
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
            var currentLootSources = new LootSource[4];
            int selectedTier = Array.IndexOf(CuratedData.RaidTiers, _lootMaster.Configuration.Data.SelectedRaidTier);
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("##Raid Tier", ref selectedTier, Array.ConvertAll(CuratedData.RaidTiers, x => x.Name), CuratedData.RaidTiers.Length))
            {
                if (selectedTier != Array.IndexOf(CuratedData.RaidTiers, CuratedData.CurrentRaidSavage))
                    _lootMaster.Configuration.Data.RaidTierOverride = CuratedData.RaidTiers[selectedTier];
                else
                    _lootMaster.Configuration.Data.RaidTierOverride = null;
            }
            ImGui.SameLine();
            ImGui.Text(Localize("Distribute loot for:", "Distribute loot for:"));
            ImGui.SameLine();
            for (int i = 0; i < currentLootSources.Length; i++)
                currentLootSources[i] = new(_lootMaster.Configuration.Data.SelectedRaidTier, i + 1);

            foreach (var lootSource in currentLootSources)
            {
                if (ImGuiHelper.Button(lootSource.ToString(), null))
                {
                    LootSessionUI lui = new(lootSource, CurrentGroup, _lootMaster.Configuration.Data.LootRuling);
                    lui.Show();
                }
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }

        internal void HandleMessage(HrtUiMessage message)
        {
            throw new NotImplementedException();
        }

        private class PlayerdetailWindow : HrtUI
        {
            private readonly LootmasterUI Parent;
            private readonly Player P;
            public PlayerdetailWindow(LootmasterUI lmui, Player p) : base(true, $"PlayerdetailWindow{p.NickName}")
            {
                Parent = lmui;
                P = p;
                Show();
                Title = Localize("PlayerDetailsTitle", "Player Details") + P.NickName;
                (Size, SizingCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);
            }
            protected override void Draw()
            {
                Parent.DrawDetailedPlayer(P);
            }
        }
    }
    internal class GetCharacterFromDBWindow : HrtUI
    {
        private readonly Player _p;
        private readonly uint[] Worlds;
        private readonly string[] WorldNames;
        private int worldSelectIndex;
        private string[] CharacterNames = Array.Empty<string>();
        private int CharacterNameIndex = 0;
        private string NickName = " ";
        internal GetCharacterFromDBWindow(ref Player p) : base(true, $"GetCharacterFromDBWindow{p.NickName}")
        {
            _p = p;
            Worlds = Services.HrtDataManager.GetWorldsWithCharacters().ToArray();
            WorldNames = Array.ConvertAll(Worlds, x => Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(x)?.Name.RawString ?? "");
            Title = Localize("GetCharacterTitle", "Get character from DB") + _p.Pos;
            Size = new Vector2(350, 420);
            WindowFlags = ImGuiWindowFlags.NoScrollbar;
        }
        protected override void Draw()
        {
            ImGui.InputText(Localize("Player Name", "Player Name"), ref NickName, 50);
            if (ImGui.ListBox("World", ref worldSelectIndex, WorldNames, WorldNames.Length))
            {
                var list = Services.HrtDataManager.GetCharacters(Worlds[worldSelectIndex]);
                list.Sort();
                CharacterNames = list.ToArray();
            }
            ImGui.ListBox("Name", ref CharacterNameIndex, CharacterNames, CharacterNames.Length);
            if (ImGuiHelper.Button(FontAwesomeIcon.Save, "save", Localize("Save", "Save")))
            {

                _p.NickName = NickName;
                var c = _p.MainChar;
                c.Name = CharacterNames[CharacterNameIndex];
                c.HomeWorldID = Worlds[worldSelectIndex];
                Services.HrtDataManager.GetManagedCharacter(ref c);
                _p.MainChar = c;
                Hide();
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.WindowClose, "cancel", Localize("Cancel", "Cancel")))
                Hide();
        }
    }
    internal class SwapPositionWindow : HrtUI
    {
        private readonly RaidGroup _group;
        private readonly PositionInRaidGroup _oldPos;
        private int _newPos;
        private readonly PositionInRaidGroup[] possiblePositions;
        private readonly string[] possiblePositionNames;
        internal SwapPositionWindow(PositionInRaidGroup pos, RaidGroup g) : base(true, $"SwapPositionWindow{g.GetHashCode()}{pos}")
        {
            _group = g;
            _oldPos = pos;
            _newPos = 0;
            List<PositionInRaidGroup> positions = new(Enum.GetValues<PositionInRaidGroup>());
            positions.RemoveAll(x => !x.IsPartOf(_group.Type));
            positions.RemoveAll(x => x == _oldPos);
            possiblePositions = positions.ToArray();
            possiblePositionNames = positions.ConvertAll(position => $"{_group[position].NickName} ({position})").ToArray();
            Size = new Vector2(170f, _group.Type == GroupType.Raid ? 230f : 150f);
            Title = $"{Localize("Swap Position of", "Swap Position of")} {_group[_oldPos].NickName}";
            WindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
        }
        protected override void Draw()
        {
            if (ImGuiHelper.SaveButton(Localize("Swap players positions", "Swap players positions")))
            {
                var newPos = possiblePositions[_newPos];
                if (newPos != _oldPos)
                {
                    (_group[_oldPos], _group[newPos]) = (_group[newPos], _group[_oldPos]);
                    _group[_oldPos].Pos = _oldPos;
                    _group[newPos].Pos = newPos;
                }
                Hide();
            }
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
                Hide();
            ImGui.ListBox("", ref _newPos, possiblePositionNames, possiblePositions.Length);


        }
    }
}