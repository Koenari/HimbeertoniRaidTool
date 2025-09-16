using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

[Flags]
public enum SlotDrawFlags
{
    None = 0,
    SingleItem = 1,
    ItemCompare = 2,
    SimpleView = 4,
    ExtendedView = 8,
    Default = ItemCompare | SimpleView,
    DetailedSingle = SingleItem | ExtendedView,
}

internal class LootmasterUi : HrtWindow
{
    private readonly Vector2 _buttonSize;
    private readonly Vector2 _buttonSizeVertical;
    private readonly LootMasterModule _module;
    private readonly Queue<HrtUiMessage> _messageQueue = new();
    private (HrtUiMessage message, DateTime time)? _currentMessage;
    private readonly Dictionary<PlayableClass, IStatTable> _statTables = new();

    internal LootmasterUi(LootMasterModule lootMaster) : base(lootMaster.Services.UiSystem, "LootMaster",
                                                              ImGuiWindowFlags.HorizontalScrollbar)
    {
        Persistent = true;
        IsOpen = false;
        _module = lootMaster;
        Size = new Vector2(1720, 750);
        _buttonSize = new Vector2(30f, 25f);
        _buttonSizeVertical = new Vector2(_buttonSize.Y, _buttonSize.X);
        SizeCondition = ImGuiCond.FirstUseEver;
        Title = LootmasterLoc.Ui_Title;
        UiSystem.AddWindow(this);
    }
    private LootMasterConfiguration.ConfigData CurConfig => _module.Configuration.Data;

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
        ImGui.Text($"{p.NickName} :");
        ImGui.SameLine();
        UiSystem.Helpers.DrawCharacterCombo("##charSelect", p, CurConfig.CharacterNameFormat);
        ImGui.SameLine();
        ImGuiHelper.GearUpdateButtons(p, _module, true);
        ImGui.SameLine();
        if (ImGuiHelper.EditButton(p, $"##EditPlayer{p.NickName}"))
            UiSystem.EditWindows.Create(p);
        ImGui.SameLine();
        if (ImGuiHelper.EditButton(p.MainChar, $"##EditCharacter{p.NickName}"))
            UiSystem.EditWindows.Create(p.MainChar);
        using (var jobList = ImRaii.Child("##JobList"))
        {
            Action? deferredAction = null;
            if (jobList)
                foreach (var playableClass in p.MainChar.Classes.Where(c => !c.HideInUi))
                {
                    using var id = ImRaii.PushId($"{playableClass.Job}");
                    ImGui.Separator();
                    ImGui.Spacing();
                    if (ImGuiHelper.DeleteButton(playableClass, "##delete"))
                        deferredAction = () => p.MainChar.RemoveClass(playableClass.Job);
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "##jobUp",
                                           string.Format(GeneralLoc.SortableList_btn_tt_moveUp,
                                                         PlayableClass.DataTypeName),
                                           p.MainChar.CanMoveUp(playableClass)))
                        deferredAction = () => p.MainChar.MoveClassUp(playableClass);
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "##jobDown",
                                           string.Format(GeneralLoc.SortableList_btn_tt_moveDown,
                                                         PlayableClass.DataTypeName),
                                           p.MainChar.CanMoveDown(playableClass)))
                        deferredAction = () => p.MainChar.MoveClassDown(playableClass);
                    bool isMainJob = p.MainChar.MainJob == playableClass.Job;

                    using (ImRaii.PushColor(ImGuiCol.Button, Colors.RedWood, isMainJob))
                    {
                        ImGui.SameLine();
                        if (ImGuiHelper.Button($"{playableClass.Job} ({playableClass.Level:D2})", null, true,
                                               new Vector2(67f * ScaleFactor, 0f)))
                            p.MainChar.MainJob = playableClass.Job;
                    }
                    ImGui.SameLine();
                    float comboWidth = 85 * ScaleFactor;
                    /*
                     * Current Gear
                     */
                    ImGui.Text(GeneralLoc.CommonTerms_Gear.Capitalized());
                    ImGui.SameLine();
                    UiSystem.Helpers.DrawGearSetCombo("##curGear", playableClass.CurGear, playableClass.GearSets,
                                                      s => playableClass.CurGear = s, playableClass.Job, comboWidth);
                    ImGui.SameLine();
                    if (ImGuiHelper.EditButton(playableClass.CurGear, "##editGear"))
                        UiSystem.EditWindows.Create(playableClass.CurGear, g => playableClass.CurGear = g, () => { },
                                                    () => playableClass.RemoveGearSet(playableClass.CurGear),
                                                    playableClass.Job);
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, "##quickCompare",
                                           LootmasterLoc.PlayerDetail_button_quickCompare))
                        UiSystem.AddWindow(new QuickCompareWindow(
                                               UiSystem, (item, flags) => DrawSlot((item, GearItem.Empty), flags),
                                               playableClass, p.MainChar.Tribe));
                    /*
                     * BiS
                     */
                    ImGui.SameLine();
                    ImGui.Text(GeneralLoc.CommonTerms_BiS);
                    ImGui.SameLine();
                    UiSystem.Helpers.DrawGearSetCombo("##curBis", playableClass.CurBis, playableClass.BisSets,
                                                      s => playableClass.CurBis = s, playableClass.Job, comboWidth);
                    ImGui.SameLine();
                    if (ImGuiHelper.EditButton(playableClass.CurBis, "##editBIS"))
                        UiSystem.EditWindows.Create(playableClass.CurBis, g => playableClass.CurBis = g, () => { },
                                                    () => playableClass.RemoveBisSet(playableClass.CurBis),
                                                    playableClass.Job);
                    ImGui.SameLine();
                    ImGuiHelper.ExternalGearUpdateButton(playableClass.CurBis, _module);
                    ImGui.Spacing();
                }
            deferredAction?.Invoke();
        }
        /*
         * Stat Table
         */
        ImGui.NextColumn();
        ImGui.Spacing();
        using (var statsChild = ImRaii.Child("##statsChild"))
        {
            if (statsChild.Success && curClass is not null)
            {
                if (!_statTables.TryGetValue(curClass, out var statTable))
                {
                    statTable = UiSystem.Helpers.CreateStatTable(curClass, p.MainChar.Tribe, curClass.CurGear,
                                                                 curClass.CurBis, LootmasterLoc.CurrentGear,
                                                                 GeneralLoc.CommonTerms_Difference,
                                                                 GeneralLoc.CommonTerms_BiS,
                                                                 UiHelpers.StatTableCompareMode.DoCompare
                                                               | UiHelpers.StatTableCompareMode.DiffRightToLeft);
                    _statTables.Add(curClass, statTable);
                }
                statTable.Draw();
            }
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
                    ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Gear.Capitalized());
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
                        $"{GeneralLoc.CommonTerms_Food} ({GeneralLoc.CommonTerms_Gear.Capitalized()})");
                    ImGui.TableSetupColumn(
                        $"{GeneralLoc.CommonTerms_Food} ({GeneralLoc.CommonTerms_BiS.Capitalized()})");
                    ImGui.TableHeadersRow();
                    ImGui.TableNextColumn();
                    UiSystem.Helpers.DrawFood(curClass?.CurGear.Food);
                    ImGui.TableNextColumn();
                    UiSystem.Helpers.DrawFood(curClass?.CurBis.Food);
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
                if (ImGuiHelper.AddButton(RaidGroup.DataTypeName, "##solo"))
                    UiSystem.EditWindows.Create(CurrentGroup[0]);
            }
        }
        else
        {
            ImGui.SetNextItemWidth(800 * ScaleFactor);
            // ReSharper disable once ConvertToUsingDeclaration
            using (var groupTable = ImRaii.Table("##RaidGroup", 15,
                                                 ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                                                                         | ImGuiTableFlags.SizingStretchProp))
            {
                if (groupTable)
                {
                    ImGui.TableSetupColumn(LootmasterLoc.Ui_MainTable_Col_Sort, ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn(LootmasterLoc.Ui_MainTable_Col_Player, ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Gear.Capitalized(),
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
                        DrawPlayerRow(CurrentGroup, position);
                    }
                }
            }
        }
    }

    private void DrawRaidGroupSwitchBar()
    {
        using var tabBar = ImRaii.TabBar("##RaidGroupSwitchBar");
        if (!tabBar) return;
        for (int tabBarIdx = 0; tabBarIdx < _module.RaidGroups.Count; tabBarIdx++)
        {
            using var id = ImRaii.PushId(tabBarIdx);
            //0 is reserved for Solo on current Character (only partially editable)
            bool isPredefinedSolo = tabBarIdx == 0;
            bool isActiveGroup = tabBarIdx == CurConfig.ActiveGroupIndex;

            var group = _module.RaidGroups[tabBarIdx];
            using var color = ImRaii.PushColor(ImGuiCol.Tab, Colors.RedWood, isActiveGroup);

            if (ImGui.TabItemButton(group.Name))
                CurConfig.ActiveGroupIndex = tabBarIdx;
            ImGuiHelper.AddTooltip(GeneralLoc.Ui_rightClickHint);

            using var popup = ImRaii.ContextPopupItem(group.Name);
            if (popup)
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
                    if (ImGuiHelper.DeleteButton(group))
                    {
                        _module.RaidGroups.Remove(group);
                        ImGui.CloseCurrentPopup();
                    }
                }
            }
        }

        const string newGroupContextMenuId = "##NewGroupContextMenu";
        if (ImGui.TabItemButton(" + ")) ImGui.OpenPopup(newGroupContextMenuId);
        using var newPopup = ImRaii.ContextPopupItem(newGroupContextMenuId);
        if (newPopup)
        {
            if (ImGuiHelper.Button(LootmasterLoc.Ui_btn_newGroupAutomatic, null))
            {
                _module.AddGroup(new RaidGroup(LootmasterLoc.AutoCreatedGroupName), true);
                ImGui.CloseCurrentPopup();
            }

            if (ImGuiHelper.Button(LootmasterLoc.Ui_btn_newGroupManual,
                                   string.Format(GeneralLoc.Ui_btn_tt_addEmpty, RaidGroup.DataTypeName)))
            {
                UiSystem.EditWindows.Create(new RaidGroup(), _module.AddGroup);
                ImGui.CloseCurrentPopup();
            }
            if (ImGuiHelper.Button(string.Format(GeneralLoc.UiHelpers_txt_AddKnown, RaidGroup.DataTypeName),
                                   null))
            {
                _module.Services.HrtDataManager.RaidGroupDb.OpenSearchWindow(UiSystem, _module.AddGroup);
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private void DrawPlayerRow(RaidGroup group, int pos)
    {
        using var id = ImRaii.PushId(pos);
        var player = group[pos];
        //Sort Row
        ImGui.TableNextColumn();
        float fullLineHeight = ImGui.GetTextLineHeightWithSpacing();
        float lineSpacing = fullLineHeight - ImGui.GetTextLineHeight();
        float dualTopRowY = ImGui.GetCursorPosY() + lineSpacing * 2.1f;
        float dualBottomRowY = ImGui.GetCursorPosY() + fullLineHeight * 2.1f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineSpacing * 1.5f);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "##sortUp",
                               string.Format(GeneralLoc.SortableList_btn_tt_moveUp, Player.DataTypeName), pos > 0,
                               ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos - 1, pos);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + lineSpacing * 0.5f);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "##sortDown",
                               string.Format(GeneralLoc.SortableList_btn_tt_moveDown, Player.DataTypeName),
                               pos < CurrentGroup.Count - 1,
                               ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos, pos + 1);
        if (player.Filled)
        {
            //Player Column
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(30);
            ImGui.Text($"{player.MainChar.MainClass?.Role.FriendlyName() ?? Player.DataTypeName.Capitalized()}");
            ImGui.SameLine();
            UiSystem.Helpers.DrawPlayerCombo("##playerCombo", player, p => group[pos] = p, 80 * ScaleFactor);
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(player, "##editPlayer"))
                UiSystem.EditWindows.Create(player);
            UiSystem.Helpers.DrawCharacterCombo("##CharCombo", player, CurConfig.CharacterNameFormat,
                                                110 * ScaleFactor);
            ImGui.SameLine();
            if (ImGuiHelper.EditButton(player.MainChar, "##editCharacter"))
                UiSystem.EditWindows.Create(player.MainChar);
            var curJob = player.MainChar.MainClass;
            UiSystem.Helpers.DrawClassCombo("##Class", player.MainChar, 110 * ScaleFactor);
            if (curJob is not null)
            {
                ImGui.SameLine();
                if (ImGuiHelper.DeleteButton(curJob, true, new Vector2(25f)))
                    player.MainChar.RemoveClass(curJob.Job);
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
                using (ImRaii.PushId("##gearButtons"))
                {
                    /*
                     * Gear Sets
                     */
                    var gear = curJob.CurGear;
                    var bis = curJob.CurBis;
                    ImGui.TableNextColumn();
                    float comboWidth = 85f * ScaleFactor;
                    /*
                     * Current Gear
                     */
                    ImGui.SetCursorPosY(dualTopRowY);
                    UiSystem.Helpers.DrawGearSetCombo("##curGear", gear, curJob.GearSets, s => curJob.CurGear = s,
                                                      curJob.Job, comboWidth);
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
                    UiSystem.Helpers.DrawGearSetCombo("##curBis", bis, curJob.BisSets, s => curJob.CurBis = s,
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
                }
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
                    UiSystem.AddWindow(new QuickCompareWindow(
                                           UiSystem, (item, flags) => DrawSlot((item, GearItem.Empty), flags), curJob!,
                                           player.MainChar.Tribe));
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
                                   string.Format(GeneralLoc.Ui_btn_tt_addEmpty, Player.DataTypeName),
                                   true, ButtonSize))
                UiSystem.EditWindows.Create(player);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "##addPlayerFromDB",
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting, RaidGroup.DataTypeName), true,
                                   ButtonSize))
                _module.Services.HrtDataManager.PlayerDb.OpenSearchWindow(UiSystem,
                                                                          selected => group[pos] = selected);
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
                                   string.Format(GeneralLoc.Ui_btn_tt_addExisting, Character.DataTypeName), true,
                                   ButtonSize))
                _module.Services.HrtDataManager.CharDb.OpenSearchWindow(UiSystem, selected =>
                {
                    if (!_module.Services.HrtDataManager.PlayerDb.TryAdd(player))
                        return;
                    player.NickName = selected.Name.Split(' ')[0];
                    player.MainChar = selected;
                });
        }
    }
    private void DrawLootHandlerButtons()
    {
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(CurConfig.ActiveExpansion.Name).X + 32f * ScaleFactor);
        using (var combo = ImRaii.Combo("##expansion", CurConfig.ActiveExpansion.Name))
        {
            if (combo)
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
            }
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(CurConfig.SelectedRaidTier.Name).X + 32f * ScaleFactor);
        using (var combo = ImRaii.Combo("##raidTier", CurConfig.SelectedRaidTier.Name))
        {
            if (combo)
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
            }
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
    private void DrawSlot((GearItem, GearItem) itemTuple, SlotDrawFlags style = SlotDrawFlags.Default)
    {
        float originalY = ImGui.GetCursorPosY();
        float fullLineHeight = ImGui.GetTextLineHeightWithSpacing();
        float lineSpacing = fullLineHeight - ImGui.GetTextLineHeight();
        float cursorDualTopY = originalY + lineSpacing * 2f;
        float cursorDualBottomY = cursorDualTopY + fullLineHeight * 1.7f;
        float cursorSingleSmall = originalY + fullLineHeight + lineSpacing;
        float cursorSingleLarge = originalY + fullLineHeight * 0.7f + lineSpacing;
        bool extended = style.HasFlag(SlotDrawFlags.ExtendedView);
        bool singleItem = style.HasFlag(SlotDrawFlags.SingleItem);
        var comparisonMode = CurConfig.IgnoreMateriaForBiS
            ? ItemComparisonMode.IgnoreMateria : ItemComparisonMode.Full;
        var (item, bis) = itemTuple;
        if (!item.Filled && !bis.Filled)
            return;
        if (singleItem || item.Filled && bis.Filled && item.Equals(bis, comparisonMode))
        {
            ImGui.SetCursorPosY(extended ? cursorSingleLarge : cursorSingleSmall);
            using (ImRaii.Group())
            {
                DrawItem(item, true);
            }
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                item.Draw();
            }
            ImGui.NewLine();
        }
        else
        {
            using (ImRaii.Group())
            {
                ImGui.SetCursorPosY(cursorDualTopY);
                DrawItem(item);
                if (!extended)
                    ImGui.NewLine();
                ImGui.SetCursorPosY(cursorDualBottomY);
                DrawItem(bis);
            }
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (item.Filled && bis.Filled)
                    itemTuple.Draw();
                else if (item.Filled)
                {
                    ImGui.TextColored(Colors.TextPetrol, LootmasterLoc.ItemTooltip_hdg_Equipped);
                    item.Draw();
                }
                else
                {
                    ImGui.TextColored(Colors.TextPetrol, LootmasterLoc.ItemTooltip_hdg_bis);
                    bis.Draw();
                }
            }
        }
        void DrawItem(GearItem itemToDraw, bool multiLine = false)
        {
            if (itemToDraw.Filled)
            {
                if (extended || CurConfig.ShowIconInGroupOverview)
                {
                    var icon = UiSystem.GetIcon(itemToDraw);
                    {
                        Vector2 iconSize = new(ImGui.GetTextLineHeightWithSpacing()
                                             * (extended ? multiLine ? 2.4f : 1.4f : 1f));
                        ImGui.Image(icon.Handle, iconSize * ScaleFactor);
                        ImGui.SameLine();
                    }
                }
                string toDraw = string.Format(CurConfig.ItemFormatString,
                                              itemToDraw.ItemLevel,
                                              itemToDraw.Source().FriendlyName(),
                                              itemToDraw.Slots.FirstOrDefault(GearSetSlot.None).FriendlyName());
                if (extended) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fullLineHeight * (multiLine ? 0.7f : 0.2f));
                Action<string> drawText = CurConfig.ColoredItemNames
                    ? t => ImGui.TextColored(LevelColor(CurConfig, itemToDraw), t)
                    : t => ImGui.Text(t);
                drawText(toDraw);
                if (!extended || !itemToDraw.Materia.Any())
                    return;
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fullLineHeight * (multiLine ? 0.7f : 0.2f));
                ImGui.Text(
                    $"( {string.Join(" | ", itemToDraw.Materia.ToList().ConvertAll(mat => $"{mat.StatType.Abbrev()} +{mat.GetStat()}"))} )");
            }
            else
                ImGui.Text(GeneralLoc.CommonTerms_Empty);

        }
        Vector4 LevelColor(LootMasterConfiguration.ConfigData config, GearItem itemToColor)
        {
            return (config.SelectedRaidTier.ItemLevel(itemToColor.Slots.FirstOrDefault(GearSetSlot.None))
                  - (int)itemToColor.ItemLevel) switch
            {
                <= 0  => config.ItemLevelColors[0],
                <= 10 => config.ItemLevelColors[1],
                <= 20 => config.ItemLevelColors[2],
                _     => config.ItemLevelColors[3],
            };
        }
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