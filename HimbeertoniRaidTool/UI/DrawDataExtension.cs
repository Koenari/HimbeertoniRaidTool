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
        if (!ImGui.BeginTable("ItemTable", 2,
                              ImGuiTableFlags.Borders | ImGuiTableFlags.SizingMask | ImGuiTableFlags.RowBg)) return;
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
            foreach (var type in gearItem.StatTypesAffected)
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
                foreach (var mat in gearItem.Materia)
                {
                    ImGui.BulletText($"{mat.Name}\r\n + {mat.GetStat()} {mat.StatType.FriendlyName()}");
                }
            }
        }
        //Shop Data
        if (ServiceManager.ItemInfo.CanBePurchased(item.Id))
        {
            DrawRow(GeneralLoc.DrawItem_hdg_ShopCosts, string.Empty);
            foreach ((string? shopName, var shopEntry) in ServiceManager.ItemInfo
                                                                        .GetShopEntriesForItem(item.Id))
            {
                ImGui.TableNextColumn();
                ImGui.Text($"    {shopName}");
                ImGui.TableNextColumn();
                foreach (var cost in shopEntry.ItemCosts.Where(cost => cost.ItemCost.RowId != 0))
                {
                    ImGui.Text(
                        $"{cost.CurrencyCost} {ServiceManager.ItemInfo.AdjustItemCost(cost.ItemCost, shopEntry.PatchNumber).Name}");
                }
            }
        }
        //Loot Data
        if (ServiceManager.ItemInfo.CanBeLooted(item.Id))
        {
            string content = ServiceManager.ItemInfo.GetLootSources(item.Id)
                                           .Aggregate("", (current, instance) => current + $"{instance.Name}\n");
            DrawRow(GeneralLoc.ItemTable_heading_LootedIn, content);
        }
        ImGui.EndTable();
    }
    public static void Draw(this (HrtItem cur, HrtItem bis) itemTuple) => Draw(itemTuple.cur, itemTuple.bis);
    private static void Draw(HrtItem left, HrtItem right)
    {
        if (!left.Filled || !right.Filled)
            return;
        if (!ImGui.BeginTable("ItemTable", 3,
                              ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            return;
        ImGui.TableSetupColumn("");
        ImGui.TableSetupColumn(LootmasterLoc.ItemTooltip_hdg_Equipped);
        ImGui.TableSetupColumn(LootmasterLoc.ItemTooltip_hdg_bis);
        ImGui.TableHeadersRow();
        //General Data
        DrawRow(GeneralLoc.ItemTable_heading_name, $"{left.Name} {(left is GearItem { IsHq: true } ? "(HQ)" : "")}",
                $"{right.Name} {(right is GearItem { IsHq: true } ? "(HQ)" : "")}");
        if (left.ItemLevel > 1 && right.ItemLevel > 1)
            DrawRow(GeneralLoc.CommonTerms_itemLevel, left.ItemLevel, right.ItemLevel);
        DrawRow(GeneralLoc.ItemTable_heading_source, left.Source, right.Source);
        //Materia Stats
        if (left is HrtMateria leftMat && right is HrtMateria rightMat)
        {
            if (leftMat.StatType == rightMat.StatType)
                DrawRow(leftMat.StatType.FriendlyName(), leftMat.GetStat(), rightMat.GetStat());
            else
            {
                DrawRow(leftMat.StatType.FriendlyName(), leftMat.GetStat(), 0);
                DrawRow(rightMat.StatType.FriendlyName(), 0, rightMat.GetStat());
            }
        }
        //Gear Data
        if (left is GearItem leftGear && right is GearItem rightGear)
        {
            bool isWeapon = leftGear.Slots.Contains(GearSetSlot.MainHand)
                         && rightGear.Slots.Contains(GearSetSlot.MainHand);
            //Stats
            if (isWeapon)
            {
                if (leftGear.GetStat(StatType.MagicalDamage) >= leftGear.GetStat(StatType.PhysicalDamage))
                    DrawRow(StatType.MagicalDamage.FriendlyName(), leftGear.GetStat(StatType.MagicalDamage),
                            rightGear.GetStat(StatType.MagicalDamage));
                else
                    DrawRow(StatType.PhysicalDamage.FriendlyName(), leftGear.GetStat(StatType.PhysicalDamage),
                            rightGear.GetStat(StatType.PhysicalDamage));
            }
            else
            {
                DrawRow(StatType.Defense.FriendlyName(), leftGear.GetStat(StatType.Defense),
                        rightGear.GetStat(StatType.Defense));
                DrawRow(StatType.MagicDefense.FriendlyName(), leftGear.GetStat(StatType.MagicDefense),
                        rightGear.GetStat(StatType.MagicDefense));
            }
            foreach (var type in leftGear.StatTypesAffected.Concat(rightGear.StatTypesAffected).Distinct())
            {
                if (type != StatType.None)
                    DrawRow(type.FriendlyName(), leftGear.GetStat(type), rightGear.GetStat(type));
            }

            //Materia
            if (leftGear.Materia.Any() || rightGear.Materia.Any())
            {
                ImGui.TableNextColumn();
                ImGui.Text(GeneralLoc.CommonTerms_Materia);
                ImGui.TableNextColumn();
                foreach (var mat in leftGear.Materia)
                {
                    ImGui.BulletText($"{mat.Name}\r\n + {mat.GetStat()} {mat.StatType.FriendlyName()}");
                }
                ImGui.TableNextColumn();
                foreach (var mat in rightGear.Materia)
                {
                    ImGui.BulletText($"{mat.Name}\r\n + {mat.GetStat()} {mat.StatType.FriendlyName()}");
                }
            }
        }
        var shopsDone = new HashSet<string>();
        //Shop Data
        if (ServiceManager.ItemInfo.CanBePurchased(left.Id) || ServiceManager.ItemInfo.CanBePurchased(right.Id))
        {
            DrawRow(GeneralLoc.DrawItem_hdg_ShopCosts, string.Empty, string.Empty);
            foreach ((string? shopName, var shopEntry) in ServiceManager.ItemInfo
                                                                        .GetShopEntriesForItem(left.Id)
                                                                        .Concat(ServiceManager.ItemInfo
                                                                                .GetShopEntriesForItem(right.Id)))
            {
                if (!shopsDone.Add(shopName)) continue;
                ImGui.TableNextColumn();
                ImGui.Text($"  {shopName}");
                ImGui.TableNextColumn();
                if (shopEntry.ReceiveItems.Any(rItem => rItem.Item.RowId == left.Id))
                {
                    foreach (var cost in shopEntry.ItemCosts.Where(cost => cost.ItemCost.RowId != 0))
                    {
                        ImGui.Text(
                            $"{cost.CurrencyCost} {ServiceManager.ItemInfo.AdjustItemCost(cost.ItemCost, shopEntry.PatchNumber).Name}");
                    }
                }
                else
                {
                    ImGui.Text("-");
                }
                ImGui.TableNextColumn();
                if (shopEntry.ReceiveItems.Any(riTem => riTem.Item.RowId == right.Id))
                {
                    foreach (var cost in shopEntry.ItemCosts.Where(cost => cost.ItemCost.RowId != 0))
                    {
                        ImGui.Text(
                            $"{cost.CurrencyCost} {ServiceManager.ItemInfo.AdjustItemCost(cost.ItemCost, shopEntry.PatchNumber).Name}");
                    }
                }
                else
                {
                    ImGui.Text("-");
                }
            }
        }
        //Loot Data
        if (ServiceManager.ItemInfo.CanBeLooted(left.Id) || ServiceManager.ItemInfo.CanBeLooted(right.Id))
        {
            string leftSources = ServiceManager.ItemInfo.GetLootSources(left.Id)
                                               .Aggregate("", (current, instance) => current + $"{instance.Name}\n");
            string rightSources = ServiceManager.ItemInfo.GetLootSources(right.Id)
                                                .Aggregate("", (current, instance) => current + $"{instance.Name}\n");

            DrawRow(GeneralLoc.ItemTable_heading_LootedIn, leftSources, rightSources);
        }
        ImGui.EndTable();

    }
    private static void DrawRow(string label, object value1)
    {

        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        ImGui.Text($"{value1}");
    }

    private static void DrawRow(string label, object value1, object value2)
    {

        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        ImGui.Text($"{value1}");
        ImGui.TableNextColumn();
        ImGui.Text($"{value2}");
    }
}