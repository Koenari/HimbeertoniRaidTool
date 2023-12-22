using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class LootmasterUi : HrtWindow
{
    private readonly LootMasterModule _lootMaster;
    private LootMasterConfiguration.ConfigData CurConfig => _lootMaster.ConfigImpl.Data;
    internal int CurrentGroupIndex { get; private set; }
    private RaidGroup CurrentGroup => CurConfig.RaidGroups[CurrentGroupIndex];
    private static GameExpansion CurrentExpansion => Common.Services.ServiceManager.GameInfo.CurrentExpansion;
    private readonly Queue<HrtUiMessage> _messageQueue = new();
    private (HrtUiMessage message, DateTime time)? _currentMessage;
    private readonly Vector2 _buttonSize;
    private readonly Vector2 _buttonSizeVertical;
    protected Vector2 ButtonSize => _buttonSize * ScaleFactor;
    protected Vector2 ButtonSizeVertical => _buttonSizeVertical * ScaleFactor;

    internal LootmasterUi(LootMasterModule lootMaster) : base("LootMaster")
    {
        _lootMaster = lootMaster;
        CurrentGroupIndex = 0;
        Size = new Vector2(1600, 670);
        _buttonSize = new Vector2(30f, 25f);
        _buttonSizeVertical = new Vector2(_buttonSize.Y, _buttonSize.X);
        SizeCondition = ImGuiCond.FirstUseEver;
        Title = Localize("LootMasterWindowTitle", "Loot Master");
        ServiceManager.PluginInterface.UiBuilder.OpenMainUi += Show;
    }

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
        foreach (HrtWindow? w in _lootMaster.WindowSystem.Windows.Where(x => !x.IsOpen).Cast<HrtWindow>())
        {
            toRemove.Enqueue(w);
        }
        foreach (HrtWindow w in toRemove.Where(w => !w.Equals(this)))
        {
            ServiceManager.PluginLog.Debug($"Cleaning Up Window: {w.WindowName}");
            _lootMaster.WindowSystem.RemoveWindow(w);
        }
    }

    private static TimeSpan MessageTimeByMessageType(HrtUiMessageType type) => type switch
    {
        HrtUiMessageType.Info => TimeSpan.FromSeconds(3),
        HrtUiMessageType.Success or HrtUiMessageType.Warning => TimeSpan.FromSeconds(5),
        _ => TimeSpan.FromSeconds(10),
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
            HrtUiMessageType.Success => Colors.TextGreen,
            HrtUiMessageType.Warning => Colors.TextYellow,
            HrtUiMessageType.Important => Colors.TextSoftRed,
            _ => Colors.TextWhite,
        };
        ImGui.TextColored(color, _currentMessage.Value.message.Message);
    }

    private void DrawDetailedPlayer(Player p)
    {
        PlayableClass? curClass = p.MainChar.MainClass;
        ImGui.BeginChild("SoloView");
        ImGui.Columns(3);
        /*
         * Job Selection
         */
        ImGui.Text($"{p.NickName} : {p.MainChar.Name} @ {p.MainChar.HomeWorld?.Name ?? "n.A"}");
        ImGui.SameLine();

        ImGuiHelper.GearUpdateButtons(p, _lootMaster, true);
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditPlayer{p.NickName}",
                $"{Localize("Edit player")} {p.NickName}"))
            AddChild(new EditPlayerWindow(p));
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditCharacter{p.NickName}",
                $"{Localize("Edit character")} {p.MainChar.Name}"))
            AddChild(new EditCharacterWindow(p.MainChar));
        ImGui.BeginChild("JobList");
        foreach (PlayableClass playableClass in p.MainChar.Classes)
        {
            ImGui.PushID($"{playableClass.Job}");
            ImGui.Separator();
            ImGui.Spacing();
            if (ImGuiHelper.Button(FontAwesomeIcon.Eraser, "Delete",
                    Localize("lootmaster:detail:deleteJob:tooltip", "Delete job (hold Shift)"),
                    ImGui.IsKeyDown(ImGuiKey.ModShift)))
                p.MainChar.RemoveClass(playableClass.Job);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "JobUp",
                    Localize("lootmaster:detail:jobUp:tooltip", "Move job up"), p.MainChar.CanMoveUp(playableClass)))
                p.MainChar.MoveClassUp(playableClass);
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "JobDown",
                    Localize("lootmaster:detail:jobDown:tooltip", "Move job down"),
                    p.MainChar.CanMoveDown(playableClass)))
                p.MainChar.MoveClassDown(playableClass);
            bool isMainJob = p.MainChar.MainJob == playableClass.Job;

            if (isMainJob)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            ImGui.SameLine();
            if (ImGuiHelper.Button(playableClass.Job.ToString(), null, true, new Vector2(38f * ScaleFactor, 0f)))
                p.MainChar.MainJob = playableClass.Job;
            if (isMainJob)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text("Level: " + playableClass.Level);
            //Current Gear
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 50 * ScaleFactor);
            GearSet? newCur = null;
            ImGui.SetNextItemWidth(80 * ScaleFactor);
            if (ImGui.BeginCombo("##CurGear", playableClass.CurGear.Name))
            {
                foreach (GearSet gearSet in playableClass.GearSets)
                {
                    if (ImGui.Selectable($"{gearSet.Name} ({gearSet.ItemLevel})"))
                        newCur = gearSet;
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            ImGui.Text(
                $"{Localize("iLvl", "iLvl")}: {playableClass.CurGear.ItemLevel:D3}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditGear",
                    $"{Localize("Edit", "Edit")} {playableClass.Job} {Localize("gear", "gear")}"))
                AddChild(
                    new EditGearSetWindow(playableClass.CurGear, playableClass.Job, g => playableClass.CurGear = g));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, $"QuickCompare",
                    $"{Localize("Quick compare", "Quick compare")}"))
                AddChild(new QuickCompareWindow(CurConfig, playableClass, p.MainChar.Tribe));
            if (newCur is not null) { playableClass.CurGear = newCur; }
            //BiS
            GearSet? newBis = null;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80 * ScaleFactor);
            if (ImGui.BeginCombo("##BisGear", playableClass.CurBis.Name))
            {
                foreach (GearSet gearSet in playableClass.BisSets)
                {
                    if (ImGui.Selectable($"{gearSet.Name} ({gearSet.ItemLevel})"))
                        newBis = gearSet;
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            ImGui.Text($"{Localize("iLvl", "iLvl")}: {playableClass.CurBis.ItemLevel:D3}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditBIS",
                    $"{Localize("Edit", "Edit")} {playableClass.CurBis.Name}"))
                AddChild(new EditGearSetWindow(playableClass.CurBis, playableClass.Job, g => playableClass.CurBis = g));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Download, playableClass.CurBis.EtroId,
                    string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), playableClass.CurBis.Name),
                    playableClass.CurBis is { ManagedBy: GearSetManager.Etro, EtroId.Length: > 0 }))
                ServiceManager.TaskManager.RegisterTask(
                    new HrtTask(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(playableClass.CurBis),
                        HandleMessage,
                        $"Update {playableClass.CurBis.Name} ({playableClass.CurBis.EtroId}) from etro"));
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
                Localize("Current", "Current"), " ", Localize("BiS", "BiS"),
                LmUiHelpers.StatTableCompareMode.DoCompare | LmUiHelpers.StatTableCompareMode.DiffRightToLeft);

        /*
         * Show Gear
         */
        ImGui.NextColumn();
        ImGui.BeginTable("SoloGear", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Borders);
        ImGui.TableSetupColumn(Localize("Gear", "Gear"));
        ImGui.TableSetupColumn("");
        ImGui.TableHeadersRow();
        if (curClass is not null)
        {
            DrawSlot(curClass[GearSetSlot.MainHand], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.OffHand], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Head], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Ear], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Body], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Neck], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Hands], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Wrist], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Legs], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Ring1], SlotDrawFlags.ExtendedView);
            DrawSlot(curClass[GearSetSlot.Feet], SlotDrawFlags.ExtendedView);
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
        if (ImGuiHelper.Button(FontAwesomeIcon.Cog, "showConfig",
                Localize("lootmaster:button:showConfig:tooltip", "Open Configuration")))
            ServiceManager.Config.Show();
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
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "Solo", Localize("Add Player", "Add Player")))
                    AddChild(new EditPlayerWindow(CurrentGroup[0]));
            }
        }
        else
        {
            if (ImGui.BeginTable("RaidGroup", 15,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn(Localize("lootmaster:group:columns:sort:title", "Sort"));
                ImGui.TableSetupColumn(Localize("lootmaster:group:columns:player:title", "Player"));
                ImGui.TableSetupColumn(Localize("lootmaster:group:columns:gear:title", "Gear"));
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
        if (!ImGui.BeginTabBar("RaidGroupSwitchBar"))
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
            ImGuiHelper.AddTooltip(Localize("Lootmaster:RaidGroupBar:Button:Tooltip", "Right click for more options"));

            if (ImGui.BeginPopupContextItem(group.Name))
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditGroup",
                        string.Format(Localize("Lootmaster:RaidGroupBar:Button:Edit:tooltip", "Edit group {0}"),
                            group.Name)))
                {
                    group.TypeLocked |= isPredefinedSolo;
                    AddChild(new EditGroupWindow(group));
                    ImGui.CloseCurrentPopup();
                }

                if (!isPredefinedSolo)
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "DeleteGroup",
                            string.Format(
                                Localize("lootmaster:button:removeGroup:tooltip", "Remove group {0} (hold shift)"),
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

        const string newGroupContextMenuId = "NewGroupContextMenu";
        if (ImGui.TabItemButton("+")) ImGui.OpenPopup(newGroupContextMenuId);
        if (ImGui.BeginPopupContextItem(newGroupContextMenuId))
        {
            if (ImGuiHelper.Button(Localize("lootmaster:button:newGroupFromCurrent", "From current Group"), null))
            {
                _lootMaster.AddGroup(new RaidGroup(Localize("AutoCreatedGroupName", "Auto Created")), true);
                ImGui.CloseCurrentPopup();
            }

            if (ImGuiHelper.Button(Localize("lootmaster:button:newGroupFromScratch", "From scratch"),
                    Localize("lootmaster:button:newGroupFromScratch:tooltip", "Add empty group")))
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
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowUp, "sortUp", Localize("LootMaster:SortButton:up", "Move up"),
                pos > 0, ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos - 1, pos);
        if (ImGuiHelper.Button(FontAwesomeIcon.ArrowDown, "sortDown",
                Localize("LootMaster:SortButton:down", "Move down"), pos < CurrentGroup.Count - 1, ButtonSizeVertical))
            CurrentGroup.SwapPlayers(pos, pos + 1);
        if (player.Filled)
        {
            //Player Column
            ImGui.TableNextColumn();
            ImGui.Text(
                $"{player.CurJob?.Role.FriendlyName() ?? Localize("lootmaster:player", "Player")}:   {player.NickName}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditPlayer",
                    string.Format(Localize("lootmaster:button:editPlayer:tooltip", "Edit player {0}"),
                        player.NickName)))
                AddChild(new EditPlayerWindow(player));
            ImGui.Text(
                $"{player.MainChar.Name} @ {player.MainChar.HomeWorld?.Name ?? Localize("general:notAvailable:abbrev", "n.A.")}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditCharacter",
                    string.Format(Localize("lootmaster:button:editCharacter:tooltip", "Edit character {0}"),
                        player.MainChar.Name)))
                AddChild(new EditCharacterWindow(player.MainChar));
            PlayableClass? curJob = player.CurJob;
            if (player.MainChar.Classes.Any())
            {
                ImGui.SetNextItemWidth(110 * ScaleFactor);
                if (ImGui.BeginCombo($"##Class", curJob?.ToString()))
                {
                    foreach (PlayableClass job in player.MainChar)
                    {
                        if (ImGui.Selectable(job.ToString()))
                            player.MainChar.MainJob = job.Job;
                    }
                    ImGui.EndCombo();
                }
            }
            else
            {
                ImGui.Text(Localize("lootmaster:noJobs", "No jobs created"));
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


                //Gear Column
                ImGui.PushID("GearButtons");
                GearSet gear = curJob.CurGear;
                GearSet bis = curJob.CurBis;
                ImGui.TableNextColumn();
                float curY = ImGui.GetCursorPosY();
                ImGui.SetCursorPosY(curY + 4 * ScaleFactor);
                ImGui.Text($"{gear.ItemLevel:D3}");
                ImGuiHelper.AddTooltip(gear.Name);
                ImGui.SameLine();
                ImGui.SetCursorPosY(curY + 3 * ScaleFactor);
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditCurGear",
                        string.Format(Localize("lootmaster:button:editGearSet:tooltip", "Edit gearset: {0}"),
                            gear.Name), true, ButtonSize))
                    AddChild(new EditGearSetWindow(gear, curJob.Job, g => curJob.CurGear = g));
                ImGui.SameLine();
                ImGui.SetCursorPosY(curY + 3 * ScaleFactor);
                ImGuiHelper.GearUpdateButtons(player, _lootMaster, false, ButtonSize);
                //ImGui.Text($"{bis.ItemLevel - gear.ItemLevel} {Localize("to BIS", "to BIS")}");
                curY = ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() / 2f;
                ImGui.SetCursorPosY(curY + 5 * ScaleFactor);
                ImGui.Text($"{bis.ItemLevel:D3}");
                if (ImGui.IsItemClicked())
                    ServiceManager.TaskManager.RegisterTask(new HrtTask(() =>
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = EtroConnector.GEARSET_WEB_BASE_URL + bis.EtroId,
                            UseShellExecute = true,
                        });
                        return new HrtUiMessage("");
                    }, _ => { }, "Open Etro"));
                ImGuiHelper.AddTooltip(EtroConnector.GEARSET_WEB_BASE_URL + bis.EtroId);

                ImGui.SameLine();
                ImGui.SetCursorPosY(curY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditBiSGear",
                        string.Format(Localize("lootmaster:button:editGearSet:tooltip", "Edit gearset: {0}"),
                            gear.Name), true, ButtonSize))
                    AddChild(new EditGearSetWindow(bis, curJob.Job, g => curJob.CurBis = g));
                ImGui.SameLine();
                ImGui.SetCursorPosY(curY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Download, bis.EtroId,
                        string.Format(
                            Localize("lootmaster:button:etroUpdate:tooltip", "Update gear set \"{0}\" from Etro.gg"),
                            bis.Name),
                        bis is { ManagedBy: GearSetManager.Etro, EtroId.Length: > 0 }, ButtonSize))
                    ServiceManager.TaskManager.RegisterTask(
                        new HrtTask(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(bis), HandleMessage,
                            $"Update {bis.Name} ({bis.EtroId}) from etro"));
                ImGui.PopID();
                foreach ((GearSetSlot slot, (GearItem, GearItem) itemTuple) in curJob.ItemTuples)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    DrawSlot(itemTuple);
                }
            }

            /*
             * Start of functional button section
             */
            {
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, $"QuickCompare",
                        $"{Localize("lootmaster:button:quickCompare:tooltip", "Quickly compare gear")}", curJob != null,
                        ButtonSize))
                    AddChild(new QuickCompareWindow(CurConfig, curJob!, player.MainChar.Tribe));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Wallet, "Inventory",
                        Localize("lootmaster:button:inventory:tooltip", "Open inventory window"), true, ButtonSize))
                    AddChild(new InventoryWindow(player.MainChar.MainInventory,
                        string.Format(Localize("lootmaster:inventoryWindow:title", "{0}'s inventory"),
                            player.MainChar.Name)));
                if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, "Details",
                        $"{Localize("lootmaster:button:playerDetails:tooltip", "Show player details for")} {player.NickName}",
                        true, ButtonSize))
                    AddChild(new PlayerDetailWindow(this, player));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "Delete",
                        string.Format(
                            Localize("lootmaster:button:removePlayer:tooltip",
                                "Remove {0} from this group (hold shift)"), player.NickName),
                        ImGui.IsKeyDown(ImGuiKey.ModShift), ButtonSize))
                    group[pos] = new Player();
            }
        }
        else
        {
            ImGui.TableNextColumn();
            ImGui.NewLine();
            ImGui.Text(Localize("lootmaster:emptyPlayer", "No Player"));
            ImGui.Text(" ");
            for (int i = 0; i < GearSet.NUM_SLOTS; i++)
            {
                ImGui.TableNextColumn();
            }
            ImGui.TableNextColumn();
            if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "AddNew",
                    Localize("lootmaster:button:NewPlayerEmpty:tooltip", "Add empty"), true, ButtonSize))
                AddChild(new EditPlayerWindow(player));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "AddPlayerFromDB",
                    Localize("lootmaster:button:PLayerFromDb", "Add player from DB"), true, ButtonSize))
                AddChild(ServiceManager.HrtDataManager.PlayerDb.OpenSearchWindow(selected => group[pos] = selected));
            if (ImGuiHelper.Button(FontAwesomeIcon.LocationCrosshairs, "AddTarget",
                    $"{Localize("lootmaster:button:PLayerFromTarget:tooltip", "Fill with target")} "
                    + $"({ServiceManager.TargetManager.Target?.Name ?? Localize("None")})",
                    ServiceManager.TargetManager.Target is not null, ButtonSize))
            {
                _lootMaster.FillPlayerFromTarget(player);
                if (ServiceManager.TargetManager.Target is PlayerCharacter target)
                    GearRefresher.RefreshGearInfos(target);
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "AddCharFromDB",
                    Localize("lootmaster:button:CharacterFromDb:tooltip", "Add character from DB"), true, ButtonSize))
                AddChild(ServiceManager.HrtDataManager.CharDb.OpenSearchWindow(selected =>
                {
                    if (!ServiceManager.HrtDataManager.PlayerDb.TryAdd(player))
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
        if (ImGui.BeginCombo("##Raid Tier", CurConfig.SelectedRaidTier.Name))
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
        ImGui.Text(Localize("lootmaster:text:lootDistribution ", "Distribute loot for:"));
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
            Title = $"{Localize("lootmaster:window:playerDetails:title", "Player Details")} {_player.NickName}";
            (Size, SizeCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);
        }

        public override void Draw() => _drawPlayer(_player);
    }
}

internal class InventoryWindow : HrtWindowWithModalChild
{
    private readonly Inventory _inv;

    internal InventoryWindow(Inventory inv, string title)
    {
        Size = new Vector2(400f, 550f);
        SizeCondition = ImGuiCond.Appearing;
        Title = title;
        _inv = inv;
        if (Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage is null)
            return;
        foreach (HrtItem item in from boss in Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage
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
        if (Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage is not null)
            foreach (InstanceWithLoot boss in Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage
                         .Bosses)
            foreach (HrtItem item in boss.GuaranteedItems)
            {
                IDalamudTextureWrap icon = ServiceManager.IconCache[item.Icon];
                ImGui.Image(icon.ImGuiHandle, icon.Size);
                ImGui.SameLine();
                ImGui.Text(item.Name);
                ImGui.SameLine();
                InventoryEntry entry = _inv[_inv.IndexOf(item.Id)];
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                ImGui.InputInt($"##{item.Name}", ref entry.Quantity);
                _inv[_inv.IndexOf(item.Id)] = entry;
            }

        ImGui.Separator();
        ImGui.Text(Localize("Inventory:AdditionalGear", "Additional Gear"));
        Vector2 iconSize = new Vector2(37, 37) * ScaleFactor;
        foreach ((int idx, InventoryEntry entry) in _inv.Where(e => e.Value.IsGear))
        {
            ImGui.PushID(idx);
            if (entry.Item is not GearItem item)
                continue;
            IDalamudTextureWrap icon = ServiceManager.IconCache[item.Icon];
            if (ImGuiHelper.Button(FontAwesomeIcon.Trash, "Delete", null, true, iconSize))
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
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, $"Add", null, true, iconSize))
            ModalChild = new SelectGearItemWindow(i => _inv.Add(_inv.FirstFreeSlot(), i), _ => { }, null, null, null,
                Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage?.ArmorItemLevel ?? 0);
        ImGui.EndDisabled();
    }
}