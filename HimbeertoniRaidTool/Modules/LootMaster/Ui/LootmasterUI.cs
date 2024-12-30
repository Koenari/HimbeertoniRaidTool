using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class LootmasterUi : HrtWindow
{
    private readonly Vector2 _buttonSize;
    private readonly Vector2 _buttonSizeVertical;
    private readonly LootMasterModule _module;
    private readonly Queue<HrtUiMessage> _messageQueue = new();
    private (HrtUiMessage message, DateTime time)? _currentMessage;

    internal LootmasterUi(LootMasterModule lootMaster) : base(lootMaster.Services.UiSystem, "LootMaster",
                                                              ImGuiWindowFlags.HorizontalScrollbar)
    {
        Persistent = true;
        _module = lootMaster;
        Size = new Vector2(1720, 750);
        _buttonSize = new Vector2(30f, 25f);
        _buttonSizeVertical = new Vector2(_buttonSize.Y, _buttonSize.X);
        SizeCondition = ImGuiCond.FirstUseEver;
        Title = LootmasterLoc.Ui_Title;
        _module.Services.PluginInterface.UiBuilder.OpenMainUi += Show;
        UiSystem.AddWindow(this);
    }
    private LootMasterConfiguration.ConfigData CurConfig => _module.ConfigImpl.Data;

    private RaidGroup CurrentGroup => CurConfig.RaidGroups[CurConfig.ActiveGroupIndex];
    //private GameExpansion ActiveExpansion => CurConfig.ActiveExpansion;
    private Vector2 ButtonSize => _buttonSize * ScaleFactor;
    private Vector2 ButtonSizeVertical => _buttonSizeVertical * ScaleFactor;

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
        while (!_currentMessage.HasValue && _messageQueue.TryDequeue(out var message))
        {
            _currentMessage = message.MessageType != HrtUiMessageType.Discard ? (message, DateTime.Now) : null;
        }
        if (!_currentMessage.HasValue) return;
        var color = _currentMessage.Value.message.MessageType switch
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
        var curClass = p.MainChar.MainClass;
        using var outerChild = ImRaii.Child("##SoloView");
        ImGui.Columns(3);
        /*
         * Job Selection
         */
        ImGui.Spacing();
        ImGui.Text($"{p.NickName} : {string.Format($"{{0:{CurConfig.CharacterNameFormat}}}", p.MainChar)}");
        ImGui.SameLine();
        ImGuiHelper.GearUpdateButtons(p, _module, true);
        ImGui.SameLine();
        if (ImGuiHelper.EditButton(p, $"##EditPlayer{p.NickName}"))
            UiSystem.EditWindows.Create(p);
        ImGui.SameLine();
        if (ImGuiHelper.EditButton(p.MainChar, $"##EditCharacter{p.NickName}"))
            UiSystem.EditWindows.Create(p.MainChar);
        ImGui.BeginChild("JobList");
        Action? deferredAction = null;
        foreach (var playableClass in p.MainChar.Classes.Where(c => !c.HideInUi))
        {
            ImGui.PushID($"{playableClass.Job}");
            ImGui.Separator();
            ImGui.Spacing();
            if (ImGuiHelper.DeleteButton(playableClass, "##delete"))
                deferredAction = () => p.MainChar.RemoveClass(playableClass.Job);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "##jobUp",
                                   string.Format(GeneralLoc.SortableList_btn_tt_moveUp, playableClass.DataTypeName),
                                   p.MainChar.CanMoveUp(playableClass)))
                deferredAction = () => p.MainChar.MoveClassUp(playableClass);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "##jobDown",
                                   string.Format(GeneralLoc.SortableList_btn_tt_moveDown, playableClass.DataTypeName),
                                   p.MainChar.CanMoveDown(playableClass)))
                deferredAction = () => p.MainChar.MoveClassDown(playableClass);
            bool isMainJob = p.MainChar.MainJob == playableClass.Job;

            if (isMainJob)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            ImGui.SameLine();
            if (ImGuiHelper.Button($"{playableClass.Job} ({playableClass.Level:D2})", null, true,
                                   new Vector2(67f * ScaleFactor, 0f)))
                p.MainChar.MainJob = playableClass.Job;
            if (isMainJob)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            float comboWidth = 85 * ScaleFactor;
            /*
             * Current Gear
             */
            ImGui.Text(GeneralLoc.CommonTerms_Gear.CapitalizedSentence());
            ImGui.SameLine();
            LmUiHelpers.DrawGearSetCombo("##curGear", playableClass.CurGear, playableClass.GearSets,
                                         s => playableClass.CurGear = s,
                                         _module, playableClass.Job, comboWidth);
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(playableClass.CurGear, "##editGear"))
                UiSystem.EditWindows.Create(playableClass.CurGear, g => playableClass.CurGear = g, () => { },
                                            () => playableClass.RemoveGearSet(playableClass.CurGear),
                                            playableClass.Job);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, "##quickCompare",
                                   LootmasterLoc.PlayerDetail_button_quickCompare))
                UiSystem.AddWindow(new QuickCompareWindow(_module, playableClass, p.MainChar.Tribe));
            /*
             * BiS
             */
            ImGui.SameLine();
            ImGui.Text(GeneralLoc.CommonTerms_BiS);
            ImGui.SameLine();
            LmUiHelpers.DrawGearSetCombo("##curBis", playableClass.CurBis, playableClass.BisSets,
                                         s => playableClass.CurBis = s,
                                         _module, playableClass.Job, comboWidth);
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(playableClass.CurBis, "##editBIS"))
                UiSystem.EditWindows.Create(playableClass.CurBis, g => playableClass.CurBis = g, () => { },
                                            () => playableClass.RemoveBisSet(playableClass.CurBis),
                                            playableClass.Job);
            ImGui.SameLine();
            ImGuiHelper.ExternalGearUpdateButton(playableClass.CurBis, _module);
            ImGui.Spacing();
            ImGui.PopID();
        }

        ImGui.EndChild();
        deferredAction?.Invoke();
        /*
         * Stat Table
         */
        ImGui.NextColumn();
        ImGui.Spacing();
        if (ImGui.BeginChild("##statsChild"))
        {

            if (curClass is not null)
                LmUiHelpers.DrawStatTable(curClass, p.MainChar.Tribe, curClass.CurGear, curClass.CurBis,
                                          LootmasterLoc.CurrentGear, GeneralLoc.CommonTerms_Difference,
                                          GeneralLoc.CommonTerms_BiS,
                                          LmUiHelpers.StatTableCompareMode.DoCompare
                                        | LmUiHelpers.StatTableCompareMode.DiffRightToLeft);
            ImGui.EndChild();
        }


        /*
         * Show Gear
         */
        ImGui.NextColumn();
        ImGui.Spacing();
        // ReSharper disable once ConvertToUsingDeclaration
        using (var gearChild = ImRaii.Child("##soloGearChild"))
        {
            if (!gearChild) return;
            using (var table =
                   ImRaii.Table("##soloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders))
            {
                if (table)
                {
                    ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Gear.CapitalizedSentence());
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
                }
            }
            using (var table =
                   ImRaii.Table("##soloFood", 2, ImGuiTableFlags.Borders))
            {
                if (table)
                {
                    ImGui.TableSetupColumn(
                        $"{GeneralLoc.CommonTerms_Food} ({GeneralLoc.CommonTerms_Gear.CapitalizedSentence()})");
                    ImGui.TableSetupColumn(
                        $"{GeneralLoc.CommonTerms_Food} ({GeneralLoc.CommonTerms_BiS.CapitalizedSentence()})");
                    ImGui.TableHeadersRow();
                    ImGui.TableNextColumn();
                    LmUiHelpers.DrawFood(UiSystem, curClass?.CurGear.Food);
                    ImGui.TableNextColumn();
                    LmUiHelpers.DrawFood(UiSystem, curClass?.CurBis.Food);
                }
            }
        }
    }

    public override void Draw()
    {
        if (CurConfig.ActiveGroupIndex > _module.RaidGroups.Count - 1 || CurConfig.ActiveGroupIndex < 0)
            CurConfig.ActiveGroupIndex = 0;
        DrawUiMessages();
        if (ImGuiHelper.Button(FontAwesomeIcon.Cog, "##showConfig",
                               LootmasterLoc.ui_btn_tt_showConfig))
            _module.Services.ConfigManager.Show();
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
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "##Solo",
                                       string.Format(GeneralLoc.Ui_btn_tt_add, CurrentGroup[0].DataTypeName)))
                    UiSystem.EditWindows.Create(CurrentGroup[0]);
            }
        }
        else
        {
            ImGui.SetNextItemWidth(800 * ScaleFactor);
            if (ImGui.BeginTable("##RaidGroup", 15,
                                 ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn(LootmasterLoc.Ui_MainTable_Col_Sort, ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn(LootmasterLoc.Ui_MainTable_Col_Player, ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Gear.CapitalizedSentence(),
                                       ImGuiTableColumnFlags.WidthFixed);
                foreach (var slot in GearSet.Slots)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    ImGui.TableSetupColumn(slot.FriendlyName(true));
                }

                ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed);
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
        for (int tabBarIdx = 0; tabBarIdx < _module.RaidGroups.Count; tabBarIdx++)
        {
            ImGui.PushID(tabBarIdx);
            //0 is reserved for Solo on current Character (only partially editable)
            bool isPredefinedSolo = tabBarIdx == 0;
            bool isActiveGroup = tabBarIdx == CurConfig.ActiveGroupIndex;

            var group = _module.RaidGroups[tabBarIdx];
            if (isActiveGroup) ImGui.PushStyleColor(ImGuiCol.Tab, Colors.RedWood);

            if (ImGui.TabItemButton(group.Name))
                CurConfig.ActiveGroupIndex = tabBarIdx;
            ImGuiHelper.AddTooltip(GeneralLoc.Ui_rightClickHint);

            if (ImGui.BeginPopupContextItem(group.Name))
            {
                if (ImGuiHelper.EditButton(group, "##editGroup"))
                {
                    group.TypeLocked |= isPredefinedSolo;
                    UiSystem.EditWindows.Create(group);
                    ImGui.CloseCurrentPopup();
                }

                if (!isPredefinedSolo)
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.GuardedButton(FontAwesomeIcon.TrashAlt, "##deleteGroup",
                                                  string.Format(GeneralLoc.General_btn_tt_remove, group.DataTypeName,
                                                                group.Name)))
                    {
                        _module.RaidGroups.Remove(group);
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
            if (ImGuiHelper.Button(LootmasterLoc.Ui_btn_newGroupAutomatic, null))
            {
                _module.AddGroup(new RaidGroup(LootmasterLoc.AutoCreatedGroupName), true);
                ImGui.CloseCurrentPopup();
            }

            if (ImGuiHelper.Button(LootmasterLoc.Ui_btn_newGroupManual,
                                   string.Format(GeneralLoc.Ui_btn_tt_addEmpty, RaidGroup.DataTypeNameStatic)))
                UiSystem.EditWindows.Create(new RaidGroup(), group => _module.AddGroup(group, false));
            ImGui.EndPopup();
        }

        ImGui.EndTabBar();
    }

    private void DrawPlayerRow(RaidGroup group, int pos)
    {
        var player = group[pos];
        //Sort Row
        ImGui.TableNextColumn();
        float fullLineHeight = ImGui.GetTextLineHeightWithSpacing();
        float lineSpacing = fullLineHeight - ImGui.GetTextLineHeight();
        float dualTopRowY = ImGui.GetCursorPosY() + lineSpacing * 2.1f;
        float dualBottomRowY = ImGui.GetCursorPosY() + fullLineHeight * 2.1f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineSpacing * 1.5f);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "##sortUp",
                               string.Format(GeneralLoc.SortableList_btn_tt_moveUp, Player.DataTypeNameStatic), pos > 0,
                               ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos - 1, pos);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineSpacing * 0.5f);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "##sortDown",
                               string.Format(GeneralLoc.SortableList_btn_tt_moveDown, Player.DataTypeNameStatic),
                               pos < CurrentGroup.Count - 1,
                               ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos, pos + 1);
        if (player.Filled)
        {
            //Player Column
            ImGui.TableNextColumn();
            ImGui.Text(
                $"{player.MainChar.MainClass?.Role.FriendlyName() ?? Player.DataTypeNameStatic.CapitalizedSentence()}:   {player.NickName}");
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(player, "##editPlayer"))
                UiSystem.EditWindows.Create(player);
            ImGui.Text(string.Format($"{{0:{CurConfig.CharacterNameFormat}}}", player.MainChar));
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(player.MainChar, "##editCharacter"))
                UiSystem.EditWindows.Create(player.MainChar);
            var curJob = player.MainChar.MainClass;
            if (player.MainChar.Classes.Any())
            {
                ImGui.SetNextItemWidth(110 * ScaleFactor);
                if (ImGui.BeginCombo("##Class", curJob?.ToString()))
                {
                    foreach (var job in player.MainChar.Where(c => !c.HideInUi))
                    {
                        if (ImGui.Selectable(job.ToString()))
                            player.MainChar.MainJob = job.Job;
                    }
                    ImGui.EndCombo();
                }
            }
            else
            {
                ImGui.Text(LootmasterLoc.Ui_txt_noJobs);
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
                var gear = curJob.CurGear;
                var bis = curJob.CurBis;
                ImGui.TableNextColumn();
                float comboWidth = 85f * ScaleFactor;
                /*
                 * Current Gear
                 */
                ImGui.SetCursorPosY(dualTopRowY);
                LmUiHelpers.DrawGearSetCombo("##curGear", gear, curJob.GearSets, s => curJob.CurGear = s, _module,
                                             curJob.Job,
                                             comboWidth);
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualTopRowY);
                if (ImGuiHelper.EditButton(gear, "##editCurGear", true, ButtonSize))
                    UiSystem.EditWindows.Create(gear, g => curJob.CurGear = g, null,
                                                () => curJob.RemoveGearSet(curJob.CurGear), curJob.Job);
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualTopRowY);
                ImGuiHelper.GearUpdateButtons(player, _module, false, ButtonSize);
                /*
                 * Current BiS
                 */
                ImGui.SetCursorPosY(dualBottomRowY);
                LmUiHelpers.DrawGearSetCombo("##curBis", bis, curJob.BisSets, s => curJob.CurBis = s, _module,
                                             curJob.Job,
                                             comboWidth);
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualBottomRowY);
                if (ImGuiHelper.EditButton(bis, "##editBiSGear", true, ButtonSize))
                    UiSystem.EditWindows.Create(bis, g => curJob.CurBis = g, null,
                                                () => curJob.RemoveBisSet(curJob.CurBis), curJob.Job);
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualBottomRowY);
                ImGuiHelper.ExternalGearUpdateButton(bis, _module, ButtonSize);
                ImGui.PopID();
                foreach (var (slot, itemTuple) in curJob.ItemTuples)
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
                                       LootmasterLoc.Ui_btn_tt_quickCompare,
                                       curJob != null,
                                       ButtonSize))
                    UiSystem.AddWindow(new QuickCompareWindow(_module, curJob!, player.MainChar.Tribe));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Wallet, "##inventory",
                                       LootmasterLoc.Ui_btn_tt_inventory, true,
                                       ButtonSize))
                    UiSystem.AddWindow(new InventoryWindow(_module.Services.UiSystem, player.MainChar,
                                                           _module.Services.CharacterInfoService));
                ImGui.SetCursorPosY(dualBottomRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, "##details",
                                       $"{LootmasterLoc.Ui_btn_tt_PlayerDetails} {player.NickName}",
                                       true, ButtonSize))
                    UiSystem.AddWindow(new PlayerDetailWindow(UiSystem, DrawDetailedPlayer, player));
                ImGui.SameLine();
                if (ImGuiHelper.GuardedButton(FontAwesomeIcon.TrashAlt, "##remove",
                                              string.Format(GeneralLoc.Ui_btn_tt_removeFrom, player.NickName,
                                                            group.Name),
                                              ButtonSize))
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
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "##addNew",
                                   string.Format(GeneralLoc.Ui_btn_tt_addEmpty, player.DataTypeName),
                                   true, ButtonSize))
                UiSystem.EditWindows.Create(player);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "##addPlayerFromDB",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting, group[pos].DataTypeName), true,
                                   ButtonSize))
                UiSystem.AddWindow(_module.Services.HrtDataManager.PlayerDb.OpenSearchWindow(UiSystem,
                                       selected => group[pos] = selected));
            if (ImGuiHelper.Button(FontAwesomeIcon.LocationCrosshairs, "AddTarget",
                                   string.Format(LootmasterLoc.Ui_btn_addPlayerFromTarget_tt,
                                                 _module.Services.TargetManager.Target?.Name
                                              ?? GeneralLoc.CommonTerms_None),
                                   _module.Services.TargetManager.Target is not null, ButtonSize))
            {
                _module.FillPlayerFromTarget(player);
                if (_module.Services.TargetManager.Target is IPlayerCharacter target)
                    CsHelpers.SafeguardedOpenExamine(target, _module.Services.Logger);
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "##addCharFromDB",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting,
                                                 Character.DataTypeNameStatic), true, ButtonSize))
                UiSystem.AddWindow(_module.Services.HrtDataManager.CharDb.OpenSearchWindow(UiSystem, selected =>
                {
                    if (!_module.Services.HrtDataManager.PlayerDb.TryAdd(player))
                        return;
                    player.NickName = selected.Name.Split(' ')[0];
                    player.MainChar = selected;
                }));
        }
    }

    private void DrawSlot((GearItem, GearItem) itemTuple, SlotDrawFlags style = SlotDrawFlags.Default) =>
        LmUiHelpers.DrawSlot(_module, itemTuple, style);

    private void DrawLootHandlerButtons()
    {
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(CurConfig.ActiveExpansion.Name).X + 32f * ScaleFactor);
        if (ImGui.BeginCombo("##expansion", CurConfig.ActiveExpansion.Name))
        {
            var expansions = GameInfo.Expansions;
            for (int i = 0; i < expansions.Count; i++)
            {
                var expansion = expansions[i];
                if (expansion.SavageRaidTiers.Length == 0) continue;
                if (ImGui.Selectable(expansion.Name))
                {
                    if (expansion == GameInfo.CurrentExpansion)
                        CurConfig.ExpansionOverride = null;
                    else
                        CurConfig.ExpansionOverride = i;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(CurConfig.SelectedRaidTier.Name).X + 32f * ScaleFactor);
        if (ImGui.BeginCombo("##raidTier", CurConfig.SelectedRaidTier.Name))
        {
            for (int i = 0; i < CurConfig.ActiveExpansion.SavageRaidTiers.Length; i++)
            {
                var tier = CurConfig.ActiveExpansion.SavageRaidTiers[i];
                if (ImGui.Selectable(tier.Name))
                {
                    if (i == CurConfig.ActiveExpansion.SavageRaidTiers.Length - 1)
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

        foreach (var lootSource in CurConfig.SelectedRaidTier.Bosses)
        {
            if (ImGuiHelper.Button(lootSource.Name, null, lootSource.IsAvailable))
                UiSystem.AddWindow(new LootSessionUi(_module, lootSource, CurrentGroup));
            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    internal void HandleMessage(HrtUiMessage message) => _messageQueue.Enqueue(message);

    private class PlayerDetailWindow : HrtWindow
    {
        private readonly Action<Player> _drawPlayer;
        private readonly Player _player;

        public PlayerDetailWindow(IUiSystem uiSystem, Action<Player> drawPlayer, Player p) : base(uiSystem)
        {
            _drawPlayer = drawPlayer;
            _player = p;
            Title = string.Format(LootmasterLoc.PlayerDetailsUi_Title, _player.NickName);
            (Size, SizeCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);
            Show();
        }

        public override void Draw() => _drawPlayer(_player);
    }
}