using Dalamud.Configuration;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Data;
using System;
using System.Collections.Generic;
using static HimbeertoniRaidTool.Data.AvailableClasses;

namespace HimbeertoniRaidTool
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public Dictionary<AvailableClasses, string> DefaultBIS { get; set; } = new Dictionary<AvailableClasses, string>
        {
            { AST, "88647808-8a28-477b-b285-687bdcbff2d4" },
            { BLM, "327d090b-2d5a-4c3c-9eb9-8fd42342cce3" },
            { BLU, "3db73aab-2968-4eb7-b392-d524f5a1b783" },
            { BRD, "cec981af-25c7-4ffb-905e-3024411b797a" },
            { DNC, "fd333e44-0f90-42a6-a070-044b332bb54e" },
            { DRG, "8bdd42db-a318-41a0-8903-14efa5e0774b" },
            { DRK, "dda8aef5-41e4-40b6-813c-df306e1f1cee" },
            { GNB, "88fbea7d-3b43-479c-adb8-b87c9d6cb5f9" },
            { MCH, "6b4b1ba5-a821-41a0-b070-b1f50e986f85" },
            { MNK, "841ecfdb-41fe-44b4-8764-b3b08e223f8c" },
            { NIN, "b9876a4d-aba9-48f0-9c03-cb542af46a29" },
            { PLD, "38fe3778-f2c1-4300-99e4-b58a0445e969" },
            { RDM, "80fdec19-1109-4ca2-8172-53d4dda44144" },
            { RPR, "b301e789-96da-42f2-9628-95f68345e35b" },
            { SAM, "3a7c7f45-b715-465d-a377-db458045506a" },
            { SCH, "f1802c19-d766-40f0-b781-f5b965cb964e" },
            { SGE, "3c7d9741-0e74-41d7-9ec4-1b2e7c1673a5" },
            { SMN, "840a5088-23fa-49c5-a12a-3731ca55b4a6" },
            { WAR, "6d0d2d4d-a477-44ea-8002-862eca8ef91d" },
            { WHM, "e78a29e3-1dcf-4e53-bbcf-234f33b2c831" },
        };
        public LootRuling LootRuling { get; set; } = new LootRuling()
        {
            RuleSet = new()
            {
                LootRules.BISOverUpgrade,
                LootRules.ByPosition,
                LootRules.HighesItemLevelGain,
                LootRules.LowestItemLevel,
                LootRules.Random

            }
        };

        public RaidGroup? GroupInfo;
        // the below exist just to make saving 

        public void Save()
        {
            Services.PluginInterface.SavePluginConfig(this);
        }

        internal void UpdateRaidGroup(RaidGroup group)
        {
            this.GroupInfo = group;
            this.Save();
        }
    }
}
