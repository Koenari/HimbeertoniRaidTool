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
                //General Data
                DrawRow(Localize("Name", "Name"), $"{item.Item.Name} {(item.IsHq ? "(HQ)" : "")}");
                DrawRow(Localize("itemLevelLong", "Item Level"), item.ItemLevel);
                DrawRow(Localize("itemSource", "Source"), item.Source);
                //Shop Data
                if (Services.ItemInfo.CanBePurchased(item.ID))
                {
                    var shopEntry = Services.ItemInfo.GetShopEntryForItem(item.ID);
                    if (shopEntry != null)
                    {
                        string content = "";
                        foreach (var cost in shopEntry.ItemCostEntries)
                        {
                            if (cost.Item.Row == 0)
                                continue;
                            content += $"{cost.Item.Value?.Name} ({cost.Count})\n";
                        }
                        DrawRow(Localize("itemShop", "Shop cost"), content);
                    }


                }
                //Loot Data
                if (Services.ItemInfo.CanBeLooted(item.ID))
                {
                    string content = "";
                    foreach (InstanceWithLoot instance in Services.ItemInfo.GetLootSources(item.ID))
                    {
                        content += $"{instance.Name}\n";
                    }
                    DrawRow(Localize("item:looted:sources", "Looted in"), content);
                }
                //Stats
                if (isWeapon)
                {
                    if (item.Item.DamageMag >= item.Item.DamagePhys)
                        DrawRow(Localize("MagicDamage", "Magic Damage"), item.Item.DamageMag);
                    else
                        DrawRow(Localize("PhysicalDamage", "Physical Damage"), item.Item.DamagePhys);
                }
                else
                {
                    DrawRow(Localize("PhysicalDefense", "Defense"), item.Item.DefensePhys);
                    DrawRow(Localize("MagicalDefense", "Magical Defense"), item.Item.DefenseMag);
                }
                foreach (var stat in item.Item.UnkData59)
                    if ((StatType)stat.BaseParam != StatType.None)
                        DrawRow(((StatType)stat.BaseParam).FriendlyName(), stat.BaseParamValue);
                //Materia
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
        private static void DrawRow(string label, object value)
        {

            ImGui.TableNextColumn();
            ImGui.Text(label);
            ImGui.TableNextColumn();
            ImGui.Text($"{value}");
        }
    }
}
