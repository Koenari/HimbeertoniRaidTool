using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;


// ReSharper disable UnusedMember.Global

namespace HimbeertoniRaidTool.Plugin.UI;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class ImGuiHelper
{


    public static bool SaveButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.Save, "##save", tooltip ?? GeneralLoc.General_btn_tt_save, enabled,
                  size ?? new Vector2(50f, 25f));
    public static bool CancelButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "##cancel", tooltip ?? GeneralLoc.Ui_btn_tt_cancel, enabled,
                  size ?? new Vector2(50f, 25f));
    public static bool CloseButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "##close", tooltip ?? GeneralLoc.General_btn_tt_close, enabled,
                  size ?? new Vector2(50f, 25f));
    public static bool DeleteButton<T>(T data, bool enabled = true, Vector2? size = null)
        where T : IHrtDataType =>
        GuardedButton(FontAwesomeIcon.Eraser, "##delete",
                      string.Format(GeneralLoc.General_btn_tt_delete, T.DataTypeName, data.Name), enabled,
                      size ?? new Vector2(50f, 25f));
    public static bool EditButton<T>(T data, string id, bool enabled = true, Vector2 size = default)
        where T : IHrtDataType
        => Button(FontAwesomeIcon.Edit, id, string.Format(GeneralLoc.General_btn_tt_edit, T.DataTypeName, data.Name),
                  enabled, size);
    public static bool DeleteButton<T>(T data, string id, bool enabled = true, Vector2 size = default)
        where T : IHrtDataType =>
        GuardedButton(FontAwesomeIcon.Eraser, id,
                      string.Format(GeneralLoc.General_btn_tt_delete, T.DataTypeName, data.Name), enabled, size);
    public static bool RemoveButton<T>(T data, string id, bool enabled = true, Vector2 size = default)
        where T : IHrtDataType =>
        GuardedButton(FontAwesomeIcon.Eraser, id,
                      string.Format(GeneralLoc.General_btn_tt_remove, T.DataTypeName, data.Name), enabled, size);
    public static bool AddButton(string dataType, string id, bool enabled = true, Vector2 size = default)
        => Button(FontAwesomeIcon.Plus, id, string.Format(GeneralLoc.Ui_btn_tt_add, dataType), enabled, size);
    public static bool GuardedButton(string label, string? tooltip, Vector2 size) =>
        GuardedButton(label, tooltip, true, size);
    public static bool GuardedButton(string label, string? tooltip, bool enabled = true, Vector2 size = default) =>
        Button(label, $"{tooltip} ({GeneralLoc.Ui_GuardedButton_notice})",
               enabled && ImGui.IsKeyDown(ImGuiKey.ModShift), size);

    public static bool Button(string label, string? tooltip, bool enabled = true, Vector2 size = default)
    {
        bool result;
        using (ImRaii.Disabled(!enabled))
        {
            result = ImGui.Button(label.Capitalized(), size);
        }
        if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(tooltip);
        return result;
    }

    public static bool GuardedButton(FontAwesomeIcon icon, string id, string? tooltip, Vector2 size) =>
        GuardedButton(icon, id, tooltip, true, size);
    public static bool GuardedButton(FontAwesomeIcon icon, string id, string? tooltip, bool enabled = true,
                                     Vector2 size = default) =>
        Button(icon, id, $"{tooltip} ({GeneralLoc.Ui_GuardedButton_notice})",
               enabled && ImGui.IsKeyDown(ImGuiKey.ModShift), size);

    public static bool Button(FontAwesomeIcon icon, string id, string? tooltip, bool enabled = true,
                              Vector2 size = default)
    {
        bool result;
        {
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            using var disabled = ImRaii.Disabled(!enabled);
            result = ImGui.Button($"{icon.ToIconChar()}##{id}", size);
        }
        if (tooltip is not null) AddTooltip(tooltip);
        return result;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox(string label, ref bool value, string? tooltip)
    {
        bool result = ImGui.Checkbox(label, ref value);
        if (tooltip is not null) AddTooltip(tooltip);
        return result;
    }
    public static bool GearUpdateButtons(Player p, IHrtModule module, bool showMultiple = false, Vector2 size = default)
    {
        using var id = ImRaii.PushId(p.NickName);
        bool result = false;
        string inspectTooltip = GeneralLoc.Ui_btn_Inspect_tt;
        bool canInspect = true;
        if (!module.Services.CharacterInfoService.TryGetChar(out var playerChar, p.MainChar.Name,
                                                             p.MainChar.HomeWorld))
        {
            canInspect = false;
            inspectTooltip = GeneralLoc.Ui_btn_tt_CharacterNotInReach;
        }
        if (canInspect || showMultiple)
            result |= DrawInspectButton();
        if (!canInspect || showMultiple)
        {
            if (showMultiple)
                ImGui.SameLine();
            result |= DrawLodestoneButton();
        }
        if (!showMultiple)
        {
            using var popupContextItem = ImRaii.ContextPopupItem("##gearUpdateContextMenu");
            if (popupContextItem)
            {
                if (DrawInspectButton(true))
                    ImGui.CloseCurrentPopup();
                ImGui.SameLine();
                if (DrawLodestoneButton(true))
                    ImGui.CloseCurrentPopup();
            }
        }
        return result;
        bool DrawInspectButton(bool insideContextMenu = false)
        {
            if (Button(FontAwesomeIcon.Search, "##inspect",
                       $"{inspectTooltip}{(!showMultiple && !insideContextMenu ? $" ({GeneralLoc.Ui_rightClickHint})" : "")}",
                       canInspect, size))
            {
                CsHelpers.SafeguardedOpenExamine(playerChar, module.Services.Logger);
                return true;
            }
            return false;
        }
        bool DrawLodestoneButton(bool insideContextMenu = false)
        {
            string tooltip = GeneralLoc.Ui_btn_tt_Lodestone;
            if (Button(FontAwesomeIcon.CloudDownloadAlt, "lodestone",
                       $"{tooltip}{(!showMultiple && !insideContextMenu ? $" ({GeneralLoc.Ui_rightClickHint})" : "")}",
                       module.Services.ConnectorPool.LodestoneConnector.CanBeUsed, size))
            {
                module.HandleMessage(
                    new HrtUiMessage($"{GeneralLoc.LodestonConnetor_msg_UpdateStarted} {p.MainChar.Name}"));
                module.Services.TaskManager.RegisterTask(
                    new HrtTask<HrtUiMessage>(() => module.Services.ConnectorPool.LodestoneConnector.UpdateCharacter(p),
                                              module.HandleMessage, $"Update {p.MainChar.Name} from Lodestone"));
                return true;
            }
            return false;
        }
    }

    public static bool ExternalGearUpdateButton(GearSet set, IHrtModule module, Vector2 size = default)
    {
        using var id = ImRaii.PushId(set.LocalId.ToString());
        if (module.Services.ConnectorPool.TryGetConnector(set.ManagedBy, out var connector))
        {
            bool result = Button(FontAwesomeIcon.Download, set.ExternalId,
                                 string.Format(GeneralLoc.Ui_btn_tt_GearSetUpdate, set.Name, set.ExternalId,
                                               set.ManagedBy.FriendlyName()),
                                 set.ExternalId.Length > 0, size);
            if (result)
                connector.RequestGearSetUpdate(set, module.HandleMessage, string.Format(
                                                   GeneralLoc.Ui_btn_tt_GearSetUpdate, set.Name,
                                                   set.ExternalId,
                                                   set.ManagedBy.FriendlyName()));
            return result;
        }
        Button(FontAwesomeIcon.Download, set.ExternalId,
               "This set is not managed by an external service",
               false, size);
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddTooltip(string tooltip)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(tooltip.Capitalized());
    }


    public static void DrawMonth(string id, DateOnly month, Action<DateOnly, bool> drawDay,
                                 DayOfWeek firstDayOfWeek = DayOfWeek.Monday, bool abbreviatedDays = false)
    {
        using var table = ImRaii.Table($"##{id}", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp);
        if (!table) return;
        ImGui.TableSetupColumn(GetDayName((DayOfWeek)(((int)firstDayOfWeek + 0) % 7)));
        ImGui.TableSetupColumn(GetDayName((DayOfWeek)(((int)firstDayOfWeek + 1) % 7)));
        ImGui.TableSetupColumn(GetDayName((DayOfWeek)(((int)firstDayOfWeek + 2) % 7)));
        ImGui.TableSetupColumn(GetDayName((DayOfWeek)(((int)firstDayOfWeek + 3) % 7)));
        ImGui.TableSetupColumn(GetDayName((DayOfWeek)(((int)firstDayOfWeek + 4) % 7)));
        ImGui.TableSetupColumn(GetDayName((DayOfWeek)(((int)firstDayOfWeek + 5) % 7)));
        ImGui.TableSetupColumn(GetDayName((DayOfWeek)(((int)firstDayOfWeek + 6) % 7)));
        ImGui.TableHeadersRow();
        var day = new DateOnly(month.Year, month.Month, 1);
        var nextMonth = day.AddMonths(1);
        //Backtrack to the first day of the week

        day = day.AddDays(firstDayOfWeek - day.DayOfWeek > 0 ? firstDayOfWeek - day.DayOfWeek - 7
                              : firstDayOfWeek - day.DayOfWeek);
        //draw entries for month and adjacent days
        while (day < nextMonth || day.DayOfWeek != firstDayOfWeek)
        {
            ImGui.TableNextColumn();
            drawDay(day, day.Month == month.Month);
            day = day.AddDays(1);
        }
        return;
        string GetDayName(DayOfWeek dayOfWeek)
        {
            return abbreviatedDays ? dayOfWeek.Abbrev() : dayOfWeek.Name();
        }
    }
}

public static class StringExtensions
{
    public static string Capitalized(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return $"{char.ToUpper(input[0])}{input[1..]}";
    }
}