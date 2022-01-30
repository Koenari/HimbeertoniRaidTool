using HimbeertoniRaidTool.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    class GearItem
    {
        private readonly int ID;
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public int itemLevel { get; set; }

        public GearItem(int idArg)
        {
            this.ID = idArg;
        }

        private void RetrieveItemData()
        {
            GearConnector con = new EtroConnector();
            con.GetGearStats(this);
        }

        public int getID()
        {
            return this.ID;
        }
    }
}
