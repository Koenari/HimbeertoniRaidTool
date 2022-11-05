using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonDictionary]
    public class Inventory : Dictionary<int, InventoryEntry>
    {
        public bool Contains(uint id) => Values.Any(i => i.ID == id);
    }
    [JsonDictionary]
    internal class Wallet : Dictionary<Currency, uint>
    {

    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore, MemberSerialization = MemberSerialization.Fields)]
    public struct InventoryEntry
    {
        public uint quantity;
        public string type;
        public HrtItem hrtItem;
        public GearItem gearItem;
        public HrtMateria hrtMateria;

        public bool IsGear => type == nameof(GearItem);
        public bool IsMateria => type == nameof(HrtMateria);
        public uint ID
        {
            get
            {
                if (type == nameof(GearItem)) return gearItem.ID;
                if (type == nameof(HrtMateria)) return hrtMateria.ID;
                if (type == nameof(HrtItem)) return hrtItem.ID;
                return 0;
            }
        }
        public static implicit operator InventoryEntry(GearItem item) =>
            new()
            {
                type = nameof(GearItem),
                gearItem = item,
                quantity = 1,
            };
        public static implicit operator InventoryEntry(HrtItem item) =>
            new()
            {
                type = nameof(HrtItem),
                hrtItem = item,
                quantity = 1,
            };
        public static implicit operator InventoryEntry(HrtMateria item) =>
            new()
            {
                type = nameof(HrtMateria),
                hrtMateria = item,
                quantity = 1,
            };
    }
    internal class Currency
    {

    }
}
