using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonDictionary]
    public class Inventory : Dictionary<int, InventoryEntry>
    {
        public bool Contains(uint id) => Values.Any(i => i.ID == id);
        public int IndexOf(uint id) => this.FirstOrDefault(i => i.Value.ID == id).Key;
        public int FirstFreeSlot()
        {
            for (int i = 0; i < Values.Count; i++)
                if (!this.ContainsKey(i)) return i;
            return Values.Count;
        }
    }
    [JsonDictionary]
    public class Wallet : Dictionary<Currency, uint>
    {

    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore, MemberSerialization = MemberSerialization.Fields)]
    public class InventoryEntry
    {
        public int quantity = 0;
        private string type;
        private HrtItem? hrtItem;
        private GearItem? gearItem;
        private HrtMateria? hrtMateria;

        public InventoryEntry(HrtItem item) => Item = item;

        private InventoryEntry(string typeArg)
        {
            type = typeArg;
        }

        [JsonIgnore]
        public HrtItem Item
        {
            get
            {
                if (IsGear)
                    return gearItem!;
                else if (IsMateria)
                    return hrtMateria!;
                else
                    return hrtItem!;
            }
            set
            {
                gearItem = null;
                hrtMateria = null;

                if (value is GearItem item)
                {
                    gearItem = item;
                    type = nameof(GearItem);
                }
                else if (value is HrtMateria mat)
                {
                    hrtMateria = mat;
                    type = nameof(HrtMateria);
                }
                else
                {
                    hrtItem = value;
                    type = nameof(HrtItem);
                }
            }
        }
        public bool IsGear => type == nameof(GearItem);
        public bool IsMateria => type == nameof(HrtMateria);
        public uint ID
        {
            get
            {
                if (type == nameof(GearItem)) return gearItem!.ID;
                if (type == nameof(HrtMateria)) return hrtMateria!.ID;
                if (type == nameof(HrtItem)) return hrtItem!.ID;
                return 0;
            }
        }
        public static implicit operator InventoryEntry(HrtItem item) => new(item);
    }
    public enum Currency : uint
    {
        Unknown = 0,
        Gil = 1,
        TomeStoneOfPhilosophy = 23,
        TomeStoneOfMythology = 24,
        WolfMark = 25,
        TomestoneOfSoldiery = 26,
        AlliedSeal = 27,
        TomestoneOfPoetics = 28,
        MGP = 29,
        TomestoneOfLaw = 30,
        TomestoneOfAstronomy = 43,
        TomestoneOfCausality = 44

    }
}
