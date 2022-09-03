using System;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.DataManagement.DataManager;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PlayableClass
    {
        [JsonProperty("ClassType")]
        public AvailableClasses? classType;
        [JsonProperty("Job")]
        public Job Job;
        [JsonProperty("Level")]
        public int Level = 1;
        [JsonProperty("Gear")]
        public GearSet Gear;
        [JsonProperty("BIS")]
        public GearSet BIS;
        public PlayableClass(Job classType)
        {
            Job = classType;
            Gear = new();
            BIS = new();
        }
        [JsonConstructor]
        private PlayableClass(AvailableClasses? classType)
        {
            this.classType = classType;
            if (classType != null)
                Job = Enum.Parse<Job>(classType.Value.ToString());
            this.classType = null;
            Gear = new();
            BIS = new();
        }
        public PlayableClass(Job ClassNameArg, Character c)
        {
            Job = ClassNameArg;
            Gear = new(GearSetManager.HRT, c, Job);
            GetManagedGearSet(ref Gear);
            BIS = new(GearSetManager.HRT, c, Job, "BIS");
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
            return Job == other.Job;
        }
    }
}
