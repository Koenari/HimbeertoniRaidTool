using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal sealed class LootMasterModule : IHrtModule<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
{
    //Interface Properties
    public string Name => "Loot Master";
    public string InternalName => "LootMaster";
    public HrtConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi> Configuration => _config;
    public string Description => "";
    public WindowSystem WindowSystem { get; }
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
    private static List<RaidGroup> RaidGroups => ServiceManager.HrtDataManager.Groups;
    private readonly LootmasterUi _ui;
    private readonly LootMasterConfiguration _config;
    private bool _fillSoloOnLogin;
    public LootMasterModule()
    {
        if (RaidGroups.Count == 0 || RaidGroups[0].Type != GroupType.Solo)
        {
            RaidGroups.Insert(0, new RaidGroup("Solo", GroupType.Solo)
            {
                TypeLocked = true,
            });
            _fillSoloOnLogin = true;
        }

        _config = new LootMasterConfiguration(this);
        WindowSystem = new WindowSystem(InternalName);
        _ui = new LootmasterUi(this);
        WindowSystem.AddWindow(_ui);
        ServiceManager.ClientState.Login += OnLogin;
    }
    public void AfterFullyLoaded()
    {
        if (ServiceManager.ClientState.IsLoggedIn)
            OnLogin();
        ServiceManager.HrtDataManager.GearDb.UpdateEtroSets(Configuration.Data.UpdateEtroBisOnStartup,
            Configuration.Data.EtroUpdateIntervalDays);
    }
    public void OnLogin()
    {
        if (_fillSoloOnLogin)
            FillSoloChar(RaidGroups[0][0], true);
        _fillSoloOnLogin = false;
        if (_config.Data.OpenOnStartup)
            _ui.Show();
    }

    public void Update()
    {

    }
    private static void FillSoloChar(Player p, bool useSelf = false)
    {
        PlayerCharacter? character;
        long contentId;
        if (useSelf)
        {
            character = ServiceManager.ClientState.LocalPlayer;
            contentId = ServiceManager.CharacterInfoService.GetLocalPlayerContentId();
        }
        else
        {
            ServiceManager.CharacterInfoService.TryGetChar(out character, p.MainChar.Name, p.MainChar.HomeWorld);
            contentId = ServiceManager.CharacterInfoService.GetContentId(character);
        }

        if (character == null)
            return;
        CharacterDb characterDb = ServiceManager.HrtDataManager.CharDb;
        ulong charId = Character.CalcCharId(contentId);
        Character? c = null;
        if (charId > 0)
            characterDb.TryGetCharacterByCharId(charId, out c);
        if (c == null)
            characterDb.SearchCharacter(character.HomeWorld.Id, character.Name.TextValue, out c);
        if (c is null)
        {
            c = new Character(character.Name.TextValue, character.HomeWorld.Id)
            {
                CharId = charId,
            };
            if (!characterDb.TryAddCharacter(c))
                return;
        }
        p.NickName = c.Name.Split(' ')[0];
        p.MainChar = c;
        c.MainJob ??= character.GetJob();
        if (c.MainClass != null)
        {
            c.MainClass.Level = character.Level;
            GearDb gearDb = ServiceManager.HrtDataManager.GearDb;
            if (!gearDb.TryGetSetByEtroId(ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(c.MainClass.Job), out GearSet? etroSet))
            {
                etroSet = new GearSet(GearSetManager.Etro)
                {
                    EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(c.MainClass.Job),
                };
                gearDb.AddSet(etroSet);
            }
            c.MainClass.Bis = etroSet;
        }
        ServiceManager.HrtDataManager.Save();
    }
    internal static void AddGroup(RaidGroup group, bool getGroupInfos)
    {
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
                FillSoloChar(group[0]);
            }
            else
                FillSoloChar(group[0], true);
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
                    (Character.CalcCharId(pm.ContentId), out Character? character))
                ServiceManager.HrtDataManager.CharDb.SearchCharacter
                    (pm.World.Id, pm.Name.TextValue, out character);
            if (character is null)
            {
                character = new Character(pm.Name.TextValue, pm.World.GameData?.RowId ?? 0);
                ServiceManager.HrtDataManager.CharDb.TryAddCharacter(character);
                bool canParseJob = Enum.TryParse(pm.ClassJob.GameData?.Abbreviation.RawString, out Job c);
                if (ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? pc, p.MainChar.Name, p.MainChar.HomeWorld) && canParseJob && c != Job.ADV)
                {
                    p.MainChar.MainJob = c;
                    p.MainChar.MainClass!.Level = pc.Level;
                    GearSet bis = new()
                    {
                        ManagedBy = GearSetManager.Etro,
                        EtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(c),
                    };
                    ServiceManager.HrtDataManager.GearDb.AddSet(bis);
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
        Configuration.Data.LastGroupIndex = _ui.CurrentGroupIndex;
        Configuration.Save();
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