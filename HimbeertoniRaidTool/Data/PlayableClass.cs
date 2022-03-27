using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    public class PlayableClass
    {
        public PlayableClass(AvailableClasses ClassNameArg)
        {
            ClassType = ClassNameArg;
        }
        public AvailableClasses ClassType;
        public int Level = 0;
        public GearSet Gear = new(GearSetManager.HRT);
        public GearSet BIS = new(GearSetManager.Etro);

        [JsonIgnore]
        public bool IsEmpty => Level == 0 && Gear.IsEmpty && BIS.IsEmpty;


        public bool Equals(PlayableClass? other)
        {
            if (other == null)
                return false;
            return ClassType == other.ClassType;
        }
    }
}