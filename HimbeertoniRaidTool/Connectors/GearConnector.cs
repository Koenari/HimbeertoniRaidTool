using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Data;
namespace HimbeertoniRaidTool.Connectors
{
    public interface GearConnector
    {
        public Task<bool> GetGearStats(GearItem item);
    }
}
