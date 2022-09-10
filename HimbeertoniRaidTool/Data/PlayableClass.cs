using System;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.DataManagement.DataManager;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PlayableClass
    {
        [JsonProperty("ClassType")]
        [Obsolete]
        public AvailableClasses? classType;
        [JsonProperty("Job")]
        public Job Job;
        [JsonIgnore]
        public ClassJob ClassJob => Services.DataManager.GetExcelSheet<ClassJob>()!.GetRow((uint)Job)!;
        private Character? _parent;
        [JsonProperty("Level")]
        public int Level = 1;
        [JsonProperty("Gear")]
        public GearSet Gear;
        [JsonProperty("BIS")]
        public GearSet BIS;
        [JsonConstructor]
        [Obsolete]
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
        public int GetCurrentStat(StatType type)
        {
            return AllaganLibrary.GetStatWithModifiers(type, Gear.GetStat(type), Level, Job, _parent?.Tribe);
        }
        public int GetBiSStat(StatType type)
        {
            return AllaganLibrary.GetStatWithModifiers(type, BIS.GetStat(type), Level, Job, _parent?.Tribe);
        }
        internal void SetParent(Character c)
        {
            string testString = string.Format("{0:X}-{1:X}", c.HomeWorldID, c.Name.ConsistentHash());
            if (Gear.HrtID.StartsWith(testString))
                _parent = c;
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
