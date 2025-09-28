using System.Globalization;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Planner.Ui;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Planner;

internal class PlannerModule : IHrtModule<PlannerModule, PlannerModuleConfig>
{
    #region Static

    public static string Name => PlannerLoc.Module_Name;

    public static string InternalName => "Planner";

    public static string Description => PlannerLoc.Module_Description;

    public static bool CanBeDisabled => true;

    #endregion
    public PlannerModuleConfig Configuration { get; }

    public RaidSession? ActiveSession { get; private set; }

    private readonly CalendarUi _calendarUi;

    private IEnumerable<RaidSession> _sessions => Services.HrtDataManager.RaidSessionDb.GetValues();

    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>
    {
        new("/planner", OnCommand)
        {
            AltCommands = new List<string>
            {
                "/calendar",
                "/cal",
            },
            Description = PlannerLoc.Commands_calenadr_helpText,
            ShowInHelp = true,
            ShouldExposeToDalamud = true,

        },
    };
    public IModuleServiceContainer Services { get; }

    public event Action? UiReady;

    private PlannerModule(IModuleServiceContainer services)
    {
        Services = services;
        PlannerLoc.Culture = Services.LocalizationManager.CurrentLocale;
        Configuration = new PlannerModuleConfig(this);
        _calendarUi = new CalendarUi(this);
        Services.UiSystem.AddWindow(_calendarUi);
#if DEBUG
        _calendarUi.Show();
#endif
        Services.ClientState.Login += OnLogin;
        Services.Framework.Update += Update;
    }

    public static PlannerModule Create(IModuleServiceContainer services) => new(services);

    /// <summary>
    /// Gets all raid sessions in the specified time frame
    /// </summary>
    /// <param name="from">If set marks the start of the time frame</param>
    /// <param name="until">End of time frame. Defaults to 24 hours after from</param>
    /// <returns>A list of raid sessions sorted by start time</returns>
    public IEnumerable<RaidSession> GetRaidSessions(DateTime? from = null, DateTime? until = null)
    {
        if (from is null) return _sessions;
        until ??= from.Value.AddDays(1);
        return _sessions.Where(s => s.StartTime >= from && s.StartTime < until);
    }

    public void CreateActiveRaidSession(Reference<RaidGroup>? group = null, Action<RaidSession>? onCreated = null)
    {
        var localChar = Services.ClientState.LocalPlayer;
        Character? self = null;
        if (localChar is not null)
        {
            var searchPred = CharacterDb.GetStandardPredicate(
                Character.CalcCharId(Services.ClientState.LocalContentId), localChar.HomeWorld.RowId,
                localChar.Name.TextValue);
            Services.HrtDataManager.CharDb.Search(searchPred, out self);
        }
        var raidSession = new RaidSession(DateTime.Now, TimeSpan.FromHours(1), self);
        if (group is not null)
        {
            raidSession.Group = group.Data;
            foreach (var player in group.Data)
            {
                if (!raidSession.Invite(player.MainChar, out var participant)) continue;
                participant.InvitationStatus = InviteStatus.Confirmed;
                participant.ParticipationStatus = ParticipationStatus.Present;
            }
        }
        else
        {
            FillRaidSessionFromCurrentGroup(raidSession);
        }
        Services.UiSystem.EditWindows.Create(raidSession, onCreated);
    }

    private void FillRaidSessionFromCurrentGroup(RaidSession session)
    {
        var charDb = Services.HrtDataManager.CharDb;
        foreach (var partyMember in Services.PartyList)
        {
            var searchPred = CharacterDb.GetStandardPredicate(Character.CalcCharId(partyMember.ContentId),
                                                              partyMember.World.RowId, partyMember.Name.TextValue);
            if (!charDb.Search(searchPred, out var partyMemberChar)) continue;
            if (!session.Invite(partyMemberChar, out var participant)) continue;
            participant.InvitationStatus = InviteStatus.Confirmed;
            participant.ParticipationStatus = ParticipationStatus.Present;
        }
    }


    public void OnLogin() => UiReady?.Invoke();
    public void AfterFullyLoaded() => _calendarUi.Show(); //Todo: Remove after testing

    public void OnLanguageChange(CultureInfo culture) => PlannerLoc.Culture = culture;
    public void Dispose() { }

    public void HandleMessage(HrtUiMessage message)
    {
        switch (message.MessageType)
        {
            case HrtUiMessageType.Error or HrtUiMessageType.Failure:
                Services.Logger.Error(message.Message);
                break;
            default:
                Services.Logger.Information(message.Message);
                break;
        }
    }

    public void Update(IFramework _)
    {
        if (ActiveSession != null && ActiveSession.EndTime < DateTime.Now)
            ActiveSession = null;
        ActiveSession ??= _sessions.FirstOrDefault(s => s?.StartTime < DateTime.Now && s.EndTime > DateTime.Now, null);
    }
    public void OnCommand(string command, string args)
    {
        switch (args)
        {
            case "toggle":
                _calendarUi.IsOpen = !_calendarUi.IsOpen;
                break;
            default:
                _calendarUi.Show();
                break;
        }
    }

    public void PrintUsage(string command, string args) => throw new NotImplementedException();
}