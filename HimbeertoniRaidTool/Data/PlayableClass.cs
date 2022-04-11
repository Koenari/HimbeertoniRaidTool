using Newtonsoft.Json;
using static HimbeertoniRaidTool.DataManagement.DataManager;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PlayableClass
    {
        [JsonProperty("ClassType")]
        public AvailableClasses ClassType;
        [JsonProperty("Level")]
        public int Level = 0;
        [JsonProperty("Gear")]
        public GearSet Gear;
        [JsonProperty("BIS")]
        public GearSet BIS;
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
