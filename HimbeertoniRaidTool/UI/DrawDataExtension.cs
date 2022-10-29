using System.Linq;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.UI
{
    public static class DrawDataExtension
    {
        public static void Draw(this Item item) => Draw(new GearItem(item.RowId));
        public static void Draw(this GearItem item)
        {
            if (item.ID == 0 || item.Item is null)
                return;
            bool isWeapon = item.Slots.Contains(GearSetSlot.MainHand);
            if (ImGui.BeginTable("ItemTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(Localize("ItemTableHeader", "Header"));
                ImGui.TableSetupColumn(Localize("Value", "Value"));
                DrawRow(Localize("Name", "Name"), $"{item.Item.Name} {(item.IsHq ? "(HQ)" : "")}");
                DrawRow(Localize("itemLevelLong", "Item Level"), item.ItemLevel.ToString());
                DrawRow(Localize("itemSource", "Source"), item.Source.ToString());
                if (isWeapon)
                {
                    if (item.Item.DamageMag >= item.Item.DamagePhys)
                        DrawRow(Localize("MagicDamage", "Magic Damage"), item.Item.DamageMag.ToString());
                    else
                        DrawRow(Localize("PhysicalDamage", "Physical Damage"), item.Item.DamagePhys.ToString());
                }
                else
                {
                    DrawRow(Localize("PhysicalDefense", "Defense"), item.Item.DefensePhys.ToString());
                    DrawRow(Localize("MagicalDefense", "Magical Defense"), item.Item.DefenseMag.ToString());
                }
                foreach (var stat in item.Item.UnkData59)
                    if ((StatType)stat.BaseParam != StatType.None)
                        DrawRow(((StatType)stat.BaseParam).FriendlyName(), item.GetStat((StatType)stat.BaseParam, false).ToString());
                if (item.Materia.Count > 0)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text("Materia");
                    ImGui.TableNextColumn();
                    foreach (var mat in item.Materia)
                        ImGui.BulletText($"{mat.Name} ({mat.StatType.FriendlyName()} +{mat.GetStat()})");
                }
                ImGui.EndTable();
            }
        }
        private static void DrawRow(string label, string value)
        {

            ImGui.TableNextColumn();
            ImGui.Text(label);
            ImGui.TableNextColumn();
            ImGui.Text(value);
        }
    }
}
