using System;
using System.Collections.Generic;
using static HimbeertoniRaidTool.Connectors.EtroConnector;

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
            for (int i = 0; i  < NumSlots; i++)
            {
                Items[i] = new(0);
            }
        }
        public int GetItemLevel()
        {
            int itemLevel = 0;
            int numItems = 0;
            for(int i = 0; i < NumSlots; i++)
            {
                if (Items[i] != null && Items[i].ItemLevel > 0)
                {
                    itemLevel += Items[i].ItemLevel;
                    numItems++;
                }
            }
            return (int)(itemLevel / (numItems > 0 ? numItems : 1));

        }
        public GearItem Get(GearSetSlot slot)
        {

            switch (slot)
            {
                case GearSetSlot.MainHand:
                    return MainHand;
                case GearSetSlot.OffHand:
                    return OffHand;
                case GearSetSlot.Head:
                    return Head;
                case GearSetSlot.Body:
                    return Body;
                case GearSetSlot.Hands:
                    return Hands;
                case GearSetSlot.Legs:
                    return Legs;
                case GearSetSlot.Feet:
                    return Feet;
                case GearSetSlot.Ear:
                    return Ear;
                case GearSetSlot.Neck:
                    return Neck;
                case GearSetSlot.Wrist:
                    return Wrist;
                case GearSetSlot.Ring1:
                    return Ring1;
                case GearSetSlot.Ring2:
                    return Ring2;
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
            foreach(bool t in tasks)
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
