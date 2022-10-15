using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
    internal class LootmasterUI : Window
    {
        private readonly LootMasterModule _lootMaster;
        private int _CurrenGroupIndex;
        protected override bool HideInBattle => _lootMaster.Configuration.Data.HideInBattle;
        private RaidGroup CurrentGroup => RaidGroups[_CurrenGroupIndex];
        private static List<RaidGroup> RaidGroups => Services.HrtDataManager.Groups;
        private readonly List<(DateTime, HrtUiMessage)> _messages = new();
        internal LootmasterUI(LootMasterModule lootMaster) : base(false, "LootMaster")
        {
            _lootMaster = lootMaster;
            _CurrenGroupIndex = 0;
            Size = new Vector2(1600, 670);
            SizingCondition = ImGuiCond.FirstUseEver;
            Title = Localize("LootMasterWindowTitle", "Loot Master");

        }
        protected override void BeforeDispose()
        {
            _lootMaster.Configuration.Data.LastGroupIndex = _CurrenGroupIndex;
            _lootMaster.Configuration.Save();
        }
        protected override void OnShow()
        {
            _CurrenGroupIndex = _lootMaster.Configuration.Data.LastGroupIndex;
        }
        private void HandleAsync()
        {
            _messages.RemoveAll(m => (DateTime.Now - m.Item1).TotalSeconds > 10);
            foreach (HrtUiMessage m in _messages.ConvertAll(i => i.Item2))
            {
                switch (m.MessageType)
                {
                    case HrtUiMessageType.Error or HrtUiMessageType.Failure:
                        ImGui.TextColored(Vec4(ColorName.RedCrayola), m.Message);
                        break;
                    case HrtUiMessageType.Success:
                        ImGui.TextColored(Vec4(ColorName.Green), m.Message);
                        break;
                    case HrtUiMessageType.Warning:
                        ImGui.TextColored(Vec4(ColorName.Yellow), m.Message);
                        break;
                    case HrtUiMessageType.Important:
                        ImGui.TextColored(Vec4(ColorName.MiddleRed), m.Message);
                        break;
                    default:
                        ImGui.Text(m.Message);
                        break;
                }
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
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditPlayer{p.NickName}", $"{Localize("Edit player", "Edit player")} {p.NickName}"))
            {
                AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, p, _lootMaster.Configuration.Data.GetDefaultBiS), true);
            }
            foreach (var playableClass in p.MainChar.Classes)
            {
                bool isMainJob = p.MainChar.MainJob == playableClass.Job;

                if (isMainJob)
                    ImGui.PushStyleColor(ImGuiCol.Button, Vec4(ColorName.Redwood.ToHsv().Value(0.6f)));
                if (ImGuiHelper.Button(playableClass.Job.ToString(), null, true, new Vector2(38f * ScaleFactor, 0f)))
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
                    Services.TaskManager.RegisterTask(_lootMaster, () => Services.ConnectorPool.EtroConnector.GetGearSet(playableClass.BIS)
                        , $"BIS update for Character {p.MainChar.Name} ({playableClass.Job}) succeeded"
                        , $"BIS update for Character {p.MainChar.Name} ({playableClass.Job}) failed");
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
                    Localize("StatsUnfinished", "Stats are under development and only work correctly for level 70/80/90 jobs"));

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
        protected override void Draw()
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
                        AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, CurrentGroup.Tank1, _lootMaster.Configuration.Data.GetDefaultBiS), true);
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
                    foreach (var position in CurrentGroup.Positions)
                    {
                        ImGui.PushID(position.ToString());
                        DrawPlayer(CurrentGroup[position], position);
                        ImGui.PopID();
                    }

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
                    _lootMaster.AddGroup(new("AutoCreated"), true);
                    ImGui.CloseCurrentPopup();
                }
                if (ImGuiHelper.Button(Localize("From scratch", "From scratch"), Localize("Add empty group", "Add empty group")))
                {
                    AddChild(new EditGroupWindow(new RaidGroup(), group => _lootMaster.AddGroup(group, false)), true);
                }
                ImGui.EndPopup();
            }
            ImGui.EndTabBar();
        }

        private void DrawPlayer(Player player, PositionInRaidGroup pos)
        {
            bool playerExists = player.Filled && player.MainChar.Filled;
            bool hasClasses = playerExists && player.MainChar.Classes.Count > 0;
            if (playerExists)
            {

                ImGui.TableNextColumn();
                ImGui.Text($"{player.MainChar.MainJob.GetRole()}:   {player.NickName}");
                ImGui.Text($"{player.MainChar.Name} @ {player.MainChar.HomeWorld?.Name ?? "n.A."}");
                var c = player.MainChar;
                if (hasClasses)
                {
                    if (player.MainChar.Classes.Count > 1)
                    {
                        int playerClass = player.MainChar.Classes.FindIndex(x => x.Job == player.MainChar.MainJob);
                        if (ImGui.Combo($"##Class", ref playerClass, player.MainChar.Classes.ConvertAll(x => x.Job.ToString()).ToArray(),
                            player.MainChar.Classes.Count))
                            player.MainChar.MainJob = player.MainChar.Classes[playerClass].Job;
                    }
                    else
                    {
                        ImGui.Text(player.MainChar.MainJob.ToString());
                    }
                    ImGui.SameLine();
                    string levelStr = $"{Localize("LvLShort", "Lvl")}: {player.MainChar.MainClass?.Level ?? 1}";
                    float posX = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(levelStr).X
                        - ImGui.GetScrollX() - ImGui.GetStyle().ItemSpacing.X;
                    if (posX > ImGui.GetCursorPosX())
                        ImGui.SetCursorPosX(posX);
                    ImGui.Text(levelStr);
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
                    ImGuiHelper.GearUpdateButton(player);
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.ArrowsAltV, $"Rearrange", "Swap Position", true, ImGui.GetItemRectSize()))
                    {
                        AddChild(new SwapPositionWindow(pos, CurrentGroup));
                    }
                    ImGui.SameLine();


                    if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "Edit",
                        $"{Localize("Edit", "Edit")} {player.NickName}"))
                    {
                        AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, player, _lootMaster.Configuration.Data.GetDefaultBiS), true);
                    }
                    if (ImGuiHelper.Button(FontAwesomeIcon.Redo, player.BIS.EtroID,
                        string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), player.BIS.Name)))
                        Services.TaskManager.RegisterTask(_lootMaster, () => Services.ConnectorPool.EtroConnector.GetGearSet(player.BIS)
                        , $"{Localize("BisUpdateResult", "BIS update for character")} {player.MainChar.Name} ({player.MainChar.MainJob}) {Localize("successful", "successful")}"
                        , $"{Localize("BisUpdateResult", "BIS update for character")} {player.MainChar.Name} ({player.MainChar.MainJob}) {Localize("failed", "failed")}");
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, "Details",
                        $"{Localize("PlayerDetails", "Show player details for")} {player.NickName}"))
                    {
                        AddChild(new PlayerdetailWindow(this, player));
                    }
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, "Delete",
                        $"{Localize("Delete", "Delete")} {player.NickName}"))
                    {
                        AddChild(new ConfimationDialog(
                            () => player.Reset(),
                            $"{Localize("DeletePlayerConfirmation", "Do you really want to delete following player?")} : {player.NickName}"));
                    }

                }
            }
            else
            {
                ImGui.TableNextColumn();
                ImGui.Text(Localize("No Player", "No Player"));
                for (int i = 0; i < 12; i++)
                    ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddNew", Localize("Add", "Add")))
                    AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, player, _lootMaster.Configuration.Data.GetDefaultBiS), true);
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Search, "AddFromDB", Localize("Add from DB", "Add from DB")))
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
                        Helper.ILevelColor(item, _lootMaster.Configuration.Data.SelectedRaidTier.ArmorItemLevel),
                        $"{item.ItemLevel} {item.Source} {item.Slots.FirstOrDefault(GearSetSlot.None).FriendlyName()}");
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
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(CuratedData.RaidTiers[selectedTier].Name).X + 32f * ScaleFactor);
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
                    LootSessionUI lui = new(lootSource, CurrentGroup, _lootMaster.Configuration.Data.LootRuling, _lootMaster.Configuration.Data.RolePriority);
                    lui.Show();
                }
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }

        internal void HandleMessage(HrtUiMessage message)
        {
            _messages.Add((DateTime.Now, message));
        }

        private class PlayerdetailWindow : Window
        {
            private readonly Action<Player> DrawPlayer;
            private readonly Player Player;
            public PlayerdetailWindow(LootmasterUI lmui, Player p) : base()
            {
                DrawPlayer = lmui.DrawDetailedPlayer;
                Player = p;
                Show();
                Title = $"{Localize("PlayerDetailsTitle", "Player Details")} {Player.NickName}";
                (Size, SizingCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);
            }
            protected override void Draw() => DrawPlayer(Player);
        }
    }
    internal class GetCharacterFromDBWindow : Window
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
            Title = Localize("GetCharacterTitle", "Get character from DB");
            Size = new Vector2(350, 420);
            WindowFlags = ImGuiWindowFlags.NoScrollbar;
        }
        protected override void Draw()
        {
            ImGui.InputText(Localize("Player Name", "Player Name"), ref NickName, 50);
            if (ImGui.ListBox("World", ref worldSelectIndex, WorldNames, WorldNames.Length))
            {
                var list = Services.HrtDataManager.GetCharacterNames(Worlds[worldSelectIndex]);
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
    internal class SwapPositionWindow : Window
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
            List<PositionInRaidGroup> positions = _group.Positions.ToList();
            positions.Remove(_oldPos);
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
