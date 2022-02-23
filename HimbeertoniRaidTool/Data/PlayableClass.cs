using System;

namespace HimbeertoniRaidTool.Data
{
    public class PlayableClass
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
            return ClassType == other.ClassType;
        }
    }
}