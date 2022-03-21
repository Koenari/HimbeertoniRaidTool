using Newtonsoft.Json;
using System;

namespace HimbeertoniRaidTool.Data
{
    public class GearSet
    {
        public DateTime? TimeStamp;
        public string EtroID = "";
        public string Name = "";
        public GearSetManager ManagedBy;
        private const int NumSlots = 12;
        private readonly GearItem[] Items = new GearItem[NumSlots];
        public GearItem MainHand { get => Items[0]; set => Items[0] = value; }
        public GearItem Head { get => Items[1]; set => Items[1] = value; }
        public GearItem Body { get => Items[2]; set => Items[2] = value; }
        public GearItem Hands { get => Items[3]; set => Items[3] = value; }
        public GearItem Legs { get => Items[4]; set => Items[4] = value; }
        public GearItem Feet { get => Items[5]; set => Items[5] = value; }
        public GearItem Ear { get => Items[6]; set => Items[6] = value; }
        public GearItem Neck { get => Items[7]; set => Items[7] = value; }
        public GearItem Wrist { get => Items[8]; set => Items[8] = value; }
        public GearItem Ring1 { get => Items[9]; set => Items[9] = value; }
        public GearItem Ring2 { get => Items[10]; set => Items[10] = value; }
        public GearItem OffHand { get => Items[11]; set => Items[11] = value; }
        [JsonIgnore]
        public int ItemLevel
        {
            get
            {
                uint itemLevel = 0;
                for (int i = 0; i < NumSlots; i++)
                {
                    if (Items[i] != null && Items[i].ItemLevel > 0)
                    {
                        itemLevel += Items[i].ItemLevel;
                    }
                }
                return (int)((float)itemLevel / (OffHand.Filled ? NumSlots : (NumSlots - 1)));

            }
        }
        public GearSet(GearSetManager manager = GearSetManager.HRT)
        {
            ManagedBy = manager;
            Clear();
        }
        public void Clear()
        {
            for (int i = 0; i < NumSlots; i++)
            {
                Items[i] = new(0);
            }
        }
        [JsonIgnore]
        public GearItem this[GearSetSlot slot]
        {
            get => Items[ToIndex(slot)];
            set => Items[ToIndex(slot)] = value;
        }
        public int GetStat(StatType type)
        {
            int result = 0;
            Array.ForEach(Items, x => result += x.GetStat(type));
            return result;
        }
        private static int ToIndex(GearSetSlot slot)
        {
            return slot switch
            {
                GearSetSlot.MainHand => 0,
                GearSetSlot.OffHand => 11,
                GearSetSlot.Head => 1,
                GearSetSlot.Body => 2,
                GearSetSlot.Hands => 3,
                GearSetSlot.Legs => 4,
                GearSetSlot.Feet => 5,
                GearSetSlot.Ear => 6,
                GearSetSlot.Neck => 7,
                GearSetSlot.Wrist => 8,
                GearSetSlot.Ring1 => 9,
                GearSetSlot.Ring2 => 10,
                _ => throw new IndexOutOfRangeException("GearSlot" + slot.ToString() + "does not exist"),
            };
        }
    }
}
namespace Lumina.Excel.GeneratedSheets
{
    using HimbeertoniRaidTool.Data;

    public static class EquipSlotCategoryExtensions
    {
        public static bool Contains(this EquipSlotCategory self, GearSetSlot slot)
        {
            switch (slot)
            {
                case GearSetSlot.MainHand: return self.MainHand != 0;
                case GearSetSlot.Head: return self.Head != 0;
                case GearSetSlot.Body: return self.Body != 0;
                case GearSetSlot.Hands: return self.Gloves != 0;
                case GearSetSlot.Waist: return self.Waist != 0;
                case GearSetSlot.Legs: return self.Legs != 0;
                case GearSetSlot.Feet: return self.Feet != 0;
                case GearSetSlot.OffHand: return self.OffHand != 0;
                case GearSetSlot.Ear: return self.Ears != 0;
                case GearSetSlot.Neck: return self.Neck != 0;
                case GearSetSlot.Wrist: return self.Wrists != 0;
                case GearSetSlot.Ring1: return self.FingerR != 0;
                case GearSetSlot.Ring2: return self.FingerL != 0;
                case GearSetSlot.SoulCrystal: return self.SoulCrystal != 0;
            }

            return false;
        }
        public static GearSetSlot ToSlot(this EquipSlotCategory self)
        {
            for (int i = 0; i < (int)GearSetSlot.SoulCrystal; i++)
            {
                if (self.Contains((GearSetSlot)i))
                {
                    return (GearSetSlot)i;
                }
            }
            return GearSetSlot.None;
        }
    }
}
