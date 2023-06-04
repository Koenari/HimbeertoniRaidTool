using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class DrawDataExtension
{
    public static void Draw(this Item item) => new GearItem(item.RowId).Draw();
    public static void Draw(this HrtItem item)
    {
        if (!item.Filled || item.Item is null)
            return;
        if (ImGui.BeginTable("ItemTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn(Localize("ItemTableHeader", "Header"));
            ImGui.TableSetupColumn(Localize("Value", "Value"));
            //General Data
            DrawRow(Localize("Name", "Name"), $"{item.Item.Name} {(item is GearItem gear2 && gear2.IsHq ? "(HQ)" : "")}");
            if (item.ItemLevel > 1)
                DrawRow(Localize("itemLevelLong", "Item Level"), item.ItemLevel);
            DrawRow(Localize("itemSource", "Source"), item.Source);
            //Materia Stats
            if (item is HrtMateria matItem && matItem.Item is not null)
            {
                DrawRow(matItem.StatType.FriendlyName(), matItem.GetStat());
            }
            //Gear Data
            if (item is GearItem gearItem && gearItem.Item is not null)
            {
                bool isWeapon = gearItem.Slots.Contains(GearSetSlot.MainHand);
                //Stats
                if (isWeapon)
                {
                    if (gearItem.Item.DamageMag >= gearItem.Item.DamagePhys)
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
                    foreach (var mat in gearItem.Materia)
                        ImGui.BulletText($"{mat.Name} ({mat.StatType.FriendlyName()} +{mat.GetStat()})");
                }
            }
            //Shop Data
            if (Common.Services.ServiceManager.ItemInfo.CanBePurchased(item.ID))
            {
                DrawRow(Localize("item:shopCosts", "Shop costs"), "");
                foreach (var (shopName, shopEntry) in Common.Services.ServiceManager.ItemInfo.GetShopEntriesForItem(item.ID))
                {
                    string content = "";
                    foreach (var cost in shopEntry.ItemCostEntries)
                    {
                        if (cost.Item.Row == 0)
                            continue;
                        content += $"{cost.Item.Value?.Name} ({cost.Count})\n";
                    }
                    DrawRow($"    {shopName}", content);
                }
            }
            //Loot Data
            if (Common.Services.ServiceManager.ItemInfo.CanBeLooted(item.ID))
            {
                string content = "";
                foreach (var instance in Common.Services.ServiceManager.ItemInfo.GetLootSources(item.ID))
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
