using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using ServiceManager = HimbeertoniRaidTool.Common.Services.ServiceManager;
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
            ImGui.TableSetupColumn(GeneralLoc.ItemTable_heading_Header);
            ImGui.TableSetupColumn(GeneralLoc.Value);
            //General Data
            DrawRow(GeneralLoc.ItemTable_heading_name,
                    $"{item.Name} {(item is GearItem { IsHq: true } ? "(HQ)" : "")}");
            if (item.ItemLevel > 1)
                DrawRow(GeneralLoc.ItemTable_heading_iLvl, item.ItemLevel);
            DrawRow(GeneralLoc.ItemTable_heading_source, item.Source);
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
                        DrawRow(StatType.MagicalDamage.FriendlyName(), gearItem.GetStat(StatType.MagicalDamage));
                    else
                        DrawRow(StatType.PhysicalDamage.FriendlyName(), gearItem.GetStat(StatType.PhysicalDamage));
                }
                else
                {
                    DrawRow(StatType.Defense.FriendlyName(), gearItem.GetStat(StatType.Defense));
                    DrawRow(StatType.MagicDefense.FriendlyName(), gearItem.GetStat(StatType.MagicDefense));
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
                    ImGui.Text(GeneralLoc.Materia);
                    ImGui.TableNextColumn();
                    foreach (HrtMateria? mat in gearItem.Materia)
                    {
                        ImGui.BulletText($"{mat.Name} ({mat.StatType.FriendlyName()} +{mat.GetStat()})");
                    }
                }
            }
            //Shop Data
            if (ServiceManager.ItemInfo.CanBePurchased(item.Id))
            {
                DrawRow(GeneralLoc.DrawItem_heading_ShopCosts, string.Empty);
                foreach ((string? shopName, SpecialShop.ShopEntry? shopEntry) in ServiceManager.ItemInfo
                             .GetShopEntriesForItem(item.Id))
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
            if (ServiceManager.ItemInfo.CanBeLooted(item.Id))
            {
                string content = "";
                foreach (InstanceWithLoot? instance in ServiceManager.ItemInfo.GetLootSources(item.Id))
                {
                    content += $"{instance.Name}\n";
                }
                DrawRow(GeneralLoc.ItemTable_heading_LootedIn, content);
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