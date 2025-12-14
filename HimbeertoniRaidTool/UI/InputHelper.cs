using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class InputHelper
{

    #region Combo

    public static bool Combo<T>(string label, ref T value, Func<T, string>? toName = null, Func<T, bool>? select = null)
        where T : struct, Enum
    {
        T? value2 = value;
        Func<T?, string>? toNameInternal = toName is null ? null : t => t.HasValue ? toName(t.Value) : "";
        bool result = Combo(label, ref value2, toNameInternal, select, false);
        if (result && value2.HasValue)
            value = value2.Value;
        return result;
    }
    public static bool Combo<T>(string label, ref T? value, Func<T?, string>? toName = null,
                                Func<T, bool>? select = null, bool allowNull = true) where T : struct, Enum
    {
        string[] names = Enum.GetNames<T>();
        toName ??= t => names[t.HasValue ? Array.IndexOf(Enum.GetValues<T>(), t) : 0];
        select ??= _ => true;
        bool result = false;
        using var combo = ImRaii.Combo(label, toName(value));
        if (!combo)
            return result;
        if (allowNull && ImGui.Selectable(toName(null)))
        {
            value = null;
            result = true;
        }
        foreach (var choice in Enum.GetValues<T>())
        {
            if (!select(choice) || !ImGui.Selectable(toName(choice))) continue;
            value = choice;
            result = true;
        }
        return result;
    }
    #region Searchable

    public static bool SearchableCombo<T>(string id, out T? selected, string preview,
                                          IEnumerable<T> possibilities, Func<T, string> toName, bool allowNull = false,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : notnull =>
        SearchableCombo(id, out selected, preview, possibilities, toName,
                        (p, s) => toName.Invoke(p).Contains(s, StringComparison.InvariantCultureIgnoreCase), allowNull,
                        flags);
    public static bool SearchableCombo<T>(string id, out T? selected, string preview,
                                          IEnumerable<T> possibilities, Func<T, string> toName,
                                          Func<T, string, bool> searchPredicate, bool allowNull = false,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : notnull =>
        SearchableCombo(id, out selected, preview, possibilities, toName, searchPredicate, _ => true, allowNull, flags);

    private static string _search = string.Empty;
    private static HashSet<object>? _filtered;
    private static int _hoveredItem;
    //This is a small hack since to my knowledge there is no way to close an existing combo when not clicking
    private static readonly Dictionary<string, (bool toogle, bool wasEnterClickedLastTime)> _comboDic = new();
    public static bool SearchableCombo<T>(string id, out T? selected, string preview,
                                          IEnumerable<T> possibilities, Func<T, string> toName,
                                          Func<T, string, bool> searchPredicate,
                                          Func<T, bool> preFilter, bool allowNull = false,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : notnull
    {

        _comboDic.TryAdd(id, (false, false));
        (bool toggle, bool wasEnterClickedLastTime) = _comboDic[id];
        selected = default;
        using var combo = ImRaii.Combo(id + (toggle ? "##x" : ""), preview, flags);
        if (!combo)
            return false;
        if (wasEnterClickedLastTime || ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            toggle = !toggle;
            _search = string.Empty;
            _filtered = null;
        }
        bool enterClicked = ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter);
        wasEnterClickedLastTime = enterClicked;
        _comboDic[id] = (toggle, wasEnterClickedLastTime);
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
            _hoveredItem--;
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
            _hoveredItem++;
        _hoveredItem = Math.Clamp(_hoveredItem, 0, Math.Max(_filtered?.Count - (allowNull ? 0 : 1) ?? 0, 0));
        if (ImGui.IsWindowAppearing() && ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive())
        {
            _search = string.Empty;
            _filtered = null;
            ImGui.SetKeyboardFocusHere(0);
        }

        if (ImGui.InputText("##ComboSearchInput", ref _search, 128))
            _filtered = null;
        if (_filtered == null)
        {
            _filtered = possibilities.Where(preFilter).Where(s => searchPredicate(s, _search)).Cast<object>()
                                     .ToHashSet();
            _hoveredItem = 0;
        }
        int i = 0;
        if (allowNull)
        {
            bool hovered = _hoveredItem == i;
            if (ImGui.Selectable(GeneralLoc.CommonTerms_None, hovered) || enterClicked && hovered)
            {
                selected = default;
                return true;
            }
            i++;
        }
        foreach (var row in _filtered.Cast<T>())
        {
            bool hovered = _hoveredItem == i;
            using var imguiId = ImRaii.PushId(i);

            if (ImGui.Selectable(toName(row), hovered) || enterClicked && hovered)
            {
                selected = row;
                return true;
            }
            i++;
        }

        return false;
    }

    #endregion

    #endregion

    #region DateTime

    public static bool InputDate(string id, ref DateOnly date, string label = "Date")
    {
        using var pushedId = ImRaii.PushId(id);
        using var table =
            ImRaii.Table("##BorderTable", 1,
                         ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedFit);
        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        return InputDateInternal(ref date);
    }

    public static bool InputTime(string id, ref TimeOnly time, string label = "Time")
    {
        using var pushedId = ImRaii.PushId(id);
        using var table =
            ImRaii.Table("##BorderTable", 1,
                         ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedFit,
                         new Vector2(100 * HrtWindow.ScaleFactor, 0));
        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        return InputTimeInternal(ref time);
    }

    public static bool InputDuration(string id, ref TimeSpan duration, string label = "Duration")
    {
        using var pushedId = ImRaii.PushId(id);
        using var table =
            ImRaii.Table("##BorderTable", 1,
                         ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedFit,
                         new Vector2(100 * HrtWindow.ScaleFactor, 0));
        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        return GenericTimeInput(ref duration, true);
    }

    public static bool InputDateTime(string id, ref DateTime dateTime, string label = "Date and Time")
    {
        var localDate = DateOnly.FromDateTime(dateTime);
        var lcoalTime = TimeOnly.FromDateTime(dateTime);
        if (!InputDateTime(id, ref localDate, ref lcoalTime, label)) return false;
        dateTime = new DateTime(localDate, lcoalTime);
        return true;
    }

    public static bool InputDateTime(string id, ref DateOnly date, ref TimeOnly time, string label = "Date and Time")
    {
        using var pushedId = ImRaii.PushId(id);
        using var table =
            ImRaii.Table("##BorderTable", 1,
                         ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX,
                         new Vector2(200 * HrtWindow.ScaleFactor, 0));
        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        return DateTimeInputInternal(ref date, ref time);
    }

    #region Internal

    private static bool DateTimeInputInternal(ref DateOnly date, ref TimeOnly time)
    {

        bool changed = false;
        using var table = ImRaii.Table("##DateTimeTable", 2, ImGuiTableFlags.SizingFixedFit);
        if (!table) return changed;
        ImGui.TableNextColumn();
        changed |= InputDateInternal(ref date);
        ImGui.TableNextColumn();
        changed |= InputTimeInternal(ref time);
        return changed;
    }

    private static bool GenericTimeInput(ref TimeSpan time, bool isDuration)
    {
        using var pushedId = ImRaii.PushId("time");
        ImGui.SetNextItemWidth(200 * HrtWindow.ScaleFactor);
        using var combo =
            ImRaii.Combo($"##TimeCombo", isDuration ? $@"{time:hh\:mm}" : $"{TimeOnly.FromTimeSpan(time):t}",
                         ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.HeightLargest);
        if (!combo) return false;
        var minuteLocal = TimeSpan.FromMinutes(time.Minutes);
        var hourLocal = TimeSpan.FromHours(time.Hours);
        int hourInt = hourLocal.Hours;
        ImGui.SetNextItemWidth(25 * HrtWindow.ScaleFactor);
        if (ImGui.InputInt($":##int##DurHour", ref hourInt))
        {
            hourLocal = TimeSpan.FromHours(hourInt);
        }
        ImGuiHelper.AddTooltip(GeneralLoc.GeneralTerm_Hour);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(25 * HrtWindow.ScaleFactor);
        int minuteInt = minuteLocal.Minutes;
        if (ImGui.InputInt($"##int##Minute", ref minuteInt))
        {
            minuteLocal = TimeSpan.FromMinutes(minuteInt);
        }
        ImGuiHelper.AddTooltip(GeneralLoc.GeneralTerm_Minute);
        using var table = ImRaii.Table("##TimeTable", 5, ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.BordersV);
        if (!table) return false;

        var hourCounter = TimeSpan.Zero;
        var halfDay = TimeSpan.FromHours(12);
        var minuteCounter = TimeSpan.Zero;
        var halfHour = TimeSpan.FromMinutes(30);
        for (int row = 0; row < 12; row++)
        {
            ImGui.TableNextColumn();
            if (ImGui.Selectable($"{FormatHour(hourCounter)}##hour", hourLocal == hourCounter,
                                 ImGuiSelectableFlags.DontClosePopups))
                hourLocal = hourCounter;
            ImGui.TableNextColumn();
            if (ImGui.Selectable($"{FormatHour(hourCounter + halfDay)}##hour", hourLocal == hourCounter + halfDay,
                                 ImGuiSelectableFlags.DontClosePopups))
                hourLocal = hourCounter + halfDay;
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            if (row < 6 && ImGui.Selectable($"{minuteCounter:mm}##minute", minuteLocal == minuteCounter,
                                            ImGuiSelectableFlags.DontClosePopups))
                minuteLocal = minuteCounter;
            ImGui.TableNextColumn();
            if (row < 6 && ImGui.Selectable($"{minuteCounter + halfHour:mm}##minute",
                                            minuteLocal == minuteCounter + halfHour,
                                            ImGuiSelectableFlags.DontClosePopups))
                minuteLocal = minuteCounter + halfHour;
            hourCounter += TimeSpan.FromHours(1);
            minuteCounter += TimeSpan.FromMinutes(5);

        }
        var newTime = new TimeSpan(hourLocal.Hours, minuteLocal.Minutes, 0);
        if (time == newTime) return false;
        time = newTime;
        return true;
        string FormatHour(TimeSpan hour)
        {
            return isDuration ? $"{hour:hh}" : $"{TimeOnly.FromTimeSpan(hour):H tt}";
        }
    }

    private static bool InputTimeInternal(ref TimeOnly time)
    {
        var timeCopy = new TimeSpan(time.Hour, time.Minute, 0);
        if (!GenericTimeInput(ref timeCopy, false)) return false;
        time = new TimeOnly(timeCopy.Hours, timeCopy.Minutes);
        return true;

    }

    private static bool InputDateInternal(ref DateOnly date)
    {
        using var pushedId = ImRaii.PushId("date");
        ImGui.SetNextItemWidth(100 * HrtWindow.ScaleFactor);
        using var combo = ImRaii.Combo($"##DateCombo", $"{date:d}", ImGuiComboFlags.NoArrowButton);
        if (!combo) return false;

        var month = (Month)date.Month;
        int year = date.Year;
        if (ImGui.Selectable(PlannerLoc.ui_btn_today, false, ImGuiSelectableFlags.DontClosePopups,
                             new Vector2(50 * HrtWindow.ScaleFactor, ImGui.GetTextLineHeight())))
        {
            date = DateOnly.FromDateTime(DateTime.Today);
            return true;
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120 * HrtWindow.ScaleFactor);
        if (Combo("##month", ref month, m => m.Name()))
        {
            date = date.AddMonths((int)month - date.Month);
            return true;
        }
        ImGuiHelper.AddTooltip(GeneralLoc.GeneralTerm_Month);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(85 * HrtWindow.ScaleFactor);
        if (ImGui.InputInt($"##year", ref year, 1))
        {
            date = date.AddYears(year - date.Year);
            return true;
        }
        ImGuiHelper.AddTooltip(GeneralLoc.GeneralTerm_Year);
        var dateCopy = date;
        ImGuiHelper.DrawMonth("day", dateCopy, DrawDaySelectable, DayOfWeek.Monday, true);
        if (date.Day != dateCopy.Day)
        {
            date = dateCopy;
            return true;
        }
        return false;
        void DrawDaySelectable(DateOnly day, bool valid)
        {
            if (!valid) return;
            if (ImGui.Selectable($"{day.Day}", dateCopy.Day == day.Day, ImGuiSelectableFlags.DontClosePopups))
            {
                dateCopy = dateCopy.AddDays(day.Day - dateCopy.Day);
            }

        }
    }

    #endregion

    #endregion

}