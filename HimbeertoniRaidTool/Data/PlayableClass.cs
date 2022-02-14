﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class PlayableClass : IEquatable<PlayableClass>
    {
        public PlayableClass (AvailableClasses ClassNameArg)
        {
            this.ClassType = ClassNameArg;
        }
        public AvailableClasses ClassType;
        public GearSet Gear = new();
        public GearSet BIS = new();

        public bool Equals(PlayableClass? other)
        {
            if (other == null)
                return false;
            return this.ClassType == other.ClassType;
        }
    }

    public enum AvailableClasses
    {
        AST,
        BLM,
        BLU,
        BRD,
        DNC,
        DRG,
        DRK,
        GNB,
        MCH,
        MNK,
        NIN,
        PLD,
        RDM,
        RPR,
        SAM,
        SCH,
        SGE,
        SMN,
        WAR,
        WHM
    }
}