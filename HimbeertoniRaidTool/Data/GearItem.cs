using HimbeertoniRaidTool.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class GearItem
    {
        public int ID { get; init; }
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public int itemLevel { get; set; } = -1;
        public GearSource Source { get; set; } = GearSource.undefined;

        public GearItem() : this(-1) { }

        public GearItem(int idArg)
        {
            this.ID = idArg;
        }

        internal void RetrieveItemData(GearConnector con)
        {
            if(ID > -1)
                con.GetGearStats(this);
        }

        public int GetID()
        {
            return this.ID;
        }
    }
    public enum GearSource
    {
        Raid,
        Tome,
        Crafted,
        undefined
    }

    public class Weapon : GearItem
    {
        public GearItem OffHand = new();

        public Weapon() : base() { }
        public Weapon(int id) : base(id) { }
        public Weapon(int id, GearItem oH) : base(id) 
        {
            OffHand = oH;
        }
        internal new void RetrieveItemData(GearConnector con)
        {
            if (OffHand.ID > -1)
                con.GetGearStats(this.OffHand);
            base.RetrieveItemData(con);
        }
    }
}
