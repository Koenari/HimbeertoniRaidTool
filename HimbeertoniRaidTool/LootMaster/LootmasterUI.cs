using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using ColorHelper;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
            /**
             * Job Selection
             */
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
            /**
             * Stat Table
             */
            ImGui.NextColumn();
            {
                var playerRole = p.MainChar.MainClass.ClassType.GetRole();
                var mainStat = p.MainChar.MainClass.ClassType.MainStat();
                var weaponStat = (p.MainChar.MainClassType.GetRole() == Role.Healer || p.MainChar.MainClassType.GetRole() == Role.Caster) ?
                    StatType.MagicalDamage : StatType.PhysicalDamage;
                ImGui.TextColored(Vec4(ColorName.RedCrayola.ToRgb()),
                    Localize("StatsUnfinished", "Stats are under development and only work corrrectly for level 90 jobs"));

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
                if (playerRole == Role.Healer || playerRole == Role.Caster)
                {
                    DrawStatRow(StatType.SpellSpeed);
                    if (playerRole == Role.Healer)
                        DrawStatRow(StatType.Piety);
                    //These two are Magic to me -> Need Allagan Studies
                    //DrawStatRow(p.Gear, StatType.AttackMagicPotency);
                    //if (playerRole == Role.Healer)
                    //    DrawStatRow(p.Gear, StatType.HealingMagicPotency);
                }
                else
                {
                    DrawStatRow(StatType.SkillSpeed);
                    //See AMP and HMP
                    //DrawStatRow(p.Gear, StatType.AttackPower);
                    if (playerRole == Role.Tank)
                        DrawStatRow(StatType.Tenacity);
                }
                ImGui.EndTable();
                ImGui.NewLine();
                void DrawStatRow(StatType type)
                {
                    int numEvals = 1;
                    if (type == StatType.CriticalHit || type == StatType.Tenacity)
                        numEvals++;
                    ImGui.TableNextColumn();
                    ImGui.Text(type.FriendlyName());
                    if (type == StatType.CriticalHit)
                        ImGui.Text("Critical Damage");
                    //Current
                    ImGui.TableNextColumn();
                    ImGui.Text(Stat(p.Gear).ToString());
                    ImGui.TableNextColumn();
                    for (int i = 0; i < numEvals; i++)
                        ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, Stat(p.Gear), p.MainChar.MainClass.Level, p.MainChar.MainClassType, i));
                    //BiS
                    ImGui.TableNextColumn();
                    ImGui.Text(Stat(p.BIS).ToString());
                    ImGui.TableNextColumn();
                    for (int i = 0; i < numEvals; i++)
                        ImGui.Text(AllaganLibrary.EvaluateStatToDisplay(type, Stat(p.BIS), p.MainChar.MainClass.Level, p.MainChar.MainClassType, i));
                    int Stat(GearSet gear) => AllaganLibrary.GetStatWithModifiers(type, gear.GetStat(type), p.MainChar.MainClass.Level, p.MainChar.MainClassType, p.MainChar.Race, p.MainChar.Clan);
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
            if (ImGui.TabItemButton("+"))
            {
                ImGui.OpenPopup(newGroupContextMenuID);
            }
            if (ImGui.BeginPopupContextItem(newGroupContextMenuID))
            {
                if (ImGuiHelper.Button(Localize("From current Group", "From current Group"), null))
                {
                    RaidGroup group = new();
                    EditGroupWindow groupWindow = new EditGroupWindow(group, () => AddGroup(group, true), () => { });
                }
                if (ImGui.Button(Localize("From scratch", "From scratch")))
                {
                    RaidGroup group = new();
                    EditGroupWindow groupWindow = new EditGroupWindow(group, () => AddGroup(group, true), () => { });
                }
                ImGui.EndPopup();
            }
            ImGui.EndTabBar();
        }
        private static void AddGroup(RaidGroup group, bool getGroupInfos)
        {
            LootMaster.RaidGroups.Add(group);
            if (getGroupInfos)
            {
                group.Type = GroupType.Raid;
                List<PartyMember> players = new();
                List<PartyMember> fill = new();
                for (int i = 0; i < Services.PartyList.Length; i++)
                {
                    PartyMember? p = Services.PartyList[i];
                    if (p != null)
                        players.Add(p);
                }
                foreach (PartyMember p in players)
                {
                    if (!Enum.TryParse(p.ClassJob.GameData!.Abbreviation.RawString, out AvailableClasses c))
                        continue;
                    Role r = c.GetRole();
                    switch (r)
                    {
                        case Role.Tank:
                            if (!group[PositionInRaidGroup.Tank1].Filled)
                                FillPosition(PositionInRaidGroup.Tank1, p, c);
                            else if (!group[PositionInRaidGroup.Tank2].Filled)
                                FillPosition(PositionInRaidGroup.Tank2, p, c);
                            else
                                fill.Add(p);
                            break;
                        case Role.Healer:
                            if (!group[PositionInRaidGroup.Heal1].Filled)
                                FillPosition(PositionInRaidGroup.Heal1, p, c);
                            else if (!group[PositionInRaidGroup.Heal2].Filled)
                                FillPosition(PositionInRaidGroup.Heal2, p, c);
                            else
                                fill.Add(p);
                            break;
                        case Role.Melee:
                            if (!group[PositionInRaidGroup.Melee1].Filled)
                                FillPosition(PositionInRaidGroup.Melee1, p, c);
                            else if (!group[PositionInRaidGroup.Melee2].Filled)
                                FillPosition(PositionInRaidGroup.Melee2, p, c);
                            else
                                fill.Add(p);
                            break;
                        case Role.Caster:
                            if (!group[PositionInRaidGroup.Caster].Filled)
                                FillPosition(PositionInRaidGroup.Caster, p, c);
                            else
                                fill.Add(p);
                            break;
                        case Role.Ranged:
                            if (!group[PositionInRaidGroup.Ranged].Filled)
                                FillPosition(PositionInRaidGroup.Ranged, p, c);
                            else
                                fill.Add(p);
                            break;
                    }
                }
                foreach (PartyMember pm in fill)
                {
                    int pos = 0;
                    while (group[(PositionInRaidGroup)pos].Filled) { pos++; }
                    if (pos > 7) break;
                    FillPosition((PositionInRaidGroup)pos, pm, Enum.Parse<AvailableClasses>(pm.ClassJob.GameData!.Abbreviation.RawString));
                }
                void FillPosition(PositionInRaidGroup pos, PartyMember pm, AvailableClasses c)
                {

                    Player p = group[pos];
                    p.Pos = pos;
                    p.NickName = pm.Name.TextValue.Split(' ')[0];
                    Character character = new Character(pm.Name.TextValue, pm.World.GameData!.RowId);
                    bool characterExisted = DataManager.CharacterExists(character.HomeWorldID, character.Name);
                    DataManager.GetManagedCharacter(ref character);
                    p.MainChar = character;
                    if (!characterExisted)
                    {
                        p.MainChar.Classes.Clear();
                        p.MainChar.MainClassType = c;
                        PlayerCharacter? pc = Helper.TryGetChar(p.MainChar.Name, p.MainChar.HomeWorld);
                        if (pc != null)
                        {
                            p.MainChar.MainClass.Level = pc.Level;
                            GearSet BIS = new()
                            {
                                ManagedBy = GearSetManager.Etro,
                                EtroID = HRTPlugin.Configuration.GetDefaultBiS(c)
                            };
                            DataManager.GetManagedGearSet(ref BIS);
                            p.MainChar.MainClass.BIS = BIS;
                        }
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
                    if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, player.Pos.ToString(),
                        string.Format(Localize("PlayerDetails", "Show player details for  {0}"), player.NickName)))
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
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Search, player.Pos.ToString(), Localize("Add from DB", "Add from DB")))
                {
                    AddChild(new GetCharacterFromDBWindow(ref player));
                }
            }
        }
        private static void DrawSlot(GearItem item, GearItem bis, bool extended = false)
        {
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
                    ImGui.TextColored(
                        Vec4(Helper.ILevelColor(item).Saturation(0.8f).Value(0.85f), 1f),
                        $"{item.ItemLevel} {item.Source} {item.Slot.FriendlyName()}");
                    if (extended)
                    {
                        string materria = "";
                        foreach (HrtMateria mat in item.Materia)
                            materria += $"{mat.Category.GetAttribute<StatAttribute>()?.StatType.Abbrev()} +{mat.GetStat()}  ";
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
            LootSource[] currentLootSources = new LootSource[4];
            for (int i = 0; i < currentLootSources.Length; i++)
                currentLootSources[i] = new(CuratedData.CurrentRaidSavage, i + 1);
            foreach (var lootSource in currentLootSources)
            {
                if (ImGui.Button(lootSource.ToString()))
                {
                    LootSessionUI lui = new(lootSource, CurrentGroup);
                    lui.Show();
                }
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }
        private class PlayerdetailWindow : HrtUI
        {
            private readonly LootmasterUI Parent;
            private readonly Player P;
            public PlayerdetailWindow(LootmasterUI lmui, Player p)
            {
                Parent = lmui;
                P = p;
                Show();
            }
            protected override void Draw()
            {
                ImGui.SetNextWindowSize(new Vector2(1600, 600), ImGuiCond.Appearing);
                if (ImGui.Begin(Localize("PlayerDetailsTitle", "Player Details") + P.NickName, ref Visible,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize))
                {
                    Parent.DrawDetailedPlayer(P);
                    ImGui.End();
                }

            }
            public override bool Equals(object? obj)
            {
                if (!(obj?.GetType().IsAssignableTo(GetType()) ?? false))
                    return false;
                return Equals((PlayerdetailWindow)obj);
            }
            public bool Equals(PlayerdetailWindow obj)
            {
                return P.Equals(obj.P);
            }
            public override int GetHashCode()
            {
                return P.GetHashCode();
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
        internal GetCharacterFromDBWindow(ref Player p)
        {
            _p = p;
            Worlds = DataManager.GetWorldsWithCharacters().ToArray();
            WorldNames = Array.ConvertAll(Worlds, x => Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(x)?.Name.RawString ?? "");
            Show();
        }
        protected override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(350, 420), ImGuiCond.Appearing);
            if (ImGui.Begin(Localize("GetCharacterTitle", "Get character from DB") + _p.Pos, ref Visible,
                ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.InputText(Localize("Player Name", "Player Name"), ref NickName, 50);
                if (ImGui.ListBox("World", ref worldSelectIndex, WorldNames, WorldNames.Length))
                {
                    List<string> list = DataManager.GetCharacters(Worlds[worldSelectIndex]);
                    list.Sort();
                    CharacterNames = list.ToArray();
                }
                ImGui.ListBox("Name", ref CharacterNameIndex, CharacterNames, CharacterNames.Length);
                if (ImGuiHelper.Button(FontAwesomeIcon.Save, "save", Localize("Save", "Save")))
                {

                    _p.NickName = NickName;
                    Character c = _p.MainChar;
                    c.Name = CharacterNames[CharacterNameIndex];
                    c.HomeWorldID = Worlds[worldSelectIndex];
                    DataManager.GetManagedCharacter(ref c);
                    _p.MainChar = c;
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
            return Equals((GetCharacterFromDBWindow)obj);
        }
        public bool Equals(GetCharacterFromDBWindow obj)
        {
            return _p.Equals(obj._p);
        }
        public override int GetHashCode()
        {
            return _p.GetHashCode();
        }
    }
    internal class SwapPositionWindow : HrtUI
    {
        private readonly RaidGroup _group;
        private readonly PositionInRaidGroup _oldPos;
        private int _newPos;
        private readonly PositionInRaidGroup[] possiblePositions;
        private readonly string[] possiblePositionNames;
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
        public override int GetHashCode()
        {
            return _group.GetHashCode() << 3 + (int)_oldPos;
        }

    }
}
