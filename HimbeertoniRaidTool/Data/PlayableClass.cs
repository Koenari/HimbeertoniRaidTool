using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PlayableClass
    {
        private static readonly ExcelSheet<ClassJob> _classJobSheet = Services.DataManager.GetExcelSheet<ClassJob>()!;
        [JsonProperty("Job")]
        public Job Job;
        [JsonIgnore]
        public ClassJob ClassJob => _classJobSheet.GetRow((uint)Job)!;
        public Character? Parent { get; private set; }
        [JsonProperty("Level")]
        public int Level = 1;
        [JsonProperty("Gear")]
        public GearSet Gear;
        [JsonProperty("BIS")]
        public GearSet BIS;
        [JsonConstructor]
        private PlayableClass()
        {
            Gear = new GearSet();
            BIS = new GearSet();
        }
        public PlayableClass(Job job, Character c)
        {
            Job = job;
            Gear = new(GearSetManager.HRT, c, Job);
            Services.HrtDataManager.GetManagedGearSet(ref Gear);
            BIS = new(GearSetManager.HRT, c, Job, "BIS");
            Services.HrtDataManager.GetManagedGearSet(ref BIS);
        }
        public (GearItem, GearItem) this[GearSetSlot slot]
        {
            get
            {
                GearSetSlot slot2 = slot;
                if (slot is GearSetSlot.Ring1 or GearSetSlot.Ring2)
                {
                    if (Gear[GearSetSlot.Ring2].Equals(BIS[GearSetSlot.Ring1], ItemComparisonMode.IdOnly)
                        || Gear[GearSetSlot.Ring1].Equals(BIS[GearSetSlot.Ring2], ItemComparisonMode.IdOnly))
                        slot2 = slot == GearSetSlot.Ring1 ? GearSetSlot.Ring2 : GearSetSlot.Ring1;
                }
                return (Gear[slot], BIS[slot2]);
            }
        }
        public IEnumerable<(GearSetSlot, (GearItem, GearItem))> ItemTuples
        {
            get
            {
                foreach (GearSetSlot slot in GearSet.Slots)
                    yield return (slot, this[slot]);
            }
        }
        /// <summary>
        /// Evaluates if all of the given slots have BiS item or an item with higher or euqla item level as given item
        /// </summary>
        /// <param name="slots">List of slots to evaluate</param>
        /// <param name="toCompare">Item to compare to items in slots</param>
        /// <returns>True if all slots are BiS or better</returns>
        public bool HaveBisOrHigherItemLevel(IEnumerable<GearSetSlot> slots, GearItem toCompare) => SwappedCompare((item, bis) => BisOrBetterComparer(item, bis, toCompare), slots);
        /// <summary>
        /// Evaluates if all given slots already are equipped with Best in Slot
        /// </summary>
        /// <param name="slots">List of slots to check</param>
        /// <returns>True if all slots have BiS</returns>
        public bool HaveBis(IEnumerable<GearSetSlot> slots) => SwappedCompare(BisComparer, slots);
        private bool SwappedCompare(Func<GearItem, GearItem, bool> comparer, IEnumerable<GearSetSlot> slots)
        {
            if (slots.Contains(GearSetSlot.Ring1) && slots.Contains(GearSetSlot.Ring2))
                return (
                    (SwappedCompare(comparer, GearSetSlot.Ring1, true, false) && SwappedCompare(comparer, GearSetSlot.Ring2, true, false))
                        || (SwappedCompare(comparer, GearSetSlot.Ring1, true, true) && SwappedCompare(comparer, GearSetSlot.Ring2, true, true)))
                    && slots.Where(slot => !(slot is GearSetSlot.Ring1 or GearSetSlot.Ring2)).All(slot => SwappedCompare(comparer, slot));
            return slots.All(slot => SwappedCompare(comparer, slot));
        }
        private bool SwappedCompare(Func<GearItem, GearItem, bool> comparer, GearSetSlot slot, bool explicitSwaps = false, bool ringsSwapped = false)
        {
            if (!explicitSwaps)
                return comparer(Gear[slot], BIS[slot])
                    || (slot == GearSetSlot.Ring1 && comparer(Gear[slot], BIS[GearSetSlot.Ring2]))
                    || (slot == GearSetSlot.Ring2 && comparer(Gear[slot], BIS[GearSetSlot.Ring1]));
            //Explicit swaps from here
            if (!ringsSwapped || (slot != GearSetSlot.Ring1 && slot != GearSetSlot.Ring2))
                return comparer(Gear[slot], BIS[slot]);
            else if (slot == GearSetSlot.Ring1)
                return comparer(Gear[slot], BIS[GearSetSlot.Ring2]);
            else if (slot == GearSetSlot.Ring2)
                return comparer(Gear[slot], BIS[GearSetSlot.Ring1]);
            else
                return false;
        }
        private static bool BisOrBetterComparer(GearItem item, GearItem bis, GearItem comp) => BisComparer(item, bis) || HigerILvlComparer(item, comp);
        private static bool HigerILvlComparer(GearItem item, GearItem comp) => item.ItemLevel >= comp.ItemLevel;
        private static bool BisComparer(GearItem item, GearItem bis) => item.ID == bis.ID;
        public int GetCurrentStat(StatType type) => GetStat(type, Gear);
        public int GetBiSStat(StatType type) => GetStat(type, BIS);
        private int GetStat(StatType type, GearSet set)
        {
            type = type switch
            {
                StatType.AttackMagicPotency => Job.MainStat(),
                StatType.HealingMagicPotency => StatType.Mind,
                StatType.AttackPower => Job.MainStat(),
                _ => type,
            };
            return AllaganLibrary.GetStatWithModifiers(type, set.GetStat(type), Level, Job, Parent?.Tribe);
        }
        internal void SetParent(Character c)
        {
            string testString = string.Format("{0:X}-{1:X}", c.HomeWorldID, c.Name.ConsistentHash());
            if (Gear.HrtID.StartsWith(testString))
                Parent = c;
        }
        public bool IsEmpty => Level == 0 && Gear.IsEmpty && BIS.IsEmpty;
        public void ManageGear()
        {
            Services.HrtDataManager.GetManagedGearSet(ref Gear);
            Services.HrtDataManager.GetManagedGearSet(ref BIS);
        }
        public bool Equals(PlayableClass? other)
        {
            if (other == null)
                return false;
            return Job == other.Job;
        }
        public override string ToString() => $"{Job} ({Level})";
    }
}
