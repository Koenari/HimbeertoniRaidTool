using System.Collections.Generic;
using HimbeertoniRaidTool.Data;

namespace HimbeertoniRaidTool.HrtServices
{
    internal class GameInfo
    {
        private readonly GameExpansion[] Expansions = new GameExpansion[6];
        private readonly Dictionary<uint, InstanceWithLoot> InstanceDB;
        public GameExpansion CurrentExpansion => Expansions[5];
        public GameInfo(CuratedData curatedData)
        {
            InstanceDB = curatedData.InstanceDB;
            Expansions[5] = curatedData.CurrentExpansion;
        }
        public InstanceWithLoot GetInstance(uint instanceID) => InstanceDB[instanceID];
    }
}
