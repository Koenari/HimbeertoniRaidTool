using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Common.GameData;
using HimbeertoniRaidTool.Plugin.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class DrawDataExtension
{
    public static void Draw(this LuminaItem item)
    {
        var gameItem = new GameItem(item);
        if (gameItem.IsFood)
            Draw(new FoodItem(item.RowId));
        else if (gameItem.IsGear)
            Draw(new GearItem(item.RowId));
        else if (gameItem.IsMateria)
            Draw(new MateriaItem(item.RowId));
        else
            Draw(new Item(item.RowId));
    }
    public static void Draw(this Item item)
    {
        if (!item.Filled)
            return;
        using var table = ImRaii.Table("ItemTable", 2,
                                       ImGuiTableFlags.Borders | ImGuiTableFlags.SizingMask | ImGuiTableFlags.RowBg);
        if (!table)
            return;
        ImGui.TableSetupColumn(GeneralLoc.ItemTable_heading_Header);
        ImGui.TableSetupColumn("");
        //General Data
        DrawRow(GeneralLoc.ItemTable_heading_name, item.ToString());
        if (item.ItemLevel > 1)
            DrawRow(GeneralLoc.CommonTerms_itemLevel, item.ItemLevel);
        DrawRow(GeneralLoc.ItemTable_heading_source, item.Source());
        //Materia Stats
        if (item is MateriaItem matItem)
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
        if (item is FoodItem foodItem)
        {
            foreach (var statType in foodItem.StatTypesEffected)
            {
                ImGui.TableNextColumn();
                ImGui.Text(statType.FriendlyName());
                ImGui.TableNextColumn();
                var effect = foodItem.GetEffect(statType);
                ImGui.Text(effect.IsRelative ? $"+{effect.Value}%% (Max {effect.MaxValue})" : $"+{effect.Value}");

            }
        }
        //Shop Data
        if (item.CanBePurchased())
        {
            DrawRow(GeneralLoc.DrawItem_hdg_ShopCosts, string.Empty);
            foreach ((string shopName, var shopEntry) in item.PurchasedFrom())
            {
                ImGui.TableNextColumn();
                ImGui.Text($"    {shopName}");
                ImGui.TableNextColumn();
                foreach (var cost in shopEntry.ItemCosts.Where(cost => cost.ItemCost.RowId != 0))
                {
                    ImGui.Text(
                        $"{cost.CurrencyCost} {cost.ItemCost.AdjustItemCost(shopEntry.PatchNumber).Name}");
                }
            }
        }
        //Loot Data
        if (item.CanBeLooted())
        {
            string content = item.LootSources().Aggregate("", (current, instance) => current + $"{instance.Name}\n");
            DrawRow(GeneralLoc.ItemTable_heading_LootedIn, content);
        }
    }
    public static void Draw(this (Item cur, Item bis) itemTuple) =>
        Draw(itemTuple.cur, itemTuple.bis);
    private static void Draw(Item left, Item right)
    {
        if (!left.Filled || !right.Filled)
            return;
        using var table = ImRaii.Table("ItemTable", 3,
                                       ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp
                                                               | ImGuiTableFlags.RowBg);
        if (!table)
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
        DrawRow(GeneralLoc.ItemTable_heading_source, left.Source(), right.Source());
        //Materia Stats
        if (left is MateriaItem leftMat && right is MateriaItem rightMat)
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
            foreach (var type in leftGear.StatTypesAffected.Union(rightGear.StatTypesAffected))
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
        if (left.CanBePurchased() || right.CanBePurchased())
        {
            DrawRow(GeneralLoc.DrawItem_hdg_ShopCosts, string.Empty, string.Empty);
            foreach ((string shopName, var shopEntry) in left.PurchasedFrom().Concat(right.PurchasedFrom()))
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
                            $"{cost.CurrencyCost} {cost.ItemCost.AdjustItemCost(shopEntry.PatchNumber).Name}");
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
                            $"{cost.CurrencyCost} {cost.ItemCost.AdjustItemCost(shopEntry.PatchNumber).Name}");
                    }
                }
                else
                {
                    ImGui.Text("-");
                }
            }
        }
        //Loot Data
        if (left.CanBeLooted() || right.CanBeLooted())
        {
            string leftSources =
                left.LootSources().Aggregate("", (current, instance) => current + $"{instance.Name}\n");
            string rightSources =
                right.LootSources().Aggregate("", (current, instance) => current + $"{instance.Name}\n");

            DrawRow(GeneralLoc.ItemTable_heading_LootedIn, leftSources, rightSources);
        }

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