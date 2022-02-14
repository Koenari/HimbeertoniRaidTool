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
        public GearItem Weapon { get => Items[0]; set => Items[0] = value; }
        public GearItem Head { get => Items[1]; set => Items[1] = value; }
        public GearItem Body { get => Items[2]; set => Items[2] = value; }
        public GearItem Gloves { get => Items[3]; set => Items[3] = value; }
        public GearItem Legs { get => Items[4]; set => Items[4] = value; }
        public GearItem Feet { get => Items[5]; set => Items[5] = value; }
        public GearItem Earrings { get => Items[6]; set => Items[6] = value; }
        public GearItem Necklace { get => Items[7]; set => Items[7] = value; }
        public GearItem Bracelet { get => Items[8]; set => Items[8] = value; }
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
        public bool FillStats()
        {

            bool result = true;
            List<bool> tasks = new();
            if (Weapon != null && Weapon.Name == "")
                tasks.Add(GetGearStats(Weapon));
            if (Head != null && Head.Name == "")
                tasks.Add(GetGearStats(Head));
            if (Body != null && Body.Name == "")
                tasks.Add(GetGearStats(Body));
            if (Gloves != null && Gloves.Name == "")
                tasks.Add(GetGearStats(Gloves));
            if (Legs != null && Legs.Name == "")
                tasks.Add(GetGearStats(Legs));
            if (Feet != null && Feet.Name == "")
                tasks.Add(GetGearStats(Feet));
            if (Earrings != null && Earrings.Name == "")
                tasks.Add(GetGearStats(Earrings));
            if (Necklace != null && Necklace.Name == "")
                tasks.Add(GetGearStats(Necklace));
            if (Bracelet != null && Bracelet.Name == "")
                tasks.Add(GetGearStats(Bracelet));
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
    
}
