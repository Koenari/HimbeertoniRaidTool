using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
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

    internal LootmasterUi(LootMasterModule lootMaster) : base("LootMaster", ImGuiWindowFlags.HorizontalScrollbar)
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
    //private GameExpansion ActiveExpansion => CurConfig.ActiveExpansion;
    protected Vector2 ButtonSize => _buttonSize * ScaleFactor;
    protected Vector2 ButtonSizeVertical => _buttonSizeVertical * ScaleFactor;

    private bool AddChild(HrtWindow? child)
    {
        if (child is null) return false;
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
            Services.ServiceManager.Logger.Debug($"Cleaning Up Window: {w.WindowName}");
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
        ImGui.Text($"{p.NickName} : {string.Format($"{{0:{CurConfig.CharacterNameFormat}}}", p.MainChar)}");
        ImGui.SameLine();

        ImGuiHelper.GearUpdateButtons(p, _lootMaster, true);
        ImGui.SameLine();
        if (ImGuiHelper.EditButton(p, $"##EditPlayer{p.NickName}"))
            AddChild(EditWindowFactory.Create(p));
        ImGui.SameLine();
        if (ImGuiHelper.EditButton(p.MainChar, $"##EditCharacter{p.NickName}"))
            AddChild(EditWindowFactory.Create(p.MainChar));
        ImGui.BeginChild("JobList");
        Action? deferredAction = null;
        foreach (PlayableClass playableClass in p.MainChar.Classes.Where(c => !c.HideInUi))
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
                                   new Vector2(62f * ScaleFactor, 0f)))
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
                                         AddChild, playableClass.Job, comboWidth);
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(playableClass.CurGear, "##editGear"))
                AddChild(EditWindowFactory.Create(playableClass.CurGear, g => playableClass.CurGear = g));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, "##quickCompare",
                                   LootmasterLoc.PlayerDetail_button_quickCompare))
                AddChild(new QuickCompareWindow(CurConfig, playableClass, p.MainChar.Tribe));
            /*
             * BiS
             */
            ImGui.SameLine();
            ImGui.Text(GeneralLoc.CommonTerms_BiS);
            ImGui.SameLine();
            LmUiHelpers.DrawGearSetCombo("##curBis", playableClass.CurBis, playableClass.BisSets,
                                         s => playableClass.CurBis = s,
                                         AddChild, playableClass.Job, comboWidth);
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(playableClass.CurBis, "##editBIS"))
                AddChild(EditWindowFactory.Create(playableClass.CurBis, g => playableClass.CurBis = g));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Download, playableClass.CurBis.ExternalId,
                                   string.Format(LootmasterLoc.Ui_btn_tt_UpdateFrmEtro, playableClass.CurBis.Name),
                                   playableClass.CurBis is { ManagedBy: GearSetManager.Etro, ExternalId.Length: > 0 }))
                Services.ServiceManager.ConnectorPool.EtroConnector.RequestGearSetUpdate(playableClass.CurBis,
                    HandleMessage,
                    string.Format(
                        LootmasterLoc.Ui_btn_tt_etroUpdate,
                        playableClass.CurBis.Name,
                        playableClass.CurBis.ExternalId));
            ImGui.Spacing();
            ImGui.PopID();
        }

        ImGui.EndChild();
        deferredAction?.Invoke();
        /*
         * Stat Table
         */
        ImGui.NextColumn();
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
        if (ImGui.BeginChild("##soloGearChild"))
        {
            if (ImGui.BeginTable("##soloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders))
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

                ImGui.EndTable();
            }
            ImGui.EndChild();
        }


        ImGui.EndChild();
    }

    public override void Draw()
    {
        if (CurrentGroupIndex > _lootMaster.RaidGroups.Count - 1 || CurrentGroupIndex < 0)
            CurrentGroupIndex = 0;
        DrawUiMessages();
        if (ImGuiHelper.Button(FontAwesomeIcon.Cog, "##showConfig",
                               LootmasterLoc.ui_btn_tt_showConfig))
            Services.ServiceManager.ConfigManager.Show();
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
                    AddChild(EditWindowFactory.Create(CurrentGroup[0]));
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
                foreach (GearSetSlot slot in GearSet.Slots)
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
            ImGuiHelper.AddTooltip(GeneralLoc.Ui_rightClickHint);

            if (ImGui.BeginPopupContextItem(group.Name))
            {
                if (ImGuiHelper.EditButton(group, "##editGroup"))
                {
                    group.TypeLocked |= isPredefinedSolo;
                    AddChild(EditWindowFactory.Create(group));
                    ImGui.CloseCurrentPopup();
                }

                if (!isPredefinedSolo)
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.GuardedButton(FontAwesomeIcon.TrashAlt, "##deleteGroup",
                                                  string.Format(GeneralLoc.General_btn_tt_remove, group.DataTypeName,
                                                                group.Name)))
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
            if (ImGuiHelper.Button(LootmasterLoc.Ui_btn_newGroupAutomatic, null))
            {
                _lootMaster.AddGroup(new RaidGroup(LootmasterLoc.AutoCreatedGroupName), true);
                ImGui.CloseCurrentPopup();
            }

            if (ImGuiHelper.Button(LootmasterLoc.Ui_btn_newGroupManual,
                                   string.Format(GeneralLoc.Ui_btn_tt_addEmpty, RaidGroup.DataTypeNameStatic)))
                AddChild(EditWindowFactory.Create(new RaidGroup(), group => _lootMaster.AddGroup(group, false)));
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
                AddChild(EditWindowFactory.Create(player));
            ImGui.Text(string.Format($"{{0:{CurConfig.CharacterNameFormat}}}", player.MainChar));
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(player.MainChar, "##editCharacter"))
                AddChild(EditWindowFactory.Create(player.MainChar));
            PlayableClass? curJob = player.MainChar.MainClass;
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
                if (ImGuiHelper.EditButton(gear, "##editCurGear", true, ButtonSize))
                    AddChild(EditWindowFactory.Create(gear, g => curJob.CurGear = g, null, curJob.Job));
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
                if (ImGuiHelper.EditButton(bis, "##editBiSGear", true, ButtonSize))
                    AddChild(EditWindowFactory.Create(bis, g => curJob.CurBis = g, null, curJob.Job));
                ImGui.SameLine();
                ImGui.SetCursorPosY(dualBottomRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Download, bis.ExternalId,
                                       string.Format(LootmasterLoc.Ui_btn_tt_etroUpdate,
                                                     bis.Name, bis.ExternalId),
                                       bis is { ManagedBy: GearSetManager.Etro, ExternalId.Length: > 0 }, ButtonSize))
                    Services.ServiceManager.ConnectorPool.EtroConnector.RequestGearSetUpdate(bis, HandleMessage,
                        string.Format(LootmasterLoc.Ui_btn_tt_etroUpdate, bis.Name,
                                      bis.ExternalId));
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
                                       LootmasterLoc.Ui_btn_tt_quickCompare,
                                       curJob != null,
                                       ButtonSize))
                    AddChild(new QuickCompareWindow(CurConfig, curJob!, player.MainChar.Tribe));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Wallet, "##inventory",
                                       LootmasterLoc.Ui_btn_tt_inventory, true,
                                       ButtonSize))
                    AddChild(new InventoryWindow(player.MainChar));
                ImGui.SetCursorPosY(dualBottomRowY);
                if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, "##details",
                                       $"{LootmasterLoc.Ui_btn_tt_PlayerDetails} {player.NickName}",
                                       true, ButtonSize))
                    AddChild(new PlayerDetailWindow(DrawDetailedPlayer, player));
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
                AddChild(EditWindowFactory.Create(player));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "##addPlayerFromDB",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting, group[pos].DataTypeName), true,
                                   ButtonSize))
                AddChild(Services.ServiceManager.HrtDataManager.PlayerDb.OpenSearchWindow(
                             selected => group[pos] = selected));
            if (ImGuiHelper.Button(FontAwesomeIcon.LocationCrosshairs, "AddTarget",
                                   string.Format(LootmasterLoc.Ui_btn_addPlayerFromTarget_tt,
                                                 Services.ServiceManager.TargetManager.Target?.Name
                                              ?? GeneralLoc.CommonTerms_None),
                                   Services.ServiceManager.TargetManager.Target is not null, ButtonSize))
            {
                _lootMaster.FillPlayerFromTarget(player);
                if (Services.ServiceManager.TargetManager.Target is IPlayerCharacter target)
                    CsHelpers.SafeguardedOpenExamine(target);
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "##addCharFromDB",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting,
                                                 Character.DataTypeNameStatic), true, ButtonSize))
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
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(CurConfig.ActiveExpansion.Name).X + 32f * ScaleFactor);
        if (ImGui.BeginCombo("##expansion", CurConfig.ActiveExpansion.Name))
        {
            var expansions = ServiceManager.GameInfo.Expansions;
            for (int i = 0; i < expansions.Count; i++)
            {
                GameExpansion expansion = expansions[i];
                if (expansion.SavageRaidTiers.Length == 0) continue;
                if (ImGui.Selectable(expansion.Name))
                {
                    if (expansion == ServiceManager.GameInfo.CurrentExpansion)
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
                RaidTier tier = CurConfig.ActiveExpansion.SavageRaidTiers[i];
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

        foreach (InstanceWithLoot lootSource in CurConfig.SelectedRaidTier.Bosses)
        {
            if (ImGuiHelper.Button(lootSource.Name, null, lootSource.IsAvailable))
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

        public PlayerDetailWindow(Action<Player> drawPlayer, Player p)
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

internal class InventoryWindow : HrtWindowWithModalChild
{
    private readonly Inventory _inv;

    internal InventoryWindow(Character c)
    {
        Size = new Vector2(400f, 550f);
        SizeCondition = ImGuiCond.Appearing;
        Title = string.Format(LootmasterLoc.InventoryUi_Title, c.Name);
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
        ImGui.Text(LootmasterLoc.InventoryUi_hdg_additionalGear);
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