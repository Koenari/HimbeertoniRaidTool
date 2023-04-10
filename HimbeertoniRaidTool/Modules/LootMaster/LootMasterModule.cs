using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.UI;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal sealed class LootMasterModule : IHrtModule<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
{
    //Interface Properties
    public string Name => "Loot Master";
    public string InternalName => "LootMaster";
    public HRTConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi> Configuration => _config;
    public string Description => "";
    public WindowSystem WindowSystem { get; }
    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>()
    {
        new()
        {
            Command = "/lootmaster",
            Description = Localize("/lootmaster", "Opens LootMaster Window (or /lm as short variant)"),
            ShowInHelp = true,
            OnCommand = OnCommand,
            ShouldExposeToDalamud = true,
        },
        new()
        {
            Command = "/lm",
            Description = Localize("/lootmaster", "Opens LootMaster Window (or /lm as short variant)"),
            ShowInHelp = false,
            OnCommand = OnCommand,
            ShouldExposeToDalamud = true,
        }
    };
    //Properties
    private static List<RaidGroup> RaidGroups => ServiceManager.HrtDataManager.Groups;
    private readonly LootmasterUI Ui;
    private readonly LootMasterConfiguration _config;
    private bool _fillSoloOnLogin = false;
    public LootMasterModule()
    {
        if (RaidGroups.Count == 0 || RaidGroups[0].Type != GroupType.Solo || !RaidGroups[0].Name.Equals("Solo"))
        {
            RaidGroups.Insert(0, new("Solo", GroupType.Solo));
            _fillSoloOnLogin = true;
        }

        _config = new(this);
        WindowSystem = new WindowSystem(InternalName);
        Ui = new(this);
        WindowSystem.AddWindow(Ui);
        ServiceManager.ClientState.Login += OnLogin;
    }
    public void AfterFullyLoaded()
    {
        if (ServiceManager.ClientState.IsLoggedIn)
            OnLogin(null, new());
        if (Configuration.Data.UpdateEtroBisOnStartup)
            ServiceManager.HrtDataManager.UpdateEtroSets(Configuration.Data.EtroUpdateIntervalDays);
    }
    public void OnLogin(object? sender, EventArgs e)
    {
        if (_fillSoloOnLogin)
            FillSoloChar(RaidGroups[0][0], true);
        _fillSoloOnLogin = false;
        if (_config.Data.OpenOnStartup)
            Ui.Show();
    }

    public void Update(Framework fw)
    {

    }
    private void FillSoloChar(Player p, bool useSelf = false)
    {
        PlayerCharacter? character = null;
        long contentID = 0;
        if (useSelf)
        {
            character = ServiceManager.ClientState.LocalPlayer;
            contentID = ServiceManager.CharacterInfoService.GetLocalPlayerContentId();
        }
        else
        {
            ServiceManager.CharacterInfoService.TryGetChar(out character, p.MainChar.Name, p.MainChar.HomeWorld);
            contentID = ServiceManager.CharacterInfoService.GetContentID(character);
        }

        if (character == null)
            return;
        var characterDB = ServiceManager.HrtDataManager.CharDB;
        ulong charID = Character.CalcCharID(contentID);
        Character? c = null;
        if (charID > 0)
            characterDB.TryGetCharacterByCharID(charID, out c);
        if (c == null)
            characterDB.SearchCharacter(character.HomeWorld.Id, character.Name.TextValue, out c);
        if (c is null)
        {
            c = new(character.Name.TextValue, character.HomeWorld.Id)
            {
                CharID = charID
            };
            if (!characterDB.TryAddCharacter(c))
                return;
        }
        p.NickName = c.Name.Split(' ')[0];
        p.MainChar = c;
        c.MainJob ??= character.GetJob();
        if (c.MainClass != null)
        {
            c.MainClass.Level = character.Level;
            var gearDB = ServiceManager.HrtDataManager.GearDB;
            if (!gearDB.TryGetSetByEtroID(_config.Data.GetDefaultBiS(c.MainClass.Job), out var etroSet))
            {
                etroSet = new(GearSetManager.Etro)
                {
                    EtroID = _config.Data.GetDefaultBiS(c.MainClass.Job)
                };
                gearDB.AddSet(etroSet);
            }
            c.MainClass.BIS = etroSet;
        }
        ServiceManager.HrtDataManager.Save();
    }
    internal void AddGroup(RaidGroup group, bool getGroupInfos)
    {
        RaidGroups.Add(group);
        if (!getGroupInfos)
            return;
        //Determine group type
        if (ServiceManager.PartyList.Length < 2)
            group.Type = GroupType.Solo;
        else if (ServiceManager.PartyList.Length < 5)
            group.Type = GroupType.Group;
        else
            group.Type = GroupType.Raid;
        //Get Infos
        if (group.Type == GroupType.Solo)
        {
            PlayerCharacter? target = ServiceManager.TargetManager.Target as PlayerCharacter;
            if (target is not null)
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
        List<PartyMember> players = new();
        List<PartyMember> fill = new();
        for (int i = 0; i < ServiceManager.PartyList.Length; i++)
        {
            var p = ServiceManager.PartyList[i];
            if (p != null)
                players.Add(p);
        }
        foreach (var p in players)
        {
            if (!Enum.TryParse(p.ClassJob.GameData?.Abbreviation.RawString, out Job c))
            {
                fill.Add(p);
                continue;
            }
            var r = c.GetRole();
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
        foreach (var pm in fill)
        {
            int pos = 0;
            while (group[pos].Filled) { pos++; }
            if (pos > 7) break;
            FillPosition(pos, pm);
        }
        void FillPosition(int pos, PartyMember pm)
        {
            var p = group[pos];
            p.NickName = pm.Name.TextValue.Split(' ')[0];
            if (!ServiceManager.HrtDataManager.CharDB.TryGetCharacterByCharID
                (Character.CalcCharID(pm.ContentId), out Character? character))
                ServiceManager.HrtDataManager.CharDB.SearchCharacter
                (pm.World.Id, pm.Name.TextValue, out character); ;
            if (character is null)
            {
                character = new(pm.Name.TextValue, pm.World.GameData?.RowId ?? 0);
                ServiceManager.HrtDataManager.CharDB.TryAddCharacter(character);
                bool canParseJob = Enum.TryParse(pm.ClassJob.GameData?.Abbreviation.RawString, out Job c);
                if (ServiceManager.CharacterInfoService.TryGetChar(out var pc, p.MainChar.Name, p.MainChar.HomeWorld) && canParseJob && c != Job.ADV)
                {
                    p.MainChar.MainJob = c;
                    p.MainChar.MainClass!.Level = pc!.Level;
                    GearSet BIS = new()
                    {
                        ManagedBy = GearSetManager.Etro,
                        EtroID = _config.Data.GetDefaultBiS(c)
                    };
                    ServiceManager.HrtDataManager.GearDB.AddSet(BIS);
                    p.MainChar.MainClass.BIS = BIS;
                }
            }
            p.MainChar = character!;
        }
        ServiceManager.HrtDataManager.Save();
    }
    public void OnCommand(string command, string args)
    {
        switch (args)
        {
            default:
                Ui.Show();
                break;
        }
    }
    public void Dispose()
    {
        Configuration.Data.LastGroupIndex = Ui._CurrenGroupIndex;
        Configuration.Save();
    }

    public void HandleMessage(HrtUiMessage message)
    {
        if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
            PluginLog.Warning(message.Message);
        else
            PluginLog.Information(message.Message);
        Ui.HandleMessage(message);
    }
}
