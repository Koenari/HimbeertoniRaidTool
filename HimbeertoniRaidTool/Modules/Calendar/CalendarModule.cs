using System.Globalization;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Calendar.Ui;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Calendar;

public class CalendarModule : IHrtModule
{
    public IHrtConfiguration Configuration => ModuleConfigImpl;

    internal readonly CalendarModuleConfig ModuleConfigImpl;
    public string Name => CalendarLoc.Module_Name;

    public string InternalName => "Calendar";

    public string Description => CalendarLoc.Module_Description;

    public IWindowSystem WindowSystem { get; }

    private readonly CalendarUi _calendarUi;

    private List<RaidSession> _sessionCache = new();

    private IEnumerable<RaidSession> _sessions => _sessionCache;

    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>()
    {
        new()
        {
            Command = "/calendar",
            Description = CalendarLoc.Commands_calenadr_helpText,
            OnCommand = OnCommand,
            ShowInHelp = true,
            ShouldExposeToDalamud = true,

        },
    };

    public event Action? UiReady;

    public CalendarModule()
    {
        ModuleConfigImpl = new CalendarModuleConfig(this);
        WindowSystem = new DalamudWindowSystem(new WindowSystem(InternalName));
        _calendarUi = new CalendarUi(this);
        WindowSystem.AddWindow(_calendarUi);
        ServiceManager.ClientState.Login += OnLogin;
    }
    /// <summary>
    /// Gets all raid sessions in the specified time frame
    /// </summary>
    /// <param name="from">If set marks the start of the time frame</param>
    /// <param name="until">End of time frame. defaults to 24 hours after from</param>
    /// <returns>A list of raid sessions sorted by start time</returns>
    public IEnumerable<RaidSession> GetRaidSessions(DateTime? from = null, DateTime? until = null)
    {
        if (from is null) return _sessions;
        until ??= from.Value.AddDays(1);
        return _sessions.Where(s => s.StartTime >= from && s.StartTime < until);
    }


    public void OnLogin() => UiReady?.Invoke();
    public void AfterFullyLoaded() => _calendarUi.Show(); //Todo: Remove after testing

    public void OnLanguageChange(string langCode) => CalendarLoc.Culture = new CultureInfo(langCode);
    public void Dispose() { }

    public void HandleMessage(HrtUiMessage message)
    {
        switch (message.MessageType)
        {
            case HrtUiMessageType.Error or HrtUiMessageType.Failure:
                ServiceManager.Logger.Error(message.Message);
                break;
            default:
                ServiceManager.Logger.Information(message.Message);
                break;
        }
    }

    public void Update()
    {
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