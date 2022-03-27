using Dalamud.Interface;
using ImGuiNET;

namespace HimbeertoniRaidTool.UI
{
    public static class ImGuiHelper
    {
        public static bool Button(string label, string? tooltip = null)
        {
            bool result = ImGui.Button(label);
            if (tooltip is not null && ImGui.IsItemHovered())
            {
                ImGui.PushFont(UiBuilder.DefaultFont);
                ImGui.SetTooltip(tooltip);
                ImGui.PopFont();
            }

            return result;
        }
        public static bool Button(FontAwesomeIcon icon, string id, string? tooltip = null)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            bool result = ImGui.Button($"{icon.ToIconString()}##{id}");
            ImGui.PopFont();
            if (tooltip is not null && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
            return result;
        }
    }
}
