using ColorHelper;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
using System.Numerics;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    public class ShowItemWindow : HrtUI
    {
        private readonly GearItem Item;
        public ShowItemWindow(GearItem item) : base() => (Item, Visible) = (item, true);
        public override void Draw()
        {
            if (!Visible)
                return;
            ImGui.SetNextWindowSize(new Vector2(250, 250), ImGuiCond.Always);
            if (ImGui.Begin(Item.Item.Name, ref Visible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse))
            {

                if (ImGui.BeginTable("ItemTable", 2, ImGuiTableFlags.Borders))
                {
                    ImGui.TableSetupColumn(Localize("Header", "Header"));
                    ImGui.TableSetupColumn(Localize("Value", "Value"));
                    DrawRow(Localize("Name", "Name"), Item.Item.Name);
                    DrawRow(Localize("Item Level", "Item Level"), Item.ItemLevel.ToString());
                    DrawRow(Localize("Item Source", "Item Source"), Item.Source.ToString());

                    ImGui.EndTable();
                }
            }
            static void DrawRow(string label, string value)
            {

                ImGui.TableNextColumn();
                ImGui.Text(label);
                ImGui.PushStyleColor(ImGuiCol.TableRowBg, HRTColorConversions.Vec4(ColorName.White, 0.5f));
                ImGui.TableNextColumn();
                ImGui.Text(value);
                ImGui.PopStyleColor();
            }
        }
    }
}
