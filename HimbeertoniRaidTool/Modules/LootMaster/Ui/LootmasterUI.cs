using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using ServiceManager = HimbeertoniRaidTool.Common.Services.ServiceManager;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class LootmasterUi : HrtWindow
{
    private readonly Vector2 _buttonSize;
    private readonly Vector2 _buttonSizeVertical;
    private readonly LootMasterModule _lootMaster;
    private readonly Queue<HrtUiMessage> _messageQueue = new();
    private (HrtUiMessage message, DateTime time)? _currentMessage;

    internal LootmasterUi(LootMasterModule lootMaster) : base("LootMaster")
    {
        _lootMaster = lootMaster;
        CurrentGroupIndex = 0;
        Size = new Vector2(1720, 750);
        _buttonSize = new Vector2(30f, 25f);
        _buttonSizeVertical = new Vector2(_buttonSize.Y, _buttonSize.X);
        SizeCondition = ImGuiCond.FirstUseEver;
        Title = LootmasterLoc.Ui_Title;
        Services.ServiceManager.PluginInterface.UiBuilder.OpenMainUi += Show;
    }
    private LootMasterConfiguration.ConfigData CurConfig => _lootMaster.ConfigImpl.Data;
    internal int CurrentGroupIndex { get; private set; }
    private RaidGroup CurrentGroup => CurConfig.RaidGroups[CurrentGroupIndex];
    private static GameExpansion CurrentExpansion => ServiceManager.GameInfo.CurrentExpansion;
    protected Vector2 ButtonSize => _buttonSize * ScaleFactor;
    protected Vector2 ButtonSizeVertical => _buttonSizeVertical * ScaleFactor;

    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool AddChild(HrtWindow child)
    {
        if (_lootMaster.WindowSystem.Windows.Any(child.Equals))
        {
            child.Hide();
            return false;
        }

        _lootMaster.WindowSystem.AddWindow(child);
        child.Show();
        return true;
    }

    public override void OnOpen() => CurrentGroupIndex = CurConfig.LastGroupIndex;

    public override void Update()
    {
        base.Update();
        Queue<HrtWindow> toRemove = new();
        foreach (HrtWindow? w in _lootMaster.WindowSystem.Windows.Where(x => !x.IsOpen))
        {
            toRemove.Enqueue(w);
        }
        foreach (HrtWindow w in toRemove.Where(w => !w.Equals(this)))
        {
            Services.ServiceManager.PluginLog.Debug($"Cleaning Up Window: {w.WindowName}");
            _lootMaster.WindowSystem.RemoveWindow(w);
        }
    }

    private static TimeSpan MessageTimeByMessageType(HrtUiMessageType type) => type switch
    {
        HrtUiMessageType.Info                                => TimeSpan.FromSeconds(3),
        HrtUiMessageType.Success or HrtUiMessageType.Warning => TimeSpan.FromSeconds(5),
        _                                                    => TimeSpan.FromSeconds(10),
    };

    private void DrawUiMessages()
    {
        if (_currentMessage.HasValue &&
            _currentMessage.Value.time + MessageTimeByMessageType(_currentMessage.Value.message.MessageType) <
            DateTime.Now)
            _currentMessage = null;
        while (!_currentMessage.HasValue && _messageQueue.TryDequeue(out HrtUiMessage? message))
        {
            _currentMessage = message.MessageType != HrtUiMessageType.Discard ? (message, DateTime.Now) : null;
        }
        if (!_currentMessage.HasValue) return;
        Vector4 color = _currentMessage.Value.message.MessageType switch
        {
            HrtUiMessageType.Error or HrtUiMessageType.Failure => Colors.TextRed,
            HrtUiMessageType.Success                           => Colors.TextGreen,
            HrtUiMessageType.Warning                           => Colors.TextYellow,
            HrtUiMessageType.Important                         => Colors.TextSoftRed,
            _                                                  => Colors.TextWhite,
        };
        ImGui.TextColored(color, _currentMessage.Value.message.Message);
    }

    private void DrawDetailedPlayer(Player p)
    {
        PlayableClass? curClass = p.MainChar.MainClass;
        ImGui.BeginChild("##SoloView");
        ImGui.Columns(3);
        /*
         * Job Selection
         */
        ImGui.Text($"{p.NickName} : {p.MainChar.Name} @ {p.MainChar.HomeWorld?.Name ?? "n.A"}");
        ImGui.SameLine();

        ImGuiHelper.GearUpdateButtons(p, _lootMaster, true);
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"##EditPlayer{p.NickName}",
                               string.Format(GeneralLoc.LootmasterUi_button_editPlayer_tooltip, p.NickName)))
            AddChild(new EditPlayerWindow(p));
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"##EditCharacter{p.NickName}",
                               string.Format(GeneralLoc.LootmasterUi_button_editCharacter_tooltip, p.MainChar.Name)))
            AddChild(new EditCharacterWindow(p.MainChar));
        ImGui.BeginChild("JobList");
        foreach (PlayableClass playableClass in p.MainChar.Classes.Where(c => !c.HideInUi))
        {
            ImGui.PushID($"{playableClass.Job}");
            ImGui.Separator();
            ImGui.Spacing();
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, "##delete",
                                   LootmasterLoc.lootmaster_detail_deleteJob_tooltip,
                                   ImGui.IsKeyDown(ImGuiKey.ModShift)))
                p.MainChar.RemoveClass(playableClass.Job);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "##jobUp",
                                   LootmasterLoc.lootmaster_detail_jobUp_tooltip,
                                   p.MainChar.CanMoveUp(playableClass)))
                p.MainChar.MoveClassUp(playableClass);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "##jobDown",
                                   LootmasterLoc.lootmaster_detail_jobDown_tooltip,
                                   p.MainChar.CanMoveDown(playableClass)))
                p.MainChar.MoveClassDown(playableClass);
            bool isMainJob = p.MainChar.MainJob == playableClass.Job;

            if (isMainJob)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            ImGui.SameLine();
            if (ImGuiHelper.Button($"{playableClass.Job} ({playableClass.Level:D2})", null, true,
                                   new Vector2(62f * ScaleFactor, 0f)))
                p.MainChar.MainJob = playableClass.Job;
            if (isMainJob)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            float comboWidth = 85 * ScaleFactor;
            /*
             * Current Gear
             */
            GearSet? newCur = null;
            ImGui.Text(GeneralLoc.Gear);
            ImGui.SameLine();
            LmUiHelpers.DrawGearSetCombo("##curGear", playableClass.CurGear, playableClass.GearSets, s => newCur = s,
                                         AddChild, playableClass.Job, comboWidth);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "##editGear",
                                   string.Format(LootmasterLoc.PlayerDetail_buttton_editGear_tooltip,
                                                 playableClass.Job)))
                AddChild(
                    new EditGearSetWindow(playableClass.CurGear, playableClass.Job, g => playableClass.CurGear = g));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, "##quickCompare",
                                   LootmasterLoc.PlayerDetail_button_quickCompare))
                AddChild(new QuickCompareWindow(CurConfig, playableClass, p.MainChar.Tribe));
            if (newCur is not null) { playableClass.CurGear = newCur; }
            /*
             * BiS
             */
            GearSet? newBis = null;
            ImGui.SameLine();
            ImGui.Text(GeneralLoc.BiS);
            ImGui.SameLine();
            LmUiHelpers.DrawGearSetCombo("##curBis", playableClass.CurBis, playableClass.BisSets, s => newBis = s,
                                         AddChild, playableClass.Job, comboWidth);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "##editBIS",
                                   $"{GeneralLoc.Edit} {playableClass.CurBis.Name}"))
                AddChild(new EditGearSetWindow(playableClass.CurBis, playableClass.Job, g => playableClass.CurBis = g));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Download, playableClass.CurBis.EtroId,
                                   string.Format(LootmasterLoc.UpdateBis, playableClass.CurBis.Name),
                                   playableClass.CurBis is { ManagedBy: GearSetManager.Etro, EtroId.Length: > 0 }))
                Services.ServiceManager.ConnectorPool.EtroConnector.GetGearSetAsync(playableClass.CurBis,
                    HandleMessage,
                    string.Format(LootmasterLoc.Lootmaster_button_etroUpdate_tooltip,
                                  playableClass.CurBis.Name, playableClass.CurBis.EtroId));
            if (newBis is not null) playableClass.CurBis = newBis;
            ImGui.Spacing();
            ImGui.PopID();
        }

        ImGui.EndChild();
        /*
         * Stat Table
         */
        ImGui.NextColumn();
        if (curClass is not null)
            LmUiHelpers.DrawStatTable(curClass, p.MainChar.Tribe, curClass.CurGear, curClass.CurBis,
                                      LootmasterLoc.CurrentGear, " ", GeneralLoc.BiS,
                                      LmUiHelpers.StatTableCompareMode.DoCompare
                                    | LmUiHelpers.StatTableCompareMode.DiffRightToLeft);

        /*
         * Show Gear
         */
        ImGui.NextColumn();
        ImGui.BeginTable("##soloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
        ImGui.TableSetupColumn(GeneralLoc.Gear);
        ImGui.TableSetupColumn("");
        ImGui.TableHeadersRow();
        if (curClass is not null)
        {
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.MainHand], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.OffHand], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Head], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Ear], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Body], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Neck], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Hands], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Wrist], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Legs], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Ring1], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Feet], SlotDrawFlags.ExtendedView);
            ImGui.TableNextColumn();
            DrawSlot(curClass[GearSetSlot.Ring2], SlotDrawFlags.ExtendedView);
        }
        else
        {
            for (int i = 0; i < GearSet.NUM_SLOTS; i++)
            {
                ImGui.TableNextColumn();
            }
        }

        ImGui.EndTable();
        ImGui.EndChild();
    }

    public override void Draw()
    {
        if (CurrentGroupIndex > _lootMaster.RaidGroups.Count - 1 || CurrentGroupIndex < 0)
            CurrentGroupIndex = 0;
        DrawUiMessages();
        if (ImGuiHelper.Button(FontAwesomeIcon.Cog, "##showConfig",
                               LootmasterLoc.lootmaster_button_showconfig_tooltip))
            Services.ServiceManager.Config.Show();
        ImGui.SameLine();
        DrawLootHandlerButtons();
        DrawRaidGroupSwitchBar();
        if (CurrentGroup.Type == GroupType.Solo)
        {
            if (CurrentGroup[0].MainChar.Filled)
            {
                DrawDetailedPlayer(CurrentGroup[0]);
            }
            else
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "##Solo", GeneralLoc.Add_Player))
                    AddChild(new EditPlayerWindow(CurrentGroup[0]));
            }
        }
        else
        {
            if (ImGui.BeginTable("##RaidGroup", 15,
                                 ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn(LootmasterLoc.Ui_MainTable_Col_Sort);
                ImGui.TableSetupColumn(LootmasterLoc.Ui_MainTable_Col_Player);
                ImGui.TableSetupColumn(GeneralLoc.Gear);
                foreach (GearSetSlot slot in GearSet.Slots)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    ImGui.TableSetupColumn(slot.FriendlyName(true));
                }

                ImGui.TableSetupColumn(string.Empty);
                ImGui.TableHeadersRow();
                for (int position = 0; position < CurrentGroup.Count; position++)
                {
                    ImGui.PushID(position);
                    DrawPlayerRow(CurrentGroup, position);
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }
    }

    private void DrawRaidGroupSwitchBar()
    {
        if (!ImGui.BeginTabBar("##RaidGroupSwitchBar"))
            return;
        for (int tabBarIdx = 0; tabBarIdx < _lootMaster.RaidGroups.Count; tabBarIdx++)
        {
            ImGui.PushID(tabBarIdx);
            //0 is reserved for Solo on current Character (only partially editable)
            bool isPredefinedSolo = tabBarIdx == 0;
            bool isActiveGroup = tabBarIdx == CurrentGroupIndex;

            RaidGroup group = _lootMaster.RaidGroups[tabBarIdx];
            if (isActiveGroup) ImGui.PushStyleColor(ImGuiCol.Tab, Colors.RedWood);

            if (ImGui.TabItemButton(group.Name))
                CurrentGroupIndex = tabBarIdx;
            ImGuiHelper.AddTooltip(LootmasterLoc.LootmasterUi_button_group_tooltip);

            if (ImGui.BeginPopupContextItem(group.Name))
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "##editGroup",
                                       string.Format(LootmasterLoc.LootmasterUi_button_editGroup, group.Name)))
                {
                    group.TypeLocked |= isPredefinedSolo;
                    AddChild(new EditGroupWindow(group));
                    ImGui.CloseCurrentPopup();
                }

                if (!isPredefinedSolo)
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "##deleteGroup",
                                           string.Format(LootmasterLoc.LootmasterUi_button_deleteGRoup_tooltip,
                                                         group.Name), ImGui.IsKeyDown(ImGuiKey.ModShift)))
                    {
                        _lootMaster.RaidGroups.Remove(group);
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndPopup();
            }

            if (isActiveGroup) ImGui.PopStyleColor();
            ImGui.PopID();
        }

        const string newGroupContextMenuId = "##NewGroupContextMenu";
        if (ImGui.TabItemButton("+")) ImGui.OpenPopup(newGroupContextMenuId);
        if (ImGui.BeginPopupContextItem(newGroupContextMenuId))
        {
            if (ImGuiHelper.Button(LootmasterLoc.Lootmaster_button_newGroupAutomatic, null))
            {
                _lootMaster.AddGroup(new RaidGroup(LootmasterLoc.AutoCreatedGroupName), true);
                ImGui.CloseCurrentPopup();
            }

            if (ImGuiHelper.Button(LootmasterLoc.Lootmaster_button_newGRoupManual,
                                   LootmasterLoc.Lootmaster_button_newGRoupManual_tooltip))
                AddChild(new EditGroupWindow(new RaidGroup(), group => _lootMaster.AddGroup(group, false)));
            ImGui.EndPopup();
        }

        ImGui.EndTabBar();
    }

    private void DrawPlayerRow(RaidGroup group, int pos)
    {
        Player player = group[pos];
        //Sort Row
        ImGui.TableNextColumn();
        float fullLineHeight = ImGui.GetTextLineHeightWithSpacing();
        float lineSpacing = fullLineHeight - ImGui.GetTextLineHeight();
        float dualTopRowY = ImGui.GetCursorPosY() + lineSpacing * 2.1f;
        float dualBottomRowY = ImGui.GetCursorPosY() + fullLineHeight * 2.1f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineSpacing * 1.5f);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "##sortUp", GeneralLoc.SortableList_moveUp, pos > 0,
                               ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos - 1, pos);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineSpacing * 0.5f);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "##sortDown", GeneralLoc.SortableList_moveDown,
                               pos < CurrentGroup.Count - 1,
                               ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos, pos + 1);
        if (player.Filled)
        {
            //Player Column
            ImGui.TableNextColumn();
            ImGui.Text(
                $"{player.CurJob?.Role.FriendlyName() ?? GeneralLoc.Player}:   {player.NickName}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "##editPlayer",
                                   string.Format(GeneralLoc.LootmasterUi_button_editPlayer_tooltip,
                                                 player.NickName)))
                AddChild(new EditPlayerWindow(player));
            ImGui.Text(
                $"{player.MainChar.Name} @ {player.MainChar.HomeWorld?.Name ?? GeneralLoc.NotAvail_Abbrev}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "##editCharacter",
                                   string.Format(GeneralLoc.LootmasterUi_button_editCharacter_tooltip,
                                                 player.MainChar.Name)))
                AddChild(new EditCharacterWindow(player.MainChar));
            PlayableClass? curJob = player.CurJob;
            if (player.MainChar.Classes.Any())
            {
                ImGui.SetNextItemWidth(110 * ScaleFactor);
                if (ImGui.BeginCombo("##Class", curJob?.ToString()))
                {
                    foreach (PlayableClass job in player.MainChar.Where(c => !c.HideInUi))
                    {
                        if (ImGui.Selectable(job.ToString()))
                            player.MainChar.MainJob = job.Job;
                    }
                    ImGui.EndCombo();
                }
            }
            else
            {
                ImGui.Text(LootmasterLoc.Lootmaster_text_noJobs);
            }
            if (curJob is null)
            {
                for (int i = 0; i < GearSet.NUM_SLOTS; i++)
                {
                    ImGui.TableNextColumn();
                }
            }
            else
            {

                GearSet? newGear = null;
                GearSet? newBis = null;
                /*
                 * Gear Sets
                 */
                ImGui.PushID("##gearButtons");
                GearSet gear = curJob.CurGear;
                GearSet bis = curJob.CurBis;
                ImGui.TableNextColumn();
                float comboWidth = 85f * ScaleFactor;
                /*
                 * Current Gear
                 */
                ImGui.SetCursorPosY(dualTopRowY);
                LmUiHelpers.DrawGearSetCombo("##curGear", gear, curJob.GearSets, s => curJob.CurGear = s, AddChild,
                                             curJob.Job,
                                             comboWidth);
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualTopRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "##editCurGear",
                                       string.Format(LootmasterLoc.Lootmaster_button_editGear_tooltip,
                                                     gear.Name), true, ButtonSize))
                    AddChild(new EditGearSetWindow(gear, curJob.Job, g => curJob.CurGear = g));
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualTopRowY);
                ImGuiHelper.GearUpdateButtons(player, _lootMaster, false, ButtonSize);
                /*
                 * Current BiS
                 */
                ImGui.SetCursorPosY(dualBottomRowY);
                LmUiHelpers.DrawGearSetCombo("##curBis", bis, curJob.BisSets, s => curJob.CurBis = s, AddChild,
                                             curJob.Job,
                                             comboWidth);
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualBottomRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "##editBiSGear",
                                       string.Format(LootmasterLoc.Lootmaster_button_editGear_tooltip,
                                                     gear.Name), true, ButtonSize))
                    AddChild(new EditGearSetWindow(bis, curJob.Job, g => curJob.CurBis = g));
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualBottomRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Download, bis.EtroId,
                                       string.Format(LootmasterLoc.Lootmaster_button_etroUpdate_tooltip,
                                                     bis.Name),
                                       bis is { ManagedBy: GearSetManager.Etro, EtroId.Length: > 0 }, ButtonSize))
                    Services.ServiceManager.ConnectorPool.EtroConnector.GetGearSetAsync(bis, HandleMessage,
                        string.Format(LootmasterLoc.Lootmaster_button_etroUpdate_tooltip, bis.Name,
                                      bis.EtroId));
                ImGui.PopID();
                foreach ((GearSetSlot slot, (GearItem, GearItem) itemTuple) in curJob.ItemTuples)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    ImGui.TableNextColumn();
                    DrawSlot(itemTuple);
                }
                if (newGear is not null) player.MainChar.MainClass!.CurGear = newGear;
                if (newBis is not null) player.MainChar.MainClass!.CurBis = newBis;
            }

            /*
             * Start of functional button section
             */
            {
                ImGui.TableNextColumn();
                ImGui.SetCursorPosY(dualTopRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, "##quickCompare",
                                       LootmasterLoc.lootmaster_button_quickCompare_tooltip,
                                       curJob != null,
                                       ButtonSize))
                    AddChild(new QuickCompareWindow(CurConfig, curJob!, player.MainChar.Tribe));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Wallet, "##inventory",
                                       LootmasterLoc.lootmaster_button_inventory_tooltip, true,
                                       ButtonSize))
                    AddChild(new InventoryWindow(player.MainChar));
                ImGui.SetCursorPosY(dualBottomRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, "##details",
                                       $"{GeneralLoc.PlayerDetails} {player.NickName}",
                                       true, ButtonSize))
                    AddChild(new PlayerDetailWindow(this, player));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "##remove",
                                       string.Format(LootmasterLoc.Ui_btn_removePlayer_tt, player.NickName),
                                       ImGui.IsKeyDown(ImGuiKey.ModShift), ButtonSize))
                    group[pos] = new Player();
            }
        }
        else
        {
            ImGui.TableNextColumn();
            ImGui.NewLine();
            ImGui.Text(LootmasterLoc.Ui_text_emptyPlayer);
            ImGui.Text(" ");
            for (int i = 0; i < GearSet.NUM_SLOTS; i++)
            {
                ImGui.TableNextColumn();
            }
            ImGui.TableNextColumn();
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "##addNew", LootmasterLoc.Ui_button_addEmptyPlayer_tt,
                                   true, ButtonSize))
                AddChild(new EditPlayerWindow(player));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "##addPlayerFromDB",
                                   LootmasterLoc.Ui_button_addExistinPlayer_tt, true, ButtonSize))
                AddChild(Services.ServiceManager.HrtDataManager.PlayerDb.OpenSearchWindow(
                             selected => group[pos] = selected));
            if (ImGuiHelper.Button(FontAwesomeIcon.LocationCrosshairs, "AddTarget",
                                   string.Format(LootmasterLoc.Ui_btn_addPlayerFromTarget_tt,
                                                 Services.ServiceManager.TargetManager.Target?.Name ?? GeneralLoc.None),
                                   Services.ServiceManager.TargetManager.Target is not null, ButtonSize))
            {
                _lootMaster.FillPlayerFromTarget(player);
                if (Services.ServiceManager.TargetManager.Target is PlayerCharacter target)
                    CsHelpers.SafeguardedOpenExamine(target);
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "##addCharFromDB",
                                   LootmasterLoc.Ui_button_addCharFromDb_tt, true, ButtonSize))
                AddChild(Services.ServiceManager.HrtDataManager.CharDb.OpenSearchWindow(selected =>
                {
                    if (!Services.ServiceManager.HrtDataManager.PlayerDb.TryAdd(player))
                        return;
                    player.NickName = selected.Name.Split(' ')[0];
                    player.MainChar = selected;
                }));
        }
    }

    private void DrawSlot((GearItem, GearItem) itemTuple, SlotDrawFlags style = SlotDrawFlags.Default) =>
        LmUiHelpers.DrawSlot(CurConfig, itemTuple, style);

    private void DrawLootHandlerButtons()
    {
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(CurConfig.SelectedRaidTier.Name).X + 32f * ScaleFactor);
        if (ImGui.BeginCombo("##raidTier", CurConfig.SelectedRaidTier.Name))
        {
            for (int i = 0; i < CurrentExpansion.SavageRaidTiers.Length; i++)
            {
                RaidTier tier = CurrentExpansion.SavageRaidTiers[i];
                if (ImGui.Selectable(tier.Name))
                {
                    if (i == CurrentExpansion.SavageRaidTiers.Length - 1)
                        CurConfig.RaidTierOverride = null;
                    else
                        CurConfig.RaidTierOverride = i;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Text(LootmasterLoc.Ui_text_openLootSessioon + ":");
        ImGui.SameLine();

        foreach (InstanceWithLoot lootSource in CurConfig.SelectedRaidTier.Bosses)
        {
            if (ImGuiHelper.Button(lootSource.Name, null))
                AddChild(new LootSessionUi(lootSource, CurrentGroup, CurConfig.RaidGroups, CurConfig.LootRuling,
                                           CurConfig.RolePriority));
            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    internal void HandleMessage(HrtUiMessage message) => _messageQueue.Enqueue(message);

    private class PlayerDetailWindow : HrtWindow
    {
        private readonly Action<Player> _drawPlayer;
        private readonly Player _player;

        public PlayerDetailWindow(LootmasterUi lmui, Player p)
        {
            _drawPlayer = lmui.DrawDetailedPlayer;
            _player = p;
            Show();
            Title = string.Format(GeneralLoc.PlayerDetailsTitle, _player.NickName);
            (Size, SizeCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);
        }

        public override void Draw() => _drawPlayer(_player);
    }
}

internal class InventoryWindow : HrtWindowWithModalChild
{
    private readonly Inventory _inv;

    internal InventoryWindow(Character c)
    {
        Size = new Vector2(400f, 550f);
        SizeCondition = ImGuiCond.Appearing;
        Title = string.Format(LootmasterLoc.InventoryWindow_Title, c.Name);
        _inv = c.MainInventory;
        if (ServiceManager.GameInfo.CurrentExpansion.CurrentSavage is null)
            return;
        foreach (HrtItem item in from boss in ServiceManager.GameInfo.CurrentExpansion.CurrentSavage
                                                            .Bosses
                                 from item in boss.GuaranteedItems
                                 where !_inv.Contains(item.Id)
                                 select item)
        {
            _inv[_inv.FirstFreeSlot()] = new InventoryEntry(item)
            {
                Quantity = 0,
            };
        }
    }

    public override void Draw()
    {
        if (ImGuiHelper.CloseButton())
            Hide();
        if (ServiceManager.GameInfo.CurrentExpansion.CurrentSavage is not null)
            foreach (InstanceWithLoot boss in ServiceManager.GameInfo.CurrentExpansion.CurrentSavage
                                                            .Bosses)
            {
                foreach (HrtItem item in boss.GuaranteedItems)
                {
                    IDalamudTextureWrap icon = Services.ServiceManager.IconCache[item.Icon];
                    ImGui.Image(icon.ImGuiHandle, icon.Size);
                    ImGui.SameLine();
                    ImGui.Text(item.Name);
                    ImGui.SameLine();
                    InventoryEntry entry = _inv[_inv.IndexOf(item.Id)];
                    ImGui.SetNextItemWidth(150f * ScaleFactor);
                    ImGui.InputInt($"##{item.Name}", ref entry.Quantity);
                    _inv[_inv.IndexOf(item.Id)] = entry;
                }
            }

        ImGui.Separator();
        ImGui.Text(LootmasterLoc.InventoryUi_heading_additionalGear);
        Vector2 iconSize = new Vector2(37, 37) * ScaleFactor;
        foreach ((int idx, InventoryEntry entry) in _inv.Where(e => e.Value.IsGear))
        {
            ImGui.PushID(idx);
            if (entry.Item is not GearItem item)
                continue;
            IDalamudTextureWrap icon = Services.ServiceManager.IconCache[item.Icon];
            if (ImGuiHelper.Button(FontAwesomeIcon.Trash, "##delete", null, true, iconSize))
                _inv.Remove(idx);
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Image(icon.ImGuiHandle, iconSize);
            ImGui.SameLine();
            ImGui.Text(item.Name);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }

            ImGui.PopID();
        }

        ImGui.BeginDisabled(ChildIsOpen);
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "##add", null, true, iconSize))
            ModalChild = new SelectGearItemWindow(i => _inv.Add(_inv.FirstFreeSlot(), i), _ => { }, null, null, null,
                                                  ServiceManager.GameInfo.CurrentExpansion.CurrentSavage?.ArmorItemLevel
                                               ?? 0);
        ImGui.EndDisabled();
    }
}