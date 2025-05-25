using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Common.Localization;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.Planner.Ui;

internal class CalendarUi : HrtWindow
{
    private readonly PlannerModule _module;
    private int _year;
    private int _inputYear;
    private bool _inputHasChanged = false;
    private DateTime _inputLastChanged;

    private Month _month;

    private Vector2 _daySize = new(200, 100);
    public Vector2 DaySize => _daySize;

    public DayOfWeek FirstDayOfWeek => _module.ModuleConfigImpl.Data.FirstDayOfWeek;

    public CalendarUi(PlannerModule module) : base(module.Services.UiSystem, "##calendarUi")
    {
        _module = module;
        Title = PlannerLoc.CalendarUi_Title;
        _year = DateTime.Today.Year;
        _inputYear = DateTime.Today.Year;
        _month = (Month)DateTime.Today.Month;
        _inputLastChanged = DateTime.Now;
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
        return;

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
        if (ImGuiHelper.Button(FontAwesomeIcon.AngleLeft, "prevMonth", null))
        {
            if (_month == Month.January)
            {
                _inputYear--;
                _month = Month.December;
                _inputHasChanged = true;
            }
            else
            {
                _month--;
            }
        }
        ImGui.SameLine();
        if (ImGuiHelper.Button(FontAwesomeIcon.AngleRight, "nextMonth", null))
        {
            if (_month == Month.December)
            {
                _inputYear++;
                _month = Month.January;
                _inputHasChanged = true;
            }
            else
            {
                _month++;
            }
        }
        ImGui.SameLine();

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
        if (ImGuiHelper.Button(PlannerLoc.ui_btn_today, PlannerLoc.ui_btn_tt_today))
        {
            var now = DateTime.Now;
            _inputYear = _year = now.Year;
            _month = (Month)now.Month;
        }
    }

    private void DrawCalendar()
    {
        using var table = ImRaii.Table("##calendar", 7, ImGuiTableFlags.Borders);
        if (!table) return;

        ImGui.TableSetupColumn(Weekday.Monday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Tuesday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Wednesday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Thursday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Friday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Saturday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableSetupColumn(Weekday.Sunday.Name(), ImGuiTableColumnFlags.WidthFixed, DaySize.X);
        ImGui.TableHeadersRow();
        DateOnly day = new(_year, (int)_month, 1);
        var nextMonth = day.AddMonths(1);
        //Backtrack to the first day of a week
        while (day.DayOfWeek != FirstDayOfWeek)
        {
            day = day.AddDays(-1);
        }
        //draw entries for month and adjacent days
        while (day < nextMonth || day.DayOfWeek != FirstDayOfWeek)
        {
            ImGui.TableNextColumn();
            DrawDay(day, _module.GetRaidSessions(day.ToDateTime(TimeOnly.MinValue)));
            day = day.AddDays(1);
        }
    }

    private void DrawDay(DateOnly date, IEnumerable<RaidSession> entries)
    {
        bool isToday = date.Year == DateTime.Today.Year && date.DayOfYear == DateTime.Today.DayOfYear;
        //using var group = ImRaii.Group();
        using var disabled = ImRaii.Disabled(date.Month != (int)_month);
        using (ImRaii.PushColor(ImGuiCol.Text, Colors.TextPetrol, isToday))
        {
            ImGui.Text($"{date.Day}");

            bool hasDrawn = false;

            foreach (var calendarEntry in entries)
            {
                hasDrawn = true;
                ImGui.Text(
                    $"{calendarEntry.Name} ({calendarEntry.Participants.Count(e => e.InvitationStatus == InviteStatus.Accepted)}/{calendarEntry.Participants.Count()})");
                ImGui.SameLine();
                if (ImGuiHelper.EditButton(calendarEntry, calendarEntry.LocalId.ToString()))
                    _module.Services.UiSystem.EditWindows.Create(calendarEntry);
            }
            if (!hasDrawn) ImGui.Text(PlannerLoc.UI_Calendar_Day_Nothing);
            if (ImGuiHelper.AddButton(RaidSession.DataTypeNameStatic, $"#add{date.DayOfYear}"))
            {
                _module.Services.UiSystem.EditWindows.Create(new RaidSession(date.ToDateTime(TimeOnly.MinValue)));
            }
        }
    }
}