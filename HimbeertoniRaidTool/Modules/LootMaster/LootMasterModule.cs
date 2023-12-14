using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using static HimbeertoniRaidTool.Plugin.Services.Localization;
using Character = HimbeertoniRaidTool.Common.Data.Character;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal sealed class LootMasterModule : IHrtModule
{
    //Interface Properties
    public string Name => "Loot Master";
    public string InternalName => "LootMaster";
    public IHrtConfiguration Configuration => ConfigImpl;
    public string Description => "";
    public WindowSystem WindowSystem { get; }

    public event Action? UiReady; 
    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>()
    {
        new()
        {
            // ReSharper disable once StringLiteralTypo
            Command = "/lootmaster",
            AltCommands = new List<string>
            {
                "/lm",
            },
            // ReSharper disable once StringLiteralTypo
            Description = Localize("/lootmaster", "Opens LootMaster Window (or /lm as short variant)"),
            ShowInHelp = true,
            OnCommand = OnCommand,
            ShouldExposeToDalamud = true,
            ShouldExposeAltsToDalamud = true,
        },
    };
    //Properties
    internal List<RaidGroup> RaidGroups => ConfigImpl.Data.RaidGroups;
    private readonly LootmasterUi _ui;
    internal readonly LootMasterConfiguration ConfigImpl;
    private bool _fillSoloOnLogin;
    public LootMasterModule()
    {
        ConfigImpl = new LootMasterConfiguration(this);
        WindowSystem = new WindowSystem(InternalName);
        _ui = new LootmasterUi(this);
        WindowSystem.AddWindow(_ui);
        ServiceManager.ClientState.Login += OnLogin;
    }
    public void AfterFullyLoaded()
    {
        if (RaidGroups.Count == 0 || RaidGroups[0].Type != GroupType.Solo)
        {
            
            var solo =  new RaidGroup("Solo", GroupType.Solo)
            {
                TypeLocked = true,
            };
            if(ServiceManager.HrtDataManager.RaidGroupDb.TryAdd(solo))
            {
                ServiceManager.PluginLog.Info("Add solo group");
                RaidGroups.Insert(0,solo);
            }
            
        }
        if(!RaidGroups[0][0].Filled || !RaidGroups[0][0].MainChar.Filled)
            _fillSoloOnLogin = true;
        if (ServiceManager.ClientState.IsLoggedIn)
            OnLogin();
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

    public void Update()
    {

    }
    public bool FillPlayerFromTarget(Player player)
    {

        GameObject? target = ServiceManager.TargetManager.Target;
        return target is PlayerCharacter character && FillPlayer(player,character);
    }
    public bool FillPlayerFromSelf(Player player)
    {
        PlayerCharacter? character = ServiceManager.ClientState.LocalPlayer;
        return character != null && FillPlayer(player,character);
    }
    private bool FillPlayer(Player player, PlayerCharacter source)
    {
        if (player.LocalId.IsEmpty && !ServiceManager.HrtDataManager.PlayerDb.TryAdd(player)) return false;
        if(player.NickName.IsNullOrEmpty())
            player.NickName = source.Name.TextValue.Split(' ')[0];
        long contentId = ServiceManager.CharacterInfoService.GetContentId(source);
        CharacterDb characterDb = ServiceManager.HrtDataManager.CharDb;
        ulong charId = Character.CalcCharId((ulong)contentId);
        Character? c = null;
        if (charId > 0)
            characterDb.TryGetCharacterByCharId(charId, out c);
        if (c == null)
            characterDb.SearchCharacter(source.HomeWorld.Id, source.Name.TextValue, out c);
        if (c is null)
        {
            c = new Character(source.Name.TextValue, source.HomeWorld.Id)
            {
                CharId = charId,
            };
            if (!characterDb.TryAdd(c))
                return false;
        }
        player.MainChar = c;
        FillCharacter(c,source);
        return true;
    }

    private void FillCharacter(Character destination, PlayerCharacter source)
    {
        Job curJob = source.GetJob();
        bool isNew = destination[curJob] is null;
        PlayableClass curClass = destination[curJob] ?? destination.AddClass(curJob);
        if (isNew)
        {
            curClass.Level = source.Level;
            GearDb gearDb = ServiceManager.HrtDataManager.GearDb;
            if (!gearDb.TryGetSetByEtroId(ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(curClass.Job), out GearSet? etroSet))
            {
                etroSet = new GearSet(GearSetManager.Etro)
                {
                    EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(curClass.Job),
                };
                gearDb.TryAdd(etroSet);
                ServiceManager.TaskManager.RegisterTask(new HrtTask(() => ServiceManager.ConnectorPool.EtroConnector.GetGearSet(etroSet), HandleMessage,"GetBiS"));
            }
            curClass.Bis = etroSet;
            gearDb.TryAdd(curClass.Gear);
        }
        ServiceManager.HrtDataManager.Save();
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
            _ => GroupType.Raid,
        };
        //Get Infos
        if (group.Type == GroupType.Solo)
        {
            if (ServiceManager.TargetManager.Target is PlayerCharacter target)
            {
                group[0].NickName = target.Name.TextValue;
                group[0].MainChar.Name = target.Name.TextValue;
                group[0].MainChar.HomeWorld = target.HomeWorld.GameData;
                FillPlayer(group[0],target);
            }
            else
                FillPlayerFromSelf(group[0]);
            return;
        }

        List<PartyMember> fill = new();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        var players = ServiceManager.PartyList.Where(p => p != null).ToList();
        foreach (PartyMember p in players)
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
        foreach (PartyMember pm in fill)
        {
            int pos = 0;
            while (group[pos].Filled) { pos++; }
            if (pos > 7) break;
            FillPosition(pos, pm);
        }
        void FillPosition(int pos, PartyMember pm)
        {
            Player p = group[pos];
            p.NickName = pm.Name.TextValue.Split(' ')[0];
            if (!ServiceManager.HrtDataManager.CharDb.TryGetCharacterByCharId
                    (Character.CalcCharId((ulong)pm.ContentId), out Character? character))
                ServiceManager.HrtDataManager.CharDb.SearchCharacter
                    (pm.World.Id, pm.Name.TextValue, out character);
            if (character is null)
            {
                character = new Character(pm.Name.TextValue, pm.World.GameData?.RowId ?? 0);
                ServiceManager.HrtDataManager.CharDb.TryAdd(character);
                bool canParseJob = Enum.TryParse(pm.ClassJob.GameData?.Abbreviation.RawString, out Job c);
                if (ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? pc, p.MainChar.Name, p.MainChar.HomeWorld) && canParseJob && c != Job.ADV)
                {
                    p.MainChar.MainJob = c;
                    p.MainChar.MainClass!.Level = pc.Level;
                    GearSet bis = new(GearSetManager.Etro)
                    {
                        EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(c),
                    };
                    ServiceManager.HrtDataManager.GearDb.TryAdd(bis);
                    p.MainChar.MainClass.Bis = bis;
                }
            }
            p.MainChar = character;
        }
        ServiceManager.HrtDataManager.Save();
    }
    
    public void OnCommand(string command, string args)
    {
        switch (args)
        {
            case "toggle":
                _ui.IsOpen = !_ui.IsOpen;
                break;
            default:
                _ui.Show();
                break;
        }
    }
    public void Dispose()
    {
        ConfigImpl.Data.LastGroupIndex = _ui.CurrentGroupIndex;
        ConfigImpl.Save(ServiceManager.HrtDataManager.ModuleConfigurationManager);
    }

    public void HandleMessage(HrtUiMessage message)
    {
        if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
            ServiceManager.PluginLog.Warning(message.Message);
        else
            ServiceManager.PluginLog.Information(message.Message);
        _ui.HandleMessage(message);
    }
}