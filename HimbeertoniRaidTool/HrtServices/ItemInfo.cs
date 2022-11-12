using System.Collections.Generic;
using System.Linq;
using Dalamud.Data;
using Dalamud.Utility;
using HimbeertoniRaidTool.Data;
using Lumina.Excel;
using Lumina.Excel.CustomSheets;

namespace HimbeertoniRaidTool.HrtServices
{
    internal class ItemInfo
    {
        private readonly ExcelSheet<SpecialShop> ShopSheet;
        private readonly Dictionary<uint, ItemSource> ItemSources;
        private readonly Dictionary<uint, ItemIDCollection> ItemContainerDB;
        private readonly Dictionary<uint, (uint shopID, int idx)> ShopIndex;
        private readonly Dictionary<uint, List<uint>> UsedAsCurrency;
        public ItemInfo(DataManager dataManager, CuratedData curData)
        {
            ItemSources = curData.ItemSourceDB;
            ItemContainerDB = curData.ItemContainerDB;
            ShopSheet = dataManager.GetExcelSheet<SpecialShop>()!;
            //Load Vendor Data
            ShopIndex = new();
            UsedAsCurrency = new();
            foreach (SpecialShop shop in ShopSheet.Where(s => !s.Name.RawString.IsNullOrEmpty()))
            {
                for (int idx = 0; idx < shop.ShopEntries.Length; idx++)
                {
                    var entry = shop.ShopEntries[idx];
                    //Cannot handle dual output
                    if (entry.ItemReceiveEntries[1].Item.Row != 0)
                        continue;
                    ShopIndex[entry.ItemReceiveEntries[0].Item.Row] = (shop.RowId, idx);
                    foreach (var item in entry.ItemCostEntries)
                    {
                        if (!UsedAsCurrency.ContainsKey(item.Item.Row))
                            UsedAsCurrency.Add(item.Item.Row, new());
                        UsedAsCurrency[item.Item.Row].Add(entry.ItemReceiveEntries[0].Item.Row);
                    }
                }
            }
        }
        public bool IsItemContainer(uint itemID) => ItemContainerDB.ContainsKey(itemID);
        public bool UsedAsShopCurrency(uint itemID) => UsedAsCurrency.ContainsKey(itemID);
        public ItemIDCollection GetPossiblePurchases(uint itemID) => new ItemIDList(UsedAsCurrency.GetValueOrDefault(itemID) ?? Enumerable.Empty<uint>());
        public ItemIDCollection GetContainerContents(uint itemID) => ItemContainerDB.GetValueOrDefault(itemID, ItemIDCollection.Empty);
        public SpecialShop.ShopEntry? GetShopEntryForItem(uint itemID) => ShopSheet.GetRow(ShopIndex[itemID].shopID)?.ShopEntries[ShopIndex[itemID].idx];
        public ItemSource GetSource(uint itemID) => ItemSources.GetValueOrDefault(itemID, ItemSource.undefined);
    }
}
