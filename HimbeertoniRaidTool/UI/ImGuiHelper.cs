using Dalamud.Interface;
using ImGuiNET;

namespace HimbeertoniRaidTool.UI
{
    public static class ImGuiHelper
    {
        public static bool Button(string label, string? tooltip = null, bool enabled = true)
        {
            if (!enabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            bool result = ImGui.Button(label);
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
        public static bool Button(FontAwesomeIcon icon, string id, string? tooltip = null, bool enabled = true)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (!enabled)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            bool result = ImGui.Button($"{icon.ToIconString()}##{id}");
            if (!enabled)
                ImGui.PopStyleVar();
            ImGui.PopFont();
            if (tooltip is not null && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
            return result && enabled;
        }
    }
}
