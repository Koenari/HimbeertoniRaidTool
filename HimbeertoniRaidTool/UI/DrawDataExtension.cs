using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorHelper;
using HimbeertoniRaidTool.Data;
using ImGuiNET;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    public static class DrawDataExtension
    {
        public static void Draw(this GearItem item)
        {
            bool isWeapon = item.Slot == GearSetSlot.MainHand;
            if (ImGui.BeginTable("ItemTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn(Localize("ItemTableHeader", "Header"));
                ImGui.TableSetupColumn(Localize("Value", "Value"));
                DrawRow(Localize("Name", "Name"), item.Item.Name);
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

                }
                foreach (var stat in item.Item.UnkData59)
                    if ((StatType)stat.BaseParam != StatType.None)
                        DrawRow(((StatType)stat.BaseParam).FriendlyName(), item.GetStat((StatType)stat.BaseParam, false).ToString());
                ImGui.TableNextColumn();
                ImGui.Text("Materia");
                ImGui.TableNextColumn();
                foreach (var mat in item.Materia)
                    ImGui.BulletText($"{mat.Name} ({mat.Category.GetStatType().FriendlyName()} +{mat.GetStat()})");
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
