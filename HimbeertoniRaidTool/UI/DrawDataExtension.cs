using HimbeertoniRaidTool.Common.Data;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.Plugin.Services.Localization;
using SpecialShop = HimbeertoniRaidTool.Common.SpecialShop;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class DrawDataExtension
{
    public static void Draw(this Item item) => new GearItem(item.RowId).Draw();
    public static void Draw(this HrtItem item)
    {
        if (!item.Filled)
            return;
        if (ImGui.BeginTable("ItemTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn(Localize("ItemTableHeader", "Header"));
            ImGui.TableSetupColumn(Localize("Value", "Value"));
            //General Data
            DrawRow(Localize("Name", "Name"), $"{item.Name} {(item is GearItem gear2 && gear2.IsHq ? "(HQ)" : "")}");
            if (item.ItemLevel > 1)
                DrawRow(Localize("itemLevelLong", "Item Level"), item.ItemLevel);
            DrawRow(Localize("itemSource", "Source"), item.Source);
            //Materia Stats
            if (item is HrtMateria matItem)
            {
                DrawRow(matItem.StatType.FriendlyName(), matItem.GetStat());
            }
            //Gear Data
            if (item is GearItem gearItem)
            {
                bool isWeapon = gearItem.Slots.Contains(GearSetSlot.MainHand);
                //Stats
                if (isWeapon)
                {
                    if (gearItem.GetStat(StatType.MagicalDamage) >= gearItem.GetStat(StatType.PhysicalDamage))
                        DrawRow(Localize("MagicDamage", "Magic Damage"), gearItem.GetStat(StatType.MagicalDamage));
                    else
                        DrawRow(Localize("PhysicalDamage", "Physical Damage"), gearItem.GetStat(StatType.PhysicalDamage));
                }
                else
                {
                    DrawRow(Localize("PhysicalDefense", "Defense"), gearItem.GetStat(StatType.Defense));
                    DrawRow(Localize("MagicalDefense", "Magical Defense"), gearItem.GetStat(StatType.MagicDefense));
                }
                foreach (StatType type in gearItem.StatTypesAffected)
                {
                    if (type != StatType.None)
                        DrawRow(type.FriendlyName(), gearItem.GetStat(type));
                }

                //Materia
                if (gearItem.Materia.Any())
                {
                    ImGui.TableNextColumn();
                    ImGui.Text("Materia");
                    ImGui.TableNextColumn();
                    foreach (HrtMateria? mat in gearItem.Materia)
                        ImGui.BulletText($"{mat.Name} ({mat.StatType.FriendlyName()} +{mat.GetStat()})");
                }
            }
            //Shop Data
            if (Common.Services.ServiceManager.ItemInfo.CanBePurchased(item.Id))
            {
                DrawRow(Localize("item:shopCosts", "Shop costs"), "");
                foreach ((string? shopName, SpecialShop.ShopEntry? shopEntry) in Common.Services.ServiceManager.ItemInfo.GetShopEntriesForItem(item.Id))
                {
                    string content = "";
                    foreach (SpecialShop.ItemCostEntry? cost in shopEntry.ItemCostEntries)
                    {
                        if (cost.Item.Row == 0)
                            continue;
                        content += $"{cost.Item.Value?.Name} ({cost.Count})\n";
                    }
                    DrawRow($"    {shopName}", content);
                }
            }
            //Loot Data
            if (Common.Services.ServiceManager.ItemInfo.CanBeLooted(item.Id))
            {
                string content = "";
                foreach (InstanceWithLoot? instance in Common.Services.ServiceManager.ItemInfo.GetLootSources(item.Id))
                {
                    content += $"{instance.Name}\n";
                }
                DrawRow(Localize("item:looted:sources", "Looted in"), content);
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