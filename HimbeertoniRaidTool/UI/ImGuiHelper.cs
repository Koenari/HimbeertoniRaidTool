using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.LootMaster;
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
        public static bool Button(string label, string? tooltip, bool enabled = true, Vector2 size = default(Vector2))
        {
            if (!enabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            bool result = ImGui.Button(label, size);
            if (!enabled)
                ImGui.PopStyleVar();
            if (tooltip is not null && ImGui.IsItemHovered())
            {
                ImGui.PushFont(UiBuilder.DefaultFont);
                ImGui.SetTooltip(tooltip);
                ImGui.PopFont();
            }

            return result && enabled;
        }
        public static bool Button(FontAwesomeIcon icon, string id, string? tooltip, bool enabled = true, Vector2 size = default(Vector2))
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (!enabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            bool result = ImGui.Button($"{icon.ToIconString()}##{id}", size);
            if (!enabled)
                ImGui.PopStyleVar();
            ImGui.PopFont();
            if (tooltip is not null && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
            return result && enabled;
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
    }
}
