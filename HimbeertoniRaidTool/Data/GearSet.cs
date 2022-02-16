using System;
using System.Collections.Generic;
using static HimbeertoniRaidTool.Connectors.EtroConnector;
using static HimbeertoniRaidTool.Connectors.DalamudConnector;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class GearSet
    {
        public DateTime? TimeStamp;
        public string EtroID = "";
        private const int NumSlots = 12;
        private GearItem[] Items = new GearItem[NumSlots];
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

        public GearSet()
        {
            Clear();
        }
        public void Clear()
        {
            for (int i = 0; i < NumSlots; i++)
            {
                Items[i] = new(0);
            }
        }
        public int GetItemLevel()
        {
            int itemLevel = 0;
            for(int i = 0; i < NumSlots; i++)
            {
                if (Items[i] != null && Items[i].ItemLevel > 0)
                {
                    itemLevel += Items[i].ItemLevel;
                }
            }
            return (int)((float)itemLevel / (OffHand.Filled ? NumSlots : (NumSlots-1)));

        }
        public GearItem Set(GearSetSlot slot, GearItem value) => Items[ToIndex(slot)] = value;
        public GearItem Get(GearSetSlot slot) => Items[ToIndex(slot)];
        private int ToIndex(GearSetSlot slot)
        {
            switch (slot)
            {
                case GearSetSlot.MainHand:
                    return 0;
                case GearSetSlot.OffHand:
                    return 11;
                case GearSetSlot.Head:
                    return 1;
                case GearSetSlot.Body:
                    return 2;
                case GearSetSlot.Hands:
                    return 3;
                case GearSetSlot.Legs:
                    return 4;
                case GearSetSlot.Feet:
                    return 5;
                case GearSetSlot.Ear:
                    return 6;
                case GearSetSlot.Neck:
                    return 7;
                case GearSetSlot.Wrist:
                    return 8;
                case GearSetSlot.Ring1:
                    return 9;
                case GearSetSlot.Ring2:
                    return 10;
                default:
                    throw new IndexOutOfRangeException("GearSlot" + slot.ToString() + "does not exist");
            }
        }

        public bool FillStats()
        {

            bool result = true;
            List<bool> tasks = new();
            if (MainHand != null && MainHand.Name == "")
                tasks.Add(GetGearStats(MainHand));
            if (Head != null && Head.Name == "")
                tasks.Add(GetGearStats(Head));
            if (Body != null && Body.Name == "")
                tasks.Add(GetGearStats(Body));
            if (Hands != null && Hands.Name == "")
                tasks.Add(GetGearStats(Hands));
            if (Legs != null && Legs.Name == "")
                tasks.Add(GetGearStats(Legs));
            if (Feet != null && Feet.Name == "")
                tasks.Add(GetGearStats(Feet));
            if (Ear != null && Ear.Name == "")
                tasks.Add(GetGearStats(Ear));
            if (Neck != null && Neck.Name == "")
                tasks.Add(GetGearStats(Neck));
            if (Wrist != null && Wrist.Name == "")
                tasks.Add(GetGearStats(Wrist));
            if (Ring1 != null && Ring1.Name == "")
                tasks.Add(GetGearStats(Ring1));
            if (Ring2 != null && Ring2.Name == "")
                tasks.Add(GetGearStats(Ring2));
            if (OffHand != null && OffHand.Name == "")
                tasks.Add(GetGearStats(OffHand));
            foreach (bool t in tasks)
            {
                result &= t;
            }
            return result;
        }
    }
    public enum GearSetSlot : short
    {
        MainHand = 0,
        OffHand = 1,
        Head = 2,
        Body = 3,
        Hands = 4,
        Waist = 5,
        Legs = 6,
        Feet = 7,
        Ear = 8,
        Neck = 9,
        Wrist = 10,
        Ring1 = 11,
        Ring2 = 12,
        SoulCrystal = 13,
    }
}
