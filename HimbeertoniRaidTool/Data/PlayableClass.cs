namespace HimbeertoniRaidTool.Data
{
    public class PlayableClass
    {
        public PlayableClass(AvailableClasses ClassNameArg)
        {
            ClassType = ClassNameArg;
        }
        public AvailableClasses ClassType;
        public int Level;
        public GearSet Gear = new();
        public GearSet BIS = new(GearSetManager.Etro);
        public GearSet AltBIS = new(GearSetManager.Etro);

        public bool Equals(PlayableClass? other)
        {
            if (other == null)
                return false;
            return ClassType == other.ClassType;
        }
    }
}