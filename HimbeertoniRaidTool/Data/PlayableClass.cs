using Newtonsoft.Json;
using static HimbeertoniRaidTool.DataManagement.DataManager;

namespace HimbeertoniRaidTool.Data
{
    public class PlayableClass
    {
        [JsonConstructor]
        public PlayableClass(AvailableClasses classType)
        {
            ClassType = classType;
            Gear = new();
            BIS = new();
        }
        public PlayableClass(AvailableClasses ClassNameArg, Character c)
        {
            ClassType = ClassNameArg;
            Gear = new(GearSetManager.HRT, c, ClassType);
            GetManagedGearSet(ref Gear);
            BIS = new(GearSetManager.HRT, c, ClassType, "BIS");
            GetManagedGearSet(ref BIS);
        }
        public AvailableClasses ClassType;
        public int Level = 0;
        public GearSet Gear;
        public GearSet BIS;


        [JsonIgnore]
        public bool IsEmpty => Level == 0 && Gear.IsEmpty && BIS.IsEmpty;

        public void ManageGear()
        {
            GetManagedGearSet(ref Gear);
            GetManagedGearSet(ref BIS);
        }

        public bool Equals(PlayableClass? other)
        {
            if (other == null)
                return false;
            return ClassType == other.ClassType;
        }
    }
}
