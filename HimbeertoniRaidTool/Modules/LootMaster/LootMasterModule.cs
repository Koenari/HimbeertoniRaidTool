﻿using System.Globalization;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using Character = HimbeertoniRaidTool.Common.Data.Character;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal sealed class LootMasterModule : IHrtModule
{
    private readonly LootmasterUi _ui;
    internal readonly LootMasterConfiguration ConfigImpl;
    private bool _fillSoloOnLogin;
    public LootMasterModule()
    {
        LootmasterLoc.Culture = new CultureInfo(ServiceManager.PluginInterface.UiLanguage);
        ConfigImpl = new LootMasterConfiguration(this);
        WindowSystem = new DalamudWindowSystem(new WindowSystem(InternalName));
        _ui = new LootmasterUi(this);
        WindowSystem.AddWindow(_ui);
        ServiceManager.ClientState.Login += OnLogin;
    }
    //Properties
    internal List<RaidGroup> RaidGroups => ConfigImpl.Data.RaidGroups;
    //Interface Properties
    public string Name => "Loot Master";
    public string InternalName => "LootMaster";
    public IHrtConfiguration Configuration => ConfigImpl;
    public string Description => "";
    public IWindowSystem WindowSystem { get; }

    public event Action? UiReady;
    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>
    {
        new("/lootmaster", OnCommand)
        {
            AltCommands = new List<string>
            {
                "/lm",
            },
            Description = LootmasterLoc.command_lootmaster,
            ShouldExposeToDalamud = true,
            ShouldExposeAltsToDalamud = true,
        },
    };
    public void AfterFullyLoaded()
    {
        if (RaidGroups.Count == 0 || RaidGroups[0].Type != GroupType.Solo)
        {

            var solo = new RaidGroup("Solo", GroupType.Solo)
            {
                TypeLocked = true,
            };
            if (ServiceManager.HrtDataManager.RaidGroupDb.TryAdd(solo))
            {
                ServiceManager.Logger.Info("Add solo group");
                RaidGroups.Insert(0, solo);
            }

        }
        if (!RaidGroups[0][0].Filled || !RaidGroups[0][0].MainChar.Filled)
            _fillSoloOnLogin = true;
        if (ServiceManager.ClientState.IsLoggedIn)
            OnLogin();
    }

    public void Update()
    {

    }
    public void OnLanguageChange(string langCode) => LootmasterLoc.Culture = new CultureInfo(langCode);
    public void PrintUsage(string command, string args)
    {
        SeStringBuilder stringBuilder = new SeStringBuilder()
                                        .AddUiForeground("[Himbeertoni Raid Tool]", 45)
                                        .AddUiForeground("[Help]", 62)
                                        .AddText(LootmasterLoc.chat_usage_heading)
                                        .Add(new NewLinePayload());

        stringBuilder
            .AddUiForeground("/lootmaster", 37)
            .AddText($" - {LootmasterLoc.command_show_helpText}")
            .Add(new NewLinePayload());
        stringBuilder
            .AddUiForeground("/lootmaster toggle", 37)
            .AddText($" - {LootmasterLoc.command_toggle_helpText}")
            .Add(new NewLinePayload());

        ServiceManager.Chat.Print(stringBuilder.BuiltString);
    }


    public void Dispose()
    {
        ConfigImpl.Data.LastGroupIndex = _ui.CurrentGroupIndex;
        ConfigImpl.Save(ServiceManager.HrtDataManager.ModuleConfigurationManager);
    }

    public void HandleMessage(HrtUiMessage message)
    {
        if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
            ServiceManager.Logger.Warning(message.Message);
        else
            ServiceManager.Logger.Information(message.Message);
        _ui.HandleMessage(message);
    }
    public void OnLogin()
    {
        if (_fillSoloOnLogin)
            FillPlayerFromSelf(RaidGroups[0][0]);
        _fillSoloOnLogin = false;
        if (ConfigImpl.Data.OpenOnStartup)
            _ui.Show();
        UiReady?.Invoke();
    }
    public bool FillPlayerFromTarget(Player player)
    {

        IGameObject? target = ServiceManager.TargetManager.Target;
        return target is IPlayerCharacter character && FillPlayer(player, character);
    }
    public bool FillPlayerFromSelf(Player player)
    {
        IPlayerCharacter? character = ServiceManager.ClientState.LocalPlayer;
        return character != null && FillPlayer(player, character);
    }
    private bool FillPlayer(Player player, IPlayerCharacter source)
    {
        if (player.LocalId.IsEmpty && !ServiceManager.HrtDataManager.PlayerDb.TryAdd(player)) return false;
        if (player.NickName.IsNullOrEmpty())
            player.NickName = source.Name.TextValue.Split(' ')[0];
        ulong contentId = ServiceManager.CharacterInfoService.GetContentId(source);
        var characterDb = ServiceManager.HrtDataManager.CharDb;
        ulong charId = Character.CalcCharId(contentId);
        if (!characterDb.Search(CharacterDb.GetStandardPredicate(charId, source.HomeWorld.Id, source.Name.TextValue),
                                out Character? c))
        {
            c = new Character(source.Name.TextValue, source.HomeWorld.Id)
            {
                CharId = charId,
            };
            if (!characterDb.TryAdd(c))
                return false;
        }
        player.MainChar = c;
        return FillCharacter(c, source);
    }

    private bool FillCharacter(Character destination, IPlayerCharacter source)
    {
        ServiceManager.Logger.Debug($"Filling Player for character: {source.Name}");
        Job curJob = source.GetJob();
        ServiceManager.Logger.Debug($"Found job: {curJob}");
        if (!curJob.IsCombatJob()) return false;
        bool isNew = destination[curJob] is null;
        PlayableClass curClass = destination[curJob] ?? destination.AddClass(curJob);
        if (isNew)
        {
            curClass.Level = source.Level;
            var gearDb = ServiceManager.HrtDataManager.GearDb;
            if (!gearDb.Search(
                    entry => entry?.ExternalId
                          == ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(curClass.Job),
                    out GearSet? etroSet))
            {
                etroSet = new GearSet(GearSetManager.Etro)
                {
                    ExternalId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(curClass.Job),
                };
                gearDb.TryAdd(etroSet);
                ServiceManager.ConnectorPool.EtroConnector.RequestGearSetUpdate(etroSet, HandleMessage);
            }
            curClass.CurBis = etroSet;
            gearDb.TryAdd(curClass.CurGear);
        }
        ServiceManager.HrtDataManager.Save();
        return true;
    }
    internal void AddGroup(RaidGroup group, bool getGroupInfos)
    {
        if (group.LocalId.IsEmpty)
            ServiceManager.HrtDataManager.RaidGroupDb.TryAdd(group);
        RaidGroups.Add(group);
        if (!getGroupInfos)
            return;
        group.Type = ServiceManager.PartyList.Length switch
        {
            //Determine group type
            < 2 => GroupType.Solo,
            < 5 => GroupType.Group,
            _   => GroupType.Raid,
        };
        //Get Infos
        if (group.Type == GroupType.Solo)
        {
            if (ServiceManager.TargetManager.Target is IPlayerCharacter target)
            {
                group[0].NickName = target.Name.TextValue;
                group[0].MainChar.Name = target.Name.TextValue;
                group[0].MainChar.HomeWorld = target.HomeWorld.GameData;
                FillPlayer(group[0], target);
            }
            else
                FillPlayerFromSelf(group[0]);
            return;
        }

        List<IPartyMember> fill = new();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        var players = ServiceManager.PartyList.Where(p => p != null).ToList();
        foreach (IPartyMember p in players)
        {
            if (!Enum.TryParse(p.ClassJob.GameData?.Abbreviation.RawString, out Job c))
            {
                fill.Add(p);
                continue;
            }
            Role r = c.GetRole();
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (r)
            {
                case Role.Tank:
                    if (!group[0].Filled)
                        FillPosition(0, p);
                    else if (!group[1].Filled && group.Type == GroupType.Raid)
                        FillPosition(1, p);
                    else
                        fill.Add(p);
                    break;
                case Role.Healer:
                    if (!group[2].Filled)
                        FillPosition(2, p);
                    else if (!group[3].Filled && group.Type == GroupType.Raid)
                        FillPosition(3, p);
                    else
                        fill.Add(p);
                    break;
                case Role.Melee:
                    if (!group[4].Filled)
                        FillPosition(4, p);
                    else if (!group[5].Filled && group.Type == GroupType.Raid)
                        FillPosition(5, p);
                    else
                        fill.Add(p);
                    break;
                case Role.Caster:
                    if (!group[6].Filled && group.Type == GroupType.Raid)
                        FillPosition(6, p);
                    else
                        fill.Add(p);
                    break;
                case Role.Ranged:
                    if (!group[7].Filled)
                        FillPosition(7, p);
                    else
                        fill.Add(p);
                    break;
                default:
                    fill.Add(p);
                    break;
            }
        }
        foreach (IPartyMember pm in fill)
        {
            int pos = 0;
            while (group[pos].Filled) { pos++; }
            if (pos > 7) break;
            FillPosition(pos, pm);
        }
        ServiceManager.HrtDataManager.Save();
        return;
        void FillPosition(int pos, IPartyMember pm)
        {
            Player p = group[pos];
            p.NickName = pm.Name.TextValue.Split(' ')[0];
            if (!ServiceManager.HrtDataManager.CharDb.Search(
                    CharacterDb.GetStandardPredicate(Character.CalcCharId((ulong)pm.ContentId), pm.World.Id,
                                                     pm.Name.TextValue), out Character? character))
            {
                character = new Character(pm.Name.TextValue, pm.World.GameData?.RowId ?? 0);
                ServiceManager.HrtDataManager.CharDb.TryAdd(character);
                bool canParseJob = Enum.TryParse(pm.ClassJob.GameData?.Abbreviation.RawString, out Job c);
                if (ServiceManager.CharacterInfoService.TryGetChar(out IPlayerCharacter? pc, p.MainChar.Name,
                                                                   p.MainChar.HomeWorld) && canParseJob && c != Job.ADV)
                {
                    p.MainChar.MainJob = c;
                    p.MainChar.MainClass!.Level = pc.Level;
                    GearSet bis = new(GearSetManager.Etro)
                    {
                        ExternalId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(c),
                    };
                    ServiceManager.HrtDataManager.GearDb.TryAdd(bis);
                    p.MainChar.MainClass.CurBis = bis;
                }
            }
            p.MainChar = character;
        }
    }

    public void OnCommand(string command, string args)
    {
        ServiceManager.Logger.Debug($"Lootmaster module handling command: {command} args: \"{args}\"");
        switch (args)
        {
            case "toggle":
                _ui.IsOpen = !_ui.IsOpen;
                break;
            case "help":
                PrintUsage("/help", "");
                break;
            default:
                _ui.Show();
                break;
        }
    }
}