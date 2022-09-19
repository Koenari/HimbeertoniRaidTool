using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.Modules.LootMaster;
using ImGuiNET;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    public static class ImGuiHelper
    {
        public static bool SaveButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.Save, "Save", tooltip ?? Localize("Save", "Save"), enabled, size ?? new Vector2(50f, 25f));
        public static bool CancelButton(string? tooltip = null, bool enabled = true, Vector2? size = null)
        => Button(FontAwesomeIcon.WindowClose, "Cancel", tooltip ?? Localize("Cancel", "Cancel"), enabled, size ?? new Vector2(50f, 25f));
        public static bool Button(string label, string? tooltip, bool enabled = true, Vector2 size = default)
        {
            ImGui.BeginDisabled(!enabled);
            bool result = ImGui.Button(label, size);
            ImGui.EndDisabled();
            if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(tooltip);
            ImGui.EndDisabled();
            return result;
        }
        public static bool Button(FontAwesomeIcon icon, string id, string? tooltip, bool enabled = true, Vector2 size = default)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.BeginDisabled(!enabled);
            bool result = ImGui.Button($"{icon.ToIconString()}##{id}", size);
            ImGui.EndDisabled();
            ImGui.PopFont();
            if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(tooltip);
            return result;
        }
        public static bool GearUpdateButton(Player p)
        {
            var playerChar = Helper.TryGetChar(p.MainChar.Name, p.MainChar.HomeWorld);
            string inspectTooltip = Localize("Inspect", "Update Gear");
            bool inspectActive = true;
            if (!GearRefresherOnExamine.CanOpenExamine)
            {
                inspectActive = false;
                inspectTooltip = Localize("InspectUnavail", "Functionality unavailable due to incompatibility with game version");
            }
            else if (playerChar is null)
            {
                inspectActive = false;
                inspectTooltip = Localize("CharacterNotInReach", "Character is not in reach to examine");
            }
            bool pressed = Button(FontAwesomeIcon.Search, p.Pos.ToString(), inspectTooltip, inspectActive);
            if (pressed)
            {
                GearRefresherOnExamine.RefreshGearInfos(playerChar);
            }
            return pressed;
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
    }
}
