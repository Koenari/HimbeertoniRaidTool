using System.Globalization;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using Character = HimbeertoniRaidTool.Common.Data.Character;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class LootMasterModule : IHrtModule<LootMasterModule, LootMasterConfiguration>
{
    #region Static

    public static string Name => "Loot Master";
    public static string InternalName => "LootMaster";

    public static string Description => "";

    public static bool CanBeDisabled => false;

    #endregion

    private readonly LootmasterUi _ui;
    private LootMasterModule(IModuleServiceContainer services)
    {
        Services = services;
        LootmasterLoc.Culture = Services.LocalizationManager.CurrentLocale;
        Configuration = new LootMasterConfiguration(this);
        _ui = new LootmasterUi(this);
        Services.ClientState.Login += OnLogin;

    }
    public static LootMasterModule Create(IModuleServiceContainer services) => new(services);

    //Properties
    internal List<RaidGroup> RaidGroups => Configuration.Data.RaidGroups;
    //Interface Properties
    public LootMasterConfiguration Configuration { get; }

    public IModuleServiceContainer Services { get; }
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
            if (Services.HrtDataManager.GetTable<RaidGroup>().TryAdd(solo))
            {
                Services.Logger.Information("Add solo group");
                RaidGroups.Insert(0, solo);
            }

        }
        if (Services.ClientState.IsLoggedIn)
            Services.TaskManager.RunOnFrameworkThread(OnLogin);
    }

    public void ShowUi() => _ui.Show();

    public void OnLanguageChange(CultureInfo culture) => LootmasterLoc.Culture = culture;
    public void PrintUsage(string command, string args)
    {
        var stringBuilder = new SeStringBuilder()
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

        Services.Chat.Print(stringBuilder.BuiltString);
    }


    public void Dispose() => Configuration.Save(Services.HrtDataManager.ModuleConfigurationManager);

    public void HandleMessage(HrtUiMessage message)
    {
        if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
            Services.Logger.Warning(message.Message);
        else
            Services.Logger.Information(message.Message);
        _ui.HandleMessage(message);
    }
    private void OnLogin()
    {
        var soloPlayer = RaidGroups[0][0];
        if (!soloPlayer.Filled || !soloPlayer.Characters.Any())
            FillPlayerFromSelf(soloPlayer);
        ulong curCharId =
            Character.CalcCharId(
                Services.CharacterInfoService.GetContentId(Services.ClientState.LocalPlayer));
        Services.Logger.Debug("OnLogin: CurCharID: {CurCharId}", curCharId);
        if (curCharId != 0)
        {
            Services.Logger.Information("Switching Solo Char");
            if (soloPlayer.Characters.Any(c => c.CharId == curCharId))
                soloPlayer.MainChar = soloPlayer.Characters.First(c => c.CharId == curCharId);
            else
                AddCurrentCharacter(soloPlayer);
        }

        if (Configuration.Data.OpenOnStartup)
            _ui.Show();
        UiReady?.Invoke();
    }
    public bool FillPlayerFromTarget(Player player)
    {

        var target = Services.TargetManager.Target;
        return target is IPlayerCharacter character && FillPlayer(player, character);
    }
    private void FillPlayerFromSelf(Player player)
    {
        var character = Services.ClientState.LocalPlayer;
        if (character != null)
            FillPlayer(player, character);
    }
    private bool FillPlayer(Player player, IPlayerCharacter source)
    {
        if (player.LocalId.IsEmpty && !Services.HrtDataManager.GetTable<Player>().TryAdd(player)) return false;
        if (player.NickName.IsNullOrEmpty())
            player.NickName = source.Name.TextValue.Split(' ')[0];
        var c = new Character();
        bool result = FillCharacter(ref c, source);
        player.MainChar = c;
        return result;
    }

    private void AddCurrentCharacter(Player player)
    {
        var sourceCharacter = Services.ClientState.LocalPlayer;
        var character = new Character();
        if (sourceCharacter == null) return;
        FillCharacter(ref character, sourceCharacter);
        player.MainChar = character;
    }
    private bool FillCharacter(ref Character destination, IPlayerCharacter source)
    {
        Services.Logger.Debug("Filling character: {SourceName}", source.Name);
        ulong charId = Character.CalcCharId(Services.CharacterInfoService.GetContentId(source));
        if (Services.HrtDataManager.GetTable<Character>().Search(
                CharacterDb.GetStandardPredicate(charId, source.HomeWorld.RowId, source.Name.TextValue),
                out var dbChar))
        {
            destination = dbChar;
        }
        else
        {
            destination.HomeWorldId = source.HomeWorld.RowId;
            destination.Name = source.Name.TextValue;
            destination.CharId = charId;
            if (!Services.HrtDataManager.GetTable<Character>().TryAdd(destination)) return false;
        }
        var curJob = source.GetJob();
        Services.Logger.Debug("Found job: {CurJob}", curJob);
        if (!curJob.IsCombatJob()) return true;
        bool isNewJob = destination[curJob] is null;
        var curClass = destination[curJob] ?? destination.AddClass(curJob);
        if (isNewJob)
        {
            curClass.Level = source.Level;
            var gearDb = Services.HrtDataManager.GetTable<GearSet>();
            var defaultBis = Services.ConnectorPool.GetDefaultBiS(curClass.Job);
            if (!gearDb.Search(defaultBis.Equals, out var bisSet))
            {

                bisSet = defaultBis.ToGearSet();
                gearDb.TryAdd(bisSet);
                if (Services.ConnectorPool.TryGetConnector(defaultBis.Service, out var connector))
                    connector.RequestGearSetUpdate(bisSet, HandleMessage);
            }
            curClass.CurBis = bisSet;
            gearDb.TryAdd(curClass.CurGear);
        }
        Services.HrtDataManager.Save();
        return true;
    }
    internal void AddGroup(RaidGroup group) => AddGroup(group, false);

    internal void AddGroup(RaidGroup group, bool getGroupInfos)
    {
        if (group.LocalId.IsEmpty)
            Services.HrtDataManager.GetTable<RaidGroup>().TryAdd(group);
        RaidGroups.Add(group);
        if (!getGroupInfos)
            return;
        group.Type = Services.PartyList.Length switch
        {
            //Determine group type
            < 2 => GroupType.Solo,
            < 5 => GroupType.Group,
            _   => GroupType.Raid,
        };
        //Get Infos
        if (group.Type == GroupType.Solo)
        {
            if (Services.TargetManager.Target is IPlayerCharacter target)
            {
                group[0].NickName = target.Name.TextValue;
                group[0].MainChar.Name = target.Name.TextValue;
                group[0].MainChar.HomeWorld = target.HomeWorld.Value;
                FillPlayer(group[0], target);
            }
            else
                FillPlayerFromSelf(group[0]);
            return;
        }

        List<IPartyMember> fill = new();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        var players = Services.PartyList.Where(p => p != null).ToList();
        foreach (var p in players)
        {
            if (!Enum.TryParse(p.ClassJob.Value.Abbreviation.ExtractText(), out Job c))
            {
                fill.Add(p);
                continue;
            }
            var r = c.GetRole();
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
        foreach (var pm in fill)
        {
            int pos = 0;
            while (group[pos].Filled) { pos++; }
            if (pos > 7) break;
            FillPosition(pos, pm);
        }
        Services.HrtDataManager.Save();
        return;
        void FillPosition(int pos, IPartyMember pm)
        {
            var p = group[pos];
            p.NickName = pm.Name.TextValue.Split(' ')[0];
            if (!Services.HrtDataManager.GetTable<Character>().Search(
                    CharacterDb.GetStandardPredicate(Character.CalcCharId((ulong)pm.ContentId), pm.World.RowId,
                                                     pm.Name.TextValue), out var character))
            {
                character = new Character(pm.Name.TextValue, pm.World.Value.RowId);
                Services.HrtDataManager.GetTable<Character>().TryAdd(character);
                bool canParseJob = Enum.TryParse(pm.ClassJob.Value.Abbreviation.ExtractText(), out Job c);
                if (Services.CharacterInfoService.TryGetChar(out var pc, p.MainChar.Name,
                                                             p.MainChar.HomeWorld) && canParseJob && c != Job.ADV)
                {
                    p.MainChar.MainJob = c;
                    p.MainChar.MainClass!.Level = pc.Level;
                    var defaultBis = Services.ConnectorPool.GetDefaultBiS(c);
                    GearSet bis = new(defaultBis.Service)
                    {
                        ExternalId = defaultBis.Id,
                    };
                    Services.HrtDataManager.GetTable<GearSet>().TryAdd(bis);
                    p.MainChar.MainClass.CurBis = bis;
                }
            }
            p.MainChar = character;
        }
    }

    public void OnCommand(string command, string args)
    {
        Services.Logger.Debug("Lootmaster module handling command: {Command} args: \"{Args}\"", command, args);
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