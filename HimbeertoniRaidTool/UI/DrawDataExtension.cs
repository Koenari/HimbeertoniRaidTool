using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;
using Lumina.Excel.Sheets;
using ServiceManager = HimbeertoniRaidTool.Common.Services.ServiceManager;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class DrawDataExtension
{
    public static void Draw(this Item item) => new GearItem(item.RowId).Draw();
    public static void Draw(this HrtItem item)
    {
        if (!item.Filled)
            return;
        if (ImGui.BeginTable("ItemTable", 2,
                             ImGuiTableFlags.Borders | ImGuiTableFlags.SizingMask | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn(GeneralLoc.ItemTable_heading_Header);
            ImGui.TableSetupColumn("");
            //General Data
            DrawRow(GeneralLoc.ItemTable_heading_name,
                    $"{item.Name} {(item is GearItem { IsHq: true } ? "(HQ)" : "")}");
            if (item.ItemLevel > 1)
                DrawRow(GeneralLoc.CommonTerms_itemLevel, item.ItemLevel);
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
                    ImGui.Text(GeneralLoc.CommonTerms_Materia);
                    ImGui.TableNextColumn();
                    foreach (HrtMateria? mat in gearItem.Materia)
                    {
                        ImGui.BulletText($"{mat.Name}\r\n + {mat.GetStat()} {mat.StatType.FriendlyName()}");
                    }
                }
            }
            //Shop Data
            if (ServiceManager.ItemInfo.CanBePurchased(item.Id))
            {
                DrawRow(GeneralLoc.DrawItem_hdg_ShopCosts, string.Empty);
                foreach ((string? shopName, SpecialShop.ItemStruct shopEntry) in ServiceManager.ItemInfo
                             .GetShopEntriesForItem(item.Id))
                {
                    string content = "";
                    foreach (SpecialShop.ItemStruct.ItemCostsStruct cost in shopEntry.ItemCosts)
                    {
                        if (cost.ItemCost.RowId == 0)
                            continue;
                        Item costItem = ServiceManager.ItemInfo.AdjustItemCost(cost.ItemCost, shopEntry.PatchNumber);
                        content += $"{costItem.Name} ({cost.CurrencyCost})\n";
                    }
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped($"    {shopName}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{content}");
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