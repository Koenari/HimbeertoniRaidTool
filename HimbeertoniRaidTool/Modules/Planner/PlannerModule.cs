using System.Globalization;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Planner.Ui;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Planner;

public class PlannerModule : IHrtModule
{
    public IHrtConfiguration Configuration => ModuleConfigImpl;

    internal readonly PlannerModuleConfig ModuleConfigImpl;
    public string Name => PlannerLoc.Module_Name;

    public const string INTERNAL_NAME = "Planner";

    public string InternalName => INTERNAL_NAME;

    public string Description => PlannerLoc.Module_Description;

    public RaidSession? ActiveSession { get; private set; }
    private RaidSession? _nextSession = null;

    private readonly CalendarUi _calendarUi;

    private List<RaidSession> _sessionCache = new();

    private IEnumerable<RaidSession> _sessions => Services.HrtDataManager.RaidSessionDb.GetValues();

    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>
    {
        new("/planner", OnCommand)
        {
            Description = PlannerLoc.Commands_calenadr_helpText,
            ShowInHelp = true,
            ShouldExposeToDalamud = true,

        },
    };
    public IModuleServiceContainer Services { get; }

    public event Action? UiReady;

    public PlannerModule()
    {
        Services = ServiceManager.GetServiceContainer(this);
        PlannerLoc.Culture = Services.LocalizationManager.CurrentLocale;
        ModuleConfigImpl = new PlannerModuleConfig(this);
        _calendarUi = new CalendarUi(this);
        Services.UiSystem.AddWindow(_calendarUi);
#if DEBUG
        _calendarUi.Show();
#endif
        Services.ClientState.Login += OnLogin;
        Services.Framework.Update += Update;
        UpdateNextSession();
    }

    private void UpdateNextSession()
    {
        foreach (var session in Services.HrtDataManager.RaidSessionDb.GetValues())
        {
            if (session.StartTime < DateTime.Now) continue;
            if (_nextSession == null || session.StartTime < _nextSession.StartTime)
                _nextSession = session;
        }
    }

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
        if (ActiveSession != null || _nextSession == null) return;
        ActiveSession = _nextSession;
        UpdateNextSession();
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