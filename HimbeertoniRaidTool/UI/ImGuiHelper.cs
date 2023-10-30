using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Modules;
using ImGuiNET;
using Lumina.Excel;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class ImGuiHelper
{
    public static bool SaveButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.Save, "Save", tooltip ?? Localize("Save", "Save"), enabled, size ?? new Vector2(50f, 25f));
    public static bool CancelButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "Cancel", tooltip ?? Localize("Cancel", "Cancel"), enabled, size ?? new Vector2(50f, 25f));
    public static bool CloseButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "Close", tooltip ?? Localize("Close", "Close"), enabled, size ?? new Vector2(50f, 25f));
    public static bool Button(string label, string? tooltip, bool enabled = true, Vector2 size = default)
    {
        ImGui.BeginDisabled(!enabled);
        bool result = ImGui.Button(label, size);
        ImGui.EndDisabled();
        if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(tooltip);
        return result;
    }
    public static bool Button(FontAwesomeIcon icon, string id, string? tooltip, bool enabled = true, Vector2 size = default)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.BeginDisabled(!enabled);
        bool result = ImGui.Button($"{icon.ToIconChar()}##{id}", size);
        ImGui.EndDisabled();
        ImGui.PopFont();
        if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(tooltip);
        return result;
    }
    public static bool GearUpdateButtons(Player p, IHrtModule module, bool showMultiple = false, Vector2 size = default)
    {
        ImGui.PushID(p.NickName);
        bool result = false;
        string inspectTooltip = Localize("Inspect", "Update Gear by Examining");
        bool canInspect = true;
        if (!ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? playerChar, p.MainChar.Name, p.MainChar.HomeWorld))
        {
            canInspect = false;
            inspectTooltip = Localize("CharacterNotInReach", "Character is not in reach to examine");
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
            if (ImGui.BeginPopupContextItem("gearUpdateContextMenu"))
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
            if (Button(FontAwesomeIcon.Search, "inspect",
                    $"{inspectTooltip}{(!showMultiple && !insideContextMenu ? $" ({Localize("rightClickHint", "right click for more options")})" : "")}",
                    canInspect, size))
            {
                GearRefresher.RefreshGearInfos(playerChar);
                return true;
            }
            return false;
        }
        bool DrawLodestoneButton(bool insideContextMenu = false)
        {
            string tooltip = Localize("Lodestone Button", "Download Gear from Lodestone");
            if (Button(FontAwesomeIcon.CloudDownloadAlt, "lodestone",
                    $"{tooltip}{(!showMultiple && !insideContextMenu ? $" ({Localize("rightClickHint", "right click for more options")})" : "")}",
                    ServiceManager.ConnectorPool.LodestoneConnector.CanBeUsed, size))
            {
                module.HandleMessage(new HrtUiMessage(
                    $"{Localize("LodestonUpdateStarted", "Started gear update for")} {p.MainChar.Name}", HrtUiMessageType.Info));
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
            ImGui.SetTooltip(tooltip);
    }


    public static bool Combo<T>(string label, ref T value) where T : struct
    {
        T? value2 = value;
        bool result = Combo(label, ref value2);
        if (result && value2.HasValue)
            value = value2.Value;
        return result;
    }


    public static bool Combo<T>(string label, ref T? value) where T : struct
    {
        int id = value.HasValue ? Array.IndexOf(Enum.GetValues(typeof(T)), value) : 0;
        bool result = ImGui.Combo(label, ref id, Enum.GetNames(typeof(T)).ToArray(), Enum.GetNames(typeof(T)).Length);
        if (result)
            value = (T?)Enum.GetValues(typeof(T)).GetValue(id);
        return result;
    }
    //Credit to UnknownX
    //Modified to have filtering of Excel sheet and be usable by keyboard only
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, Func<T, string, bool> searchPredicate, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), searchPredicate, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), preFilter, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, Func<T, string, bool> searchPredicate, Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, t => t.ToString(), searchPredicate, preFilter, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, toName, (t, s) => toName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase), flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
        Func<T, string, bool> searchPredicate, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, toName, searchPredicate, (t) => true, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName,
        Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
        => ExcelSheetCombo(id, out selected, getPreview, toName,
            (t, s) => toName(t).Contains(s, StringComparison.CurrentCultureIgnoreCase), preFilter, flags);
    public static bool ExcelSheetCombo<T>(string id, [NotNullWhen(true)] out T? selected,
        Func<ExcelSheet<T>, string> getPreview, Func<T, string> toName, Func<T, string, bool> searchPredicate,
        Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : ExcelRow
    {
        var sheet = ServiceManager.DataManager.GetExcelSheet<T>();
        if (sheet is null)
        {
            selected = null;
            return false;
        }
        return SearchableCombo(id, out selected, getPreview(sheet), sheet, toName, searchPredicate, preFilter, flags);
    }

    private static string _search = string.Empty;
    private static HashSet<object>? _filtered;
    private static int _hoveredItem = 0;
    //This is a small hack since to my knowledge there is no way to close and existing combo when not clicking
    private static readonly Dictionary<string, (bool toogle, bool wasEnterClickedLastTime)> _comboDic = new();
    public static bool SearchableCombo<T>(string id, [NotNullWhen(true)] out T? selected, string preview,
        IEnumerable<T> possibilities, Func<T, string> toName, Func<T, string, bool> searchPredicate,
        ImGuiComboFlags flags = ImGuiComboFlags.None) where T : notnull
        => SearchableCombo(id, out selected, preview, possibilities, toName, searchPredicate, _ => true, flags);
    public static bool SearchableCombo<T>(string id, [NotNullWhen(true)] out T? selected, string preview,
        IEnumerable<T> possibilities, Func<T, string> toName, Func<T, string, bool> searchPredicate,
        Func<T, bool> preFilter, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : notnull
    {
        if (!_comboDic.ContainsKey(id))
            _comboDic.Add(id, (false, false));
        (bool toogle, bool wasEnterClickedLastTime) = _comboDic[id];
        selected = default;
        if (!ImGui.BeginCombo(id + (toogle ? "##x" : ""), preview, flags)) return false;
        if (wasEnterClickedLastTime || ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            toogle = !toogle;
            _search = string.Empty;
            _filtered = null;
        }
        bool enterClicked = ImGui.IsKeyPressed(ImGuiKey.Enter);
        wasEnterClickedLastTime = enterClicked;
        _comboDic[id] = (toogle, wasEnterClickedLastTime);
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
            _filtered = possibilities.Where(preFilter).Where(s => searchPredicate(s, _search)).Cast<object>().ToHashSet();
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