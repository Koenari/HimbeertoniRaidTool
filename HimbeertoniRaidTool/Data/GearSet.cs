using HimbeertoniRaidTool.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class GearSet
    {
        public DateTime? TimeStamp;
        public string EtroID = "";
        private const int NumSlots = 12;
        private GearItem?[] Items = new GearItem?[NumSlots];
        public GearItem? Weapon { get => Items[0]; set => Items[0] = value; }
        public GearItem? Head { get => Items[1]; set => Items[1] = value; }
        public GearItem? Body { get => Items[2]; set => Items[2] = value; }
        public GearItem? Gloves { get => Items[3]; set => Items[3] = value; }
        public GearItem? Legs { get => Items[4]; set => Items[4] = value; }
        public GearItem? Feet { get => Items[5]; set => Items[5] = value; }
        public GearItem? Earrings { get => Items[6]; set => Items[6] = value; }
        public GearItem? Necklace { get => Items[7]; set => Items[7] = value; }
        public GearItem? Bracelet { get => Items[8]; set => Items[8] = value; }
        public GearItem? Ring1 { get => Items[9]; set => Items[9] = value; }
        public GearItem? Ring2 { get => Items[10]; set => Items[10] = value; }
        public GearItem? OffHand { get => Items[11]; set => Items[11] = value; }

        public int GetItemLevel()
        {
            int itemLevel = 0;
            int numItems = 0;
            for(int i = 0; i < NumSlots; i++)
            {
                if (Items[i] != null && Items[i].itemLevel > 0)
                {
                    itemLevel += Items[i].itemLevel;
                    numItems++;
                }
            }
            return (int)(itemLevel / (numItems > 0 ? numItems : 1));

        }
        public async Task<bool> FillStats(GearConnector db)
        {
            bool result = true;
            List<Task<bool>> tasks = new();
            if (Weapon != null && Weapon.name == "")
                tasks.Add(db.GetGearStats(Weapon));
            if (Head != null && Head.name == "")
                tasks.Add(db.GetGearStats(Head));
            if (Body != null && Body.name == "")
                tasks.Add(db.GetGearStats(Body));
            if (Gloves != null && Gloves.name == "")
                tasks.Add(db.GetGearStats(Gloves));
            if (Legs != null && Legs.name == "")
                tasks.Add(db.GetGearStats(Legs));
            if (Feet != null && Feet.name == "")
                tasks.Add(db.GetGearStats(Feet));
            if (Earrings != null && Earrings.name == "")
                tasks.Add(db.GetGearStats(Earrings));
            if (Necklace != null && Necklace.name == "")
                tasks.Add(db.GetGearStats(Necklace));
            if (Bracelet != null && Bracelet.name == "")
                tasks.Add(db.GetGearStats(Bracelet));
            if (Ring1 != null && Ring1.name == "")
                tasks.Add(db.GetGearStats(Ring1));
            if (Ring2 != null && Ring2.name == "")
                tasks.Add(db.GetGearStats(Ring2));
            foreach(Task<bool> t in tasks)
            {
                result = result & await t;
            }
            return result;
        }
    }
    
}
