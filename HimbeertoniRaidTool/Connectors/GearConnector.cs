using System.Threading.Tasks;
using HimbeertoniRaidTool.Data;
namespace HimbeertoniRaidTool.Connectors
{
    public interface GearConnector
    {
        public Task<bool> GetGearStats(GearItem item);
        public Task<bool> GetGearSet(GearSet set);
    }
}
