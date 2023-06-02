using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;
internal class LootmasterUI : HrtWindow
{

    private readonly LootMasterModule _lootMaster;
    private LootMasterConfiguration.ConfigData CurConfig => _lootMaster.Configuration.Data;
    internal int _CurrenGroupIndex { get; private set; }
    private RaidGroup CurrentGroup => RaidGroups[_CurrenGroupIndex];
    private static GameExpansion CurrentExpansion => Common.Services.ServiceManager.GameInfo.CurrentExpansion;
    private static List<RaidGroup> RaidGroups => ServiceManager.HrtDataManager.Groups;
    private readonly Queue<HrtUiMessage> _messageQueue = new();
    private (HrtUiMessage message, DateTime time)? _currentMessage;
    private readonly Vector2 _buttonSize;
    private Vector2 ButtonSize => _buttonSize * ScaleFactor;
    internal LootmasterUI(LootMasterModule lootMaster) : base("LootMaster")
    {
        _lootMaster = lootMaster;
        _CurrenGroupIndex = 0;
        Size = new Vector2(1600, 670);
        _buttonSize = new Vector2(30f, 25f);
        SizeCondition = ImGuiCond.FirstUseEver;
        Title = Localize("LootMasterWindowTitle", "Loot Master");
    }
    private bool AddChild(HrtWindow child)
    {
        if (_lootMaster.WindowSystem.Windows.Any(w => child.Equals(w)))
        {
            child.Hide();
            return false;
        }
        _lootMaster.WindowSystem.AddWindow(child);
        child.Show();
        return true;
    }
    public override void OnOpen()
    {
        _CurrenGroupIndex = CurConfig.LastGroupIndex;
    }
    public override void Update()
    {
        base.Update();
        Queue<HrtWindow> toRemove = new();
        foreach (var w in _lootMaster.WindowSystem.Windows.Where(x => !x.IsOpen).Cast<HrtWindow>())
            toRemove.Enqueue(w);
        foreach (var w in toRemove)
        {
            if (!w.Equals(this))
            {
                PluginLog.Debug($"Cleaning Up Window: {w.WindowName}");
                _lootMaster.WindowSystem.RemoveWindow(w);
            }

        }
    }
    private static TimeSpan MessageTimeByMessgeType(HrtUiMessageType type) => type switch
    {
        HrtUiMessageType.Info
        => TimeSpan.FromSeconds(3),
        HrtUiMessageType.Success
        or HrtUiMessageType.Warning
            => TimeSpan.FromSeconds(5),
        HrtUiMessageType.Failure or HrtUiMessageType.Error
        or HrtUiMessageType.Important
            => TimeSpan.FromSeconds(10),
        _ => TimeSpan.FromSeconds(10),
    };
    private void DrawUiMessages()
    {
        if (_currentMessage.HasValue && _currentMessage.Value.time + MessageTimeByMessgeType(_currentMessage.Value.message.MessageType) < DateTime.Now)
            _currentMessage = null;
        if (!_currentMessage.HasValue && _messageQueue.TryDequeue(out var message))
            _currentMessage = (message, DateTime.Now);
        if (_currentMessage.HasValue)
        {
            switch (_currentMessage.Value.message.MessageType)
            {
                case HrtUiMessageType.Error or HrtUiMessageType.Failure:
                    ImGui.TextColored(new Vector4(0.85f, 0.17f, 0.17f, 1f), _currentMessage.Value.message.Message);
                    break;
                case HrtUiMessageType.Success:
                    ImGui.TextColored(new Vector4(0.17f, 0.85f, 0.17f, 1f), _currentMessage.Value.message.Message);
                    break;
                case HrtUiMessageType.Warning:
                    ImGui.TextColored(new Vector4(0.85f, 0.85f, 0.17f, 1f), _currentMessage.Value.message.Message);
                    break;
                case HrtUiMessageType.Important:
                    ImGui.TextColored(new Vector4(0.85f, 0.27f, 0.27f, 1f), _currentMessage.Value.message.Message);
                    break;
                default:
                    ImGui.Text(_currentMessage.Value.message.Message);
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

        ImGuiHelper.GearUpdateButtons(p, _lootMaster, true);
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditPlayer{p.NickName}", $"{Localize("Edit player", "Edit player")} {p.NickName}"))
        {
            AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, p, _lootMaster.Configuration.Data.GetDefaultBiS));
        }
        ImGui.BeginChild("JobList");
        foreach (var playableClass in p.MainChar.Classes)
        {
            ImGui.Separator();
            ImGui.Spacing();
            bool isMainJob = p.MainChar.MainJob == playableClass.Job;

            if (isMainJob)
                ImGui.PushStyleColor(ImGuiCol.Button, Colors.RedWood);
            if (ImGuiHelper.Button(playableClass.Job.ToString(), null, true, new Vector2(38f * ScaleFactor, 0f)))
                p.MainChar.MainJob = playableClass.Job;
            if (isMainJob)
                ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text("Level: " + playableClass.Level);
            //Current Gear
            ImGui.SameLine();
            ImGui.Text($"{Localize("Current", "Current")} {Localize("iLvl", "iLvl")}: {playableClass.Gear.ItemLevel:D3}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditGear{playableClass.Job}", $"{Localize("Edit", "Edit")} {playableClass.Job} {Localize("gear", "gear")}"))
                AddChild(new EditGearSetWindow(playableClass.Gear, playableClass.Job));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.MagnifyingGlassChart, $"QuickCompare{playableClass.Job}", $"{Localize("Quick compare", "Quick compare")}"))
                AddChild(new QuickCompareWindow(CurConfig, playableClass));
            //BiS
            ImGui.SameLine();
            ImGui.Text($"{Localize("BiS", "BiS")} {Localize("iLvl", "iLvl")}: {playableClass.BIS.ItemLevel:D3}");
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Edit, $"EditBIS{playableClass.Job}", $"{Localize("Edit", "Edit")} {playableClass.BIS.Name}"))
                AddChild(new EditGearSetWindow(playableClass.BIS, playableClass.Job));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Download, playableClass.BIS.EtroID,
                string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), playableClass.BIS.Name), playableClass.BIS.EtroID.Length > 0))
                ServiceManager.TaskManager.RegisterTask(
                    new(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(playableClass.BIS), HandleMessage));
            ImGui.Spacing();
        }
        ImGui.EndChild();
        /**
         * Stat Table
         */
        ImGui.NextColumn();
        if (curClass is not null)
            LmUiHelpers.DrawStatTable(curClass, curClass.Gear, curClass.BIS,
                Localize("Current", "Current"), " ", Localize("BiS", "BiS"),
                LmUiHelpers.StatTableCompareMode.DoCompare | LmUiHelpers.StatTableCompareMode.DiffRightToLeft);

        /**
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
            for (int i = 0; i < 12; i++)
            {
                ImGui.TableNextColumn();
            }
        }
        ImGui.EndTable();
        ImGui.EndChild();
    }
    public override void Draw()
    {
        if (_CurrenGroupIndex > RaidGroups.Count - 1 || _CurrenGroupIndex < 0)
            _CurrenGroupIndex = 0;
        DrawUiMessages();
        if (ImGuiHelper.Button(FontAwesomeIcon.Cog, "showconfig", Localize("lootmaster:button:showconfig:tooltip", "Open Configuration")))
            ServiceManager.Config.Show();
        ImGui.SameLine();
        DrawLootHandlerButtons();
        DrawRaidGroupSwitchBar();
        if (CurrentGroup.Type == GroupType.Solo)
        {
            if (CurrentGroup[0].MainChar.Filled)
                DrawDetailedPlayer(CurrentGroup[0]);
            else
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "Solo", Localize("Add Player", "Add Player")))
                    AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, CurrentGroup[0], CurConfig.GetDefaultBiS));
            }
        }
        else
        {
            if (ImGui.BeginTable("RaidGroup", 14,
            ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn(Localize("Player", "Player"));
                ImGui.TableSetupColumn(Localize("itemLevelShort", "iLvl"));
                foreach (var slot in GearSet.Slots)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    ImGui.TableSetupColumn(slot.FriendlyName(true));
                }
                ImGui.TableSetupColumn(Localize("Options", "Options"));
                ImGui.TableHeadersRow();
                for (int position = 0; position < CurrentGroup.Count; position++)
                {
                    ImGui.PushID(position.ToString());
                    DrawPlayerRow(CurrentGroup[position], position);
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }
    }

    private void DrawRaidGroupSwitchBar()
    {
        ImGui.BeginTabBar("RaidGroupSwichtBar");

        for (int tabBarIdx = 0; tabBarIdx < RaidGroups.Count; tabBarIdx++)
        {
            bool colorPushed = false;
            var g = RaidGroups[tabBarIdx];
            if (tabBarIdx == _CurrenGroupIndex)
            {
                ImGui.PushStyleColor(ImGuiCol.Tab, Colors.RedWood);
                colorPushed = true;
            }
            if (ImGui.TabItemButton($"{g.Name}##{tabBarIdx}"))
                _CurrenGroupIndex = tabBarIdx;
            ImGuiHelper.AddTooltip(Localize("GroupTabTooltip", "Right click for more options"));
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
                    if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "DeleteGroup", Localize("Delete group", "Delete group")))
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
                _lootMaster.AddGroup(new(Localize("AutoCreatedGroupName", "Auto Created")), true);
                ImGui.CloseCurrentPopup();
            }
            if (ImGuiHelper.Button(Localize("From scratch", "From scratch"), Localize("Add empty group", "Add empty group")))
            {
                AddChild(new EditGroupWindow(new RaidGroup(), group => _lootMaster.AddGroup(group, false)));
            }
            ImGui.EndPopup();
        }
        ImGui.EndTabBar();
    }

    private void DrawPlayerRow(Player player, int pos)
    {
        if (player.Filled)
        {
            //Player Column
            ImGui.TableNextColumn();
            ImGui.Text($"{player.CurJob?.Role.FriendlyName()}:   {player.NickName}");
            ImGui.Text($"{player.MainChar.Name} @ {player.MainChar.HomeWorld?.Name ?? Localize("n.A.", "n.A.")}");
            var curJob = player.CurJob;
            if (curJob != null)
            {
                if (player.MainChar.Classes.Count() > 1)
                {
                    ImGui.SetNextItemWidth(110 * ScaleFactor);
                    if (ImGui.BeginCombo($"##Class", curJob.ToString()))
                    {
                        foreach (var job in player.MainChar)
                        {
                            if (ImGui.Selectable(job.ToString()))
                                player.MainChar.MainJob = job.Job;
                        }
                        ImGui.EndCombo();
                    }
                }
                else
                {
                    ImGui.Text(curJob.ToString());
                }
                //Gear Column
                ImGui.PushID("GearButtons");
                var gear = curJob.Gear;
                var bis = curJob.BIS;
                ImGui.TableNextColumn();
                float curY = ImGui.GetCursorPosY();
                ImGui.SetCursorPosY(curY + 4 * ScaleFactor);
                ImGui.Text($"{gear.ItemLevel:D3}");
                ImGuiHelper.AddTooltip(gear.Name);
                ImGui.SameLine();
                ImGui.SetCursorPosY(curY + 3 * ScaleFactor);
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditCurGear", $"Edit {gear.Name}", true, ButtonSize))
                    AddChild(new EditGearSetWindow(gear, curJob.Job)); ;
                ImGui.SameLine();
                ImGui.SetCursorPosY(curY + 3 * ScaleFactor);
                ImGuiHelper.GearUpdateButtons(player, _lootMaster, false, ButtonSize);
                //ImGui.Text($"{bis.ItemLevel - gear.ItemLevel} {Localize("to BIS", "to BIS")}");
                curY = ImGui.GetCursorPosY() + ImGui.GetTextLineHeightWithSpacing() / 2f;
                ImGui.SetCursorPosY(curY + 5 * ScaleFactor);
                ImGui.Text($"{bis.ItemLevel:D3}");
                if (ImGui.IsItemClicked())
                    ServiceManager.TaskManager.RegisterTask(new(() =>
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = EtroConnector.GearsetWebBaseUrl + bis.EtroID,
                            UseShellExecute = true,
                        });
                        return new HrtUiMessage();
                    }, a => { }));
                ImGuiHelper.AddTooltip(EtroConnector.GearsetWebBaseUrl + bis.EtroID);

                ImGui.SameLine();
                ImGui.SetCursorPosY(curY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "EditBiSGear", $"Edit {bis.Name}", true, ButtonSize))
                    AddChild(new EditGearSetWindow(bis, curJob.Job));
                ImGui.SameLine();
                ImGui.SetCursorPosY(curY);
                if (ImGuiHelper.Button(FontAwesomeIcon.Download, bis.EtroID,
                    string.Format(Localize("UpdateBis", "Update \"{0}\" from Etro.gg"), bis.Name), bis.EtroID.Length > 0, ButtonSize))
                    ServiceManager.TaskManager.RegisterTask(
                        new(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(bis), HandleMessage));
                ImGui.PopID();
                foreach ((var slot, var itemTuple) in curJob.ItemTuples)
                {
                    if (slot == GearSetSlot.OffHand)
                        continue;
                    DrawSlot(itemTuple);
                }
            }
            else
            {
                ImGui.Text("");
                for (int i = 0; i < 12; i++)
                    ImGui.TableNextColumn();
            }
            /**
             * Start of functional button section
             */
            {
                ImGui.TableNextColumn();
                ImGuiHelper.GearUpdateButtons(player, _lootMaster, false, ButtonSize);
                var buttonSize = ImGui.GetItemRectSize();
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.ArrowsAltV, $"Rearrange", Localize("lootmaster:button:swapposition:tooltip", "Swap Position"), true, ButtonSize))
                {
                    AddChild(new SwapPositionWindow(pos, CurrentGroup));
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.Edit, "Edit",
                    $"{Localize("Edit", "Edit")} {player.NickName}", true, buttonSize))
                {
                    AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, player, CurConfig.GetDefaultBiS));
                }
                if (ImGuiHelper.Button(FontAwesomeIcon.Wallet, "Inventory", Localize("lootmaster:button:inventorytooltip", "Open inventory window"), true, buttonSize))
                    AddChild(new InventoryWindow(player.MainChar.MainInventory, $"{player.MainChar.Name}'s Inventory"));
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.SearchPlus, "Details",
                    $"{Localize("PlayerDetails", "Show player details for")} {player.NickName}", true, buttonSize))
                {
                    AddChild(new PlayerdetailWindow(this, player));
                }
                ImGui.SameLine();
                if (ImGuiHelper.Button(FontAwesomeIcon.TrashAlt, "Delete",
                    $"{Localize("Delete", "Delete")} {player.NickName}", true, buttonSize))
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
                AddChild(new EditPlayerWindow(_lootMaster.HandleMessage, player, CurConfig.GetDefaultBiS));
            ImGui.SameLine();
            if (ImGuiHelper.Button(FontAwesomeIcon.Search, "AddFromDB", Localize("Add from DB", "Add from DB")))
                AddChild(new GetCharacterFromDBWindow(ref player));
        }
    }
    private void DrawSlot((GearItem, GearItem) itemTuple, SlotDrawFlags style = SlotDrawFlags.Default)
        => LmUiHelpers.DrawSlot(CurConfig, itemTuple, style);

    private void DrawLootHandlerButtons()
    {
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(CurConfig.SelectedRaidTier.Name).X + 32f * ScaleFactor);
        if (ImGui.BeginCombo("##Raid Tier", CurConfig.SelectedRaidTier.Name))
        {
            for (int i = 0; i < CurrentExpansion.SavageRaidTiers.Length; i++)
            {
                var tier = CurrentExpansion.SavageRaidTiers[i];
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
        ImGui.Text(Localize("Distribute loot for:", "Distribute loot for:"));
        ImGui.SameLine();

        foreach (var lootSource in CurConfig.SelectedRaidTier.Bosses)
        {
            if (ImGuiHelper.Button(lootSource.Name, null))
            {
                AddChild(new LootSessionUI(lootSource, CurrentGroup, CurConfig.LootRuling, CurConfig.RolePriority));
            }
            ImGui.SameLine();
        }
        ImGui.NewLine();
    }

    internal void HandleMessage(HrtUiMessage message)
    {
        _messageQueue.Enqueue(message);
    }

    private class PlayerdetailWindow : HrtWindow
    {
        private readonly Action<Player> DrawPlayer;
        private readonly Player Player;
        public PlayerdetailWindow(LootmasterUI lmui, Player p) : base()
        {
            DrawPlayer = lmui.DrawDetailedPlayer;
            Player = p;
            Show();
            Title = $"{Localize("PlayerDetailsTitle", "Player Details")} {Player.NickName}";
            (Size, SizeCondition) = (new Vector2(1600, 600), ImGuiCond.Appearing);
        }
        public override void Draw() => DrawPlayer(Player);
    }
}
internal class GetCharacterFromDBWindow : HrtWindow
{
    private readonly Player _p;
    private readonly uint[] Worlds;
    private readonly string[] WorldNames;
    private int worldSelectIndex;
    private string[] CharacterNames = Array.Empty<string>();
    private int CharacterNameIndex = 0;
    private string NickName = " ";
    internal GetCharacterFromDBWindow(ref Player p) : base($"GetCharacterFromDBWindow{p.NickName}")
    {
        _p = p;
        Worlds = ServiceManager.HrtDataManager.CharDB.GetUsedWorlds().ToArray();
        WorldNames = Array.ConvertAll(Worlds, x => ServiceManager.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(x)?.Name.RawString ?? "");
        Title = Localize("GetCharacterTitle", "Get character from DB");
        Size = new Vector2(350, 420);
        Flags = ImGuiWindowFlags.NoScrollbar;
    }
    public override void Draw()
    {
        ImGui.InputText(Localize("Player Name", "Player Name"), ref NickName, 50);
        if (ImGui.ListBox("World", ref worldSelectIndex, WorldNames, WorldNames.Length))
        {
            var list = ServiceManager.HrtDataManager.CharDB.GetKnownChracters(Worlds[worldSelectIndex]);
            CharacterNames = list.ToArray();
            Array.Sort(CharacterNames);
        }
        ImGui.ListBox("Name", ref CharacterNameIndex, CharacterNames, CharacterNames.Length);
        if (ImGuiHelper.Button(FontAwesomeIcon.Save, "save", Localize("Save", "Save")))
        {

            _p.NickName = NickName;
            var c = _p.MainChar;
            c.Name = CharacterNames[CharacterNameIndex];
            c.HomeWorldID = Worlds[worldSelectIndex];
            if (ServiceManager.HrtDataManager.CharDB.SearchCharacter(c.HomeWorldID, c.Name, out c))
                _p.MainChar = c!;
            Hide();
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.WindowClose, "cancel", Localize("Cancel", "Cancel")))
            Hide();
    }
}
internal class InventoryWindow : HRTWindowWithModalChild
{
    private readonly Inventory _inv;

    internal InventoryWindow(Inventory inv, string title)
    {
        Size = new(400f, 550f);
        SizeCondition = ImGuiCond.Appearing;
        Title = title;
        _inv = inv;
        foreach (var boss in Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.Bosses)
            foreach (var item in boss.GuaranteedItems)
            {
                if (!_inv.Contains(item.ID))
                {
                    _inv[_inv.FirstFreeSlot()] = new(item)
                    {
                        quantity = 0
                    };
                }
            }

    }
    public override void Draw()
    {
        if (ImGuiHelper.CloseButton())
            Hide();
        foreach (var boss in Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.Bosses)
            foreach (var item in boss.GuaranteedItems)
            {
                var icon = ServiceManager.IconCache[item.Item!.Icon];
                ImGui.Image(icon.ImGuiHandle, icon.Size());
                ImGui.SameLine();
                ImGui.Text(item.Name);
                ImGui.SameLine();
                var entry = _inv[_inv.IndexOf(item.ID)];
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                ImGui.InputInt($"##{item.Name}", ref entry.quantity);
                _inv[_inv.IndexOf(item.ID)] = entry;
            }
        ImGui.Separator();
        ImGui.Text(Localize("Inventory:AdditionalGear", "Additional Gear"));
        var iconSize = new Vector2(37, 37) * ScaleFactor;
        foreach ((int idx, var entry) in _inv.Where(e => e.Value.IsGear))
        {
            ImGui.PushID(idx);
            if (entry.Item is not GearItem item || item.Item is null)
                continue;
            var icon = ServiceManager.IconCache[item.Item.Icon];
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
            ModalChild = new SelectGearItemWindow(i => _inv.Add(_inv.FirstFreeSlot(), i), i => { }, null, null, null, Common.Services.ServiceManager.GameInfo.CurrentExpansion.CurrentSavage.ArmorItemLevel);
        ImGui.EndDisabled();
    }
}
internal class SwapPositionWindow : HrtWindow
{
    private readonly RaidGroup _group;
    private readonly int _oldPos;
    private int _newPos;
    private readonly int[] possiblePositions;
    private readonly string[] possiblePositionNames;
    internal SwapPositionWindow(int pos, RaidGroup g) : base($"SwapPositionWindow{g.GetHashCode()}{pos}")
    {
        _group = g;
        _oldPos = pos;
        _newPos = 0;
        List<int> positions = Enumerable.Range(0, g.Count).ToList();
        positions.Remove(_oldPos);
        possiblePositions = positions.ToArray();
        possiblePositionNames = positions.ConvertAll(position => $"{_group[position].NickName} (#{position + 1})").ToArray();
        Size = new Vector2(170f, _group.Type == GroupType.Raid ? 230f : 150f);
        Title = $"{Localize("Swap Position of", "Swap Position of")} {_group[_oldPos].NickName}";
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
    }
    public override void Draw()
    {
        if (ImGuiHelper.SaveButton(Localize("Swap players positions", "Swap players positions")))
        {
            int newPos = possiblePositions[_newPos];
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
