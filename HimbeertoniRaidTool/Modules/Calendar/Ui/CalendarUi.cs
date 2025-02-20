using System.Numerics;
using HimbeertoniRaidTool.Common.Localization;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.Calendar.Ui;

internal class CalendarUi : HrtWindow
{
    private readonly CalendarModule _module;
    private int _year;
    private int _inputYear;
    private bool _inputHasChanged = false;
    private DateTime _inputLastChanged;

    private Month _month;

    private Vector2 _daySize = new(200, 100);
    public Vector2 DaySize => _daySize;

    public DayOfWeek FirstDayOfWeek => _module.ModuleConfigImpl.Data.FirstDayOfWeek;

    public CalendarUi(CalendarModule module) : base(module.Services.UiSystem, "##calendarUi")
    {
        _module = module;
        Title = CalendarLoc.CalendarUi_Title;
        var now = DateTime.Now;
        _year = now.Year;
        _inputYear = now.Year;
        _month = (Month)now.Month;
        _inputLastChanged = now;
        (Size, SizeCondition) = (ImGui.GetIO().DisplaySize / 3, ImGuiCond.Appearing); //Todo: change to first time
    }

    public override void Draw()
    {
        _daySize.X = ImGui.GetWindowSize().X / 8f;
        DrawHeader();
        ProcessInput();
        ImGui.NewLine();
        DrawCalendar();
    }

    private void ProcessInput()
    {
        if (!_inputHasChanged)
            return;
        if (!IsValidYear(_inputYear) && _inputLastChanged + TimeSpan.FromSeconds(1) > DateTime.Now) return;
        _year = ToValidYear(_inputYear);
        _inputYear = _year;
        _inputHasChanged = false;

        static bool IsValidYear(int year)
        {
            return year is >= 1970 and <= 3000;
        }
        static int ToValidYear(int year)
        {
            return Math.Clamp(year, 1970, 3000);
        }
    }

    private void DrawHeader()
    {
        //Right
        ImGui.SetNextItemWidth(ScaleFactor * 200);
        ImGuiHelper.Combo(CommonLoc.Month, ref _month);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ScaleFactor * 200);
        if (ImGui.InputInt(CommonLoc.Year, ref _inputYear))
        {
            _inputLastChanged = DateTime.Now;
            _inputHasChanged = true;
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(CalendarLoc.ui_btn_today, CalendarLoc.ui_btn_tt_today))
        {
            var now = DateTime.Now;
            _year = now.Year;
            _month = (Month)now.Month;
        }
    }

    private void DrawCalendar()
    {
        if (!ImGui.BeginTable("##calendar", 7, ImGuiTableFlags.Borders)) return;
        DateOnly day = new(_year, (int)_month, 1);
        ImGui.TableSetupColumn(Weekday.Monday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Tuesday.Name(), ImGuiTableColumnFlags.WidthFixed,
                               DaySize.X);
        ImGui.TableSetupColumn(Weekday.Wednesday.Name(), ImGuiTableColumnFlags.WidthFixed,
                               DaySize.X);
        ImGui.TableSetupColumn(Weekday.Thursday.Name(), ImGuiTableColumnFlags.WidthFixed,
                               DaySize.X);
        ImGui.TableSetupColumn(Weekday.Friday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Saturday.Name(), ImGuiTableColumnFlags.WidthFixed,
                               DaySize.X);
        ImGui.TableSetupColumn(Weekday.Sunday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableHeadersRow();
        //Backtrack to first day of a week
        while (day.DayOfWeek != FirstDayOfWeek)
        {
            day = day.AddDays(-1);
        }
        var nextMonth = new DateOnly(_year, (int)_month, 1).AddMonths(1
        );
        //draw entries for month and adjacent days
        while (day < nextMonth || day.DayOfWeek != FirstDayOfWeek)
        {
            ImGui.TableNextColumn();
            DrawDay(day, _module.GetRaidSessions(day.ToDateTime(TimeOnly.MinValue)));
            day = day.AddDays(1);
        }

        ImGui.EndTable();
    }

    private void DrawDay(DateOnly date, IEnumerable<RaidSession> entries)
    {
        ImGui.BeginGroup();
        ImGui.BeginDisabled(date.Month != (int)_month);
        ImGui.Text($"{date.Day}");
        bool hasDrawn = false;

        foreach (var calendarEntry in entries)
        {
            hasDrawn = true;
            ImGui.Text(
                $"{calendarEntry.Name} ({calendarEntry.Participants.Count(e => e.InvitationStatus == InviteStatus.Accepted)}/{calendarEntry.Participants.Count()})");
        }

        if (!hasDrawn) ImGui.Text(CalendarLoc.UI_Calendar_Day_Nothing);
        ImGui.EndDisabled();
        ImGui.EndGroup();
        string newEntryPopupId = $"NewCalendarEntryPopup{date}";
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(newEntryPopupId);
        if (ImGui.BeginPopup(newEntryPopupId))
        {
            if (ImGuiHelper.AddButton(RaidSession.DataTypeNameStatic, "#add"))
            {
                _module.Services.UiSystem.EditWindows.Create(new RaidSession(date.ToDateTime(TimeOnly.MinValue)));
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}