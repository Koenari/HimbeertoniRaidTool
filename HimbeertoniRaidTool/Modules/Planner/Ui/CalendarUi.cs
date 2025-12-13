using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Common.Localization;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Planner.Ui;

internal class CalendarUi : HrtWindow
{
    private readonly PlannerModule _module;
    private int _year;
    private int _inputYear;
    private bool _inputHasChanged = false;
    private DateTime _inputLastChanged;

    private Month _month;

    public DayOfWeek FirstDayOfWeek => _module.Configuration.Data.FirstDayOfWeek;

    public CalendarUi(PlannerModule module) : base(module.Services.UiSystem, "##calendarUi")
    {
        _module = module;
        Title = PlannerLoc.CalendarUi_Title;
        _year = DateTime.Today.Year;
        _inputYear = DateTime.Today.Year;
        _month = (Month)DateTime.Today.Month;
        _inputLastChanged = DateTime.Now;
        (Size, SizeCondition) = (ImGui.GetIO().DisplaySize / 3, ImGuiCond.FirstUseEver);
        Persistent = true;
        IsOpen = false;
    }

    public override void Draw()
    {
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
        if (ImGuiHelper.Button(FontAwesomeIcon.AngleLeft, "prevMonth", "Show last month"))
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
        if (ImGuiHelper.Button(FontAwesomeIcon.AngleRight, "nextMonth", "Show next month"))
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
        InputHelper.Combo(CommonLoc.Month, ref _month);
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
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetTextLineHeight());
        ImGui.Text(GeneralLoc.CalendarUi_txt_Active_session);
        ImGui.SameLine();
        if (_module.ActiveSession is null)
            ImGui.Text(GeneralLoc.CommonTerms_None);
        else
        {
            ImGui.TextColored(Colors.TextPetrol, $"{_module.ActiveSession}");
            ImGuiHelper.AddTooltip(GeneralLoc.CalendarUi_tt_Click_to_edit);
            if (ImGui.IsItemClicked())
                _module.Services.UiSystem.EditWindows.Create(_module.ActiveSession);
        }
    }

    private void DrawCalendar() => ImGuiHelper.DrawMonth("calendar", new DateOnly(_year, (int)_month, 1), DrawDay);

    private void DrawDay(DateOnly date, bool isThisMonth)
    {
        var entries = _module.GetRaidSessions(date.ToDateTime(TimeOnly.MinValue));
        using var disabled = ImRaii.Disabled(!isThisMonth);
        using var color =
            ImRaii.PushColor(ImGuiCol.Text, Colors.TextPetrol, date == DateOnly.FromDateTime(DateTime.Today));
        ImGui.Spacing();
        ImGui.Text($"{date.Day}");
        ImGui.SameLine();
        if (ImGuiHelper.AddButton(RaidSession.DataTypeName, $"#add{date.DayOfYear}"))
            _module.Services.UiSystem.EditWindows.Create(new RaidSession(date.ToDateTime(TimeOnly.MinValue)));
        bool hasDrawn = false;

        foreach (var calendarEntry in entries)
        {
            using var table = ImRaii.Table("##calendarEntry", 2,
                                           ImGuiTableFlags.BordersOuter | ImGuiTableFlags.SizingStretchProp);
            ImGui.TableNextColumn();
            hasDrawn = true;
            int maxInstances = GameInfo.CurrentSavageTier?.Bosses.Count ?? 0;
            //Potentially read from entry
            ImGui.Text($"{calendarEntry.StartTime:t} - {calendarEntry.Name}");
            ImGui.Text($"{calendarEntry.Participants.Count(e => e.InvitationStatus.WillBePresent())}"
                     + $"/{calendarEntry.Participants.Count()} Players");
            ImGui.Text($"{calendarEntry.PlannedContent.Count}/{maxInstances} Instances");
            ImGui.TableNextColumn();
            if (ImGuiHelper.EditButton(calendarEntry, calendarEntry.LocalId.ToString()))
                _module.Services.UiSystem.EditWindows.Create(calendarEntry, null, null, DeleteEntry);
            if (ImGuiHelper.Button(FontAwesomeIcon.Copy, "Copy", "Copy session"))
                UiSystem.AddWindow(new CopySessionWindow(UiSystem, calendarEntry));
            continue;

            void DeleteEntry()
            {
                _module.Services.HrtDataManager.RaidSessionDb.TryRemove(calendarEntry);
            }
        }
        if (!hasDrawn) ImGui.Text(PlannerLoc.UI_Calendar_Day_Nothing);
        ImGui.Spacing();
        ImGui.Spacing();
    }
}