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
        public Weapon? Weapon { get; set; }
        public GearItem? Head { get; set; }
        public GearItem? Body { get; set; }
        public GearItem? Gloves { get; set; }
        public GearItem? Legs { get; set; }
        public GearItem? Feet { get; set; }
        public GearItem? Earrings { get; set; }
        public GearItem? Necklace { get; set; }
        public GearItem? Bracelet { get; set; }
        public GearItem? Ring1 { get; set; }
        public GearItem? Ring2 { get; set; }

        public async Task FillStats(GearConnector db)
        {
            if (Weapon != null && Weapon.name == "")
                _ = db.GetGearStats(Weapon);
            if (Head != null && Head.name == "")
                _ = db.GetGearStats(Head);
            if (Body != null && Body.name == "")
                _ = db.GetGearStats(Body);
            if (Gloves != null && Gloves.name == "")
                _ = db.GetGearStats(Gloves);
            if (Legs != null && Legs.name == "")
                _ = db.GetGearStats(Legs);
            if (Feet != null && Feet.name == "")
                _ = db.GetGearStats(Feet);
            if (Earrings != null && Earrings.name == "")
                _ = db.GetGearStats(Earrings);
            if (Necklace != null && Necklace.name == "")
                _ = db.GetGearStats(Necklace);
            if (Bracelet != null && Bracelet.name == "")
                _ = db.GetGearStats(Bracelet);
            if (Ring1 != null && Ring1.name == "")
                _ = db.GetGearStats(Ring1);
            if (Ring2 != null && Ring2.name == "")
                await db.GetGearStats(Ring2);

        }
    }
    
}
