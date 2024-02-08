using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using ImGuiNET;
using Lumina.Excel;

// ReSharper disable UnusedMember.Global

namespace HimbeertoniRaidTool.Plugin.UI;

public static class ImGuiHelper
{

    private static string _search = string.Empty;
    private static HashSet<object>? _filtered;
    private static int _hoveredItem;
    //This is a small hack since to my knowledge there is no way to close and existing combo when not clicking
    private static readonly Dictionary<string, (bool toogle, bool wasEnterClickedLastTime)> _comboDic = new();
    public static bool SaveButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.Save, "##save", tooltip ?? GeneralLoc.General_btn_tt_save, enabled,
                  size ?? new Vector2(50f, 25f));
    public static bool CancelButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "##cancel", tooltip ?? GeneralLoc.Ui_btn_tt_cancel, enabled,
                  size ?? new Vector2(50f, 25f));
    public static bool CloseButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "##close", tooltip ?? GeneralLoc.General_btn_tt_close, enabled,
                  size ?? new Vector2(50f, 25f));
    public static bool EditButton<T>(T data, string id, bool enabled = true, Vector2 size = default)
        where T : IHrtDataType
        => Button(FontAwesomeIcon.Edit, id, string.Format(GeneralLoc.Ui_btn_tt_add, data.DataTypeName, ""),
                  enabled, size);
    public static bool DeleteButton<T>(T data, string id, bool enabled = true, Vector2 size = default)
        where T : IHrtDataType =>
        GuardedButton(FontAwesomeIcon.Eraser, id,
                      string.Format(GeneralLoc.General_btn_tt_delete, data.DataTypeName, data.Name), enabled, size);
    public static bool RemoveButton<T>(T data, string id, bool enabled = true, Vector2 size = default)
        where T : IHrtDataType =>
        GuardedButton(FontAwesomeIcon.Eraser, id,
                      string.Format(GeneralLoc.General_btn_tt_remove, data.DataTypeName, data.Name), enabled, size);
    public static bool AddButton(string dataType, string id, bool enabled = true, Vector2 size = default)
        => Button(FontAwesomeIcon.Plus, id, string.Format(GeneralLoc.Ui_btn_tt_add, dataType), enabled, size);
    public static bool GuardedButton(string label, string? tooltip, Vector2 size) =>
        GuardedButton(label, tooltip, true, size);
    public static bool GuardedButton(string label, string? tooltip, bool enabled = true, Vector2 size = default) =>
        Button(label, $"{tooltip} ({GeneralLoc.Ui_GuardedButton_notice})",
               enabled && ImGui.IsKeyDown(ImGuiKey.ModShift), size);

    public static bool Button(string label, string? tooltip, bool enabled = true, Vector2 size = default)
    {
        ImGui.BeginDisabled(!enabled);
        bool result = ImGui.Button(label.CapitaliezSentence(), size);
        ImGui.EndDisabled();
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
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.BeginDisabled(!enabled);
        bool result = ImGui.Button($"{icon.ToIconChar()}##{id}", size);
        ImGui.EndDisabled();
        ImGui.PopFont();
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
        ImGui.PushID(p.NickName);
        bool result = false;
        string inspectTooltip = GeneralLoc.Ui_btn_Inspect_tt;
        bool canInspect = true;
        if (!ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? playerChar, p.MainChar.Name,
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
            if (ImGui.BeginPopupContextItem("##gearUpdateContextMenu"))
            {
                if (DrawInspectButton(true))
                    ImGui.CloseCurrentPopup();
                ImGui.SameLine();
                if (DrawLodestoneButton(true))
                    ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
        }
        ImGui.PopID();
        return result;
        bool DrawInspectButton(bool insideContextMenu = false)
        {
            if (Button(FontAwesomeIcon.Search, "##inspect",
                       $"{inspectTooltip}{(!showMultiple && !insideContextMenu ? $" ({GeneralLoc.Ui_rightClickHint})" : "")}",
                       canInspect, size))
            {
                CsHelpers.SafeguardedOpenExamine(playerChar);
                return true;
            }
            return false;
        }
        bool DrawLodestoneButton(bool insideContextMenu = false)
        {
            string tooltip = GeneralLoc.Ui_btn_tt_Lodestone;
            if (Button(FontAwesomeIcon.CloudDownloadAlt, "lodestone",
                       $"{tooltip}{(!showMultiple && !insideContextMenu ? $" ({GeneralLoc.Ui_rightClickHint})" : "")}",
                       ServiceManager.ConnectorPool.LodestoneConnector.CanBeUsed, size))
            {
                module.HandleMessage(
                    new HrtUiMessage($"{GeneralLoc.LodestonConnetor_msg_UpdateStarted} {p.MainChar.Name}"));
                ServiceManager.TaskManager.RegisterTask(
                    new HrtTask(() => ServiceManager.ConnectorPool.LodestoneConnector.UpdateCharacter(p),
                                module.HandleMessage, $"Update {p.MainChar.Name} from Lodestone"));
                return true;
            }
            return false;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddTooltip(string tooltip)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(tooltip.CapitaliezSentence());
    }


    public static bool Combo<T>(string label, ref T value, Func<T, string>? toName = null) where T : struct, Enum
    {
        T? value2 = value;
        Func<T?, string>? toNameInternal = toName is null ? null : t => t.HasValue ? toName(t.Value) : "";
        bool result = Combo(label, ref value2, toNameInternal);
        if (result && value2.HasValue)
            value = value2.Value;
        return result;
    }


    public static bool Combo<T>(string label, ref T? value, Func<T?, string>? toName = null) where T : struct, Enum
    {
        string[] names = Enum.GetNames(typeof(T));
        toName ??= t => names[t.HasValue ? Array.IndexOf(Enum.GetValues(typeof(T)), t) : 0];
        bool result = false;
        if (ImGui.BeginCombo(label, toName(value)))
        {
            foreach (T choice in Enum.GetValues<T>())
            {
                if (ImGui.Selectable(toName(choice)))
                {
                    value = choice;
                    result = true;
                }
            }
            ImGui.EndCombo();
        }
        return result;
    }
    //Credit to UnknownX
    //Modified to have filtering of Excel sheet and be usable by keyboard only
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview, Func<T, string, bool> searchPredicate,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), searchPredicate, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview, Func<T, bool> preFilter,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), preFilter, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview, Func<T, string, bool> searchPredicate,
                                          Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), searchPredicate, preFilter, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, toName,
                           (t, s) => toName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase), flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                          Func<T, string, bool> searchPredicate,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, toName, searchPredicate, _ => true, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                          Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, toName,
                           (t, s) => toName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase), preFilter,
                           flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
                                          Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
                                          Func<T, string, bool> searchPredicate,
                                          Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : ExcelRow
    {
        var sheet = ServiceManager.DataManager.GetExcelSheet<T>();
        if (sheet is null)
        {
            selected = null;
            return false;
        }
        return SearchableCombo(id, out selected, getPreview(sheet), sheet, toName, searchPredicate, preFilter, flags);
    }

    public static bool SearchableCombo<T>(string id, [NotNullWhen(true)] out T? selected, string preview,
                                          IEnumerable<T> possibilities, Func<T, string> toName,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : notnull =>
        SearchableCombo(id, out selected, preview, possibilities, toName,
                        (p, s) => toName.Invoke(p).Contains(s, StringComparison.InvariantCultureIgnoreCase), flags);
    public static bool SearchableCombo<T>(string id, [NotNullWhen(true)] out T? selected, string preview,
                                          IEnumerable<T> possibilities, Func<T, string> toName,
                                          Func<T, string, bool> searchPredicate,
                                          ImGuiComboFlags flags = ImGuiComboFlags.None) where T : notnull =>
        SearchableCombo(id, out selected, preview, possibilities, toName, searchPredicate, _ => true, flags);
    public static bool SearchableCombo<T>(string id, [NotNullWhen(true)] out T? selected, string preview,
                                          IEnumerable<T> possibilities, Func<T, string> toName,
                                          Func<T, string, bool> searchPredicate,
                                          Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None)
        where T : notnull
    {

        _comboDic.TryAdd(id, (false, false));
        (bool toggle, bool wasEnterClickedLastTime) = _comboDic[id];
        selected = default;
        if (!ImGui.BeginCombo(id + (toggle ? "##x" : ""), preview, flags)) return false;
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
        _hoveredItem = Math.Clamp(_hoveredItem, 0, Math.Max(_filtered?.Count - 1 ?? 0, 0));
        if (ImGui.IsWindowAppearing() && ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive())
        {
            _search = string.Empty;
            _filtered = null;
            ImGui.SetKeyboardFocusHere(0);
        }

        if (ImGui.InputText("##ExcelSheetComboSearch", ref _search, 128))
            _filtered = null;
        if (_filtered == null)
        {
            _filtered = possibilities.Where(preFilter).Where(s => searchPredicate(s, _search)).Cast<object>()
                                     .ToHashSet();
            _hoveredItem = 0;
        }
        int i = 0;
        foreach (T? row in _filtered.Cast<T>())
        {
            bool hovered = _hoveredItem == i;
            ImGui.PushID(i);

            if (ImGui.Selectable(toName(row), hovered) || enterClicked && hovered)
            {
                selected = row;
                ImGui.PopID();
                ImGui.EndCombo();
                return true;
            }
            ImGui.PopID();
            i++;
        }

        ImGui.EndCombo();
        return false;
    }
}

public static class StringExtensions
{
    public static string CapitaliezSentence(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return $"{char.ToUpper(input[0])}{input[1..]}";
    }
}