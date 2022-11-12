using System;
using System.Collections;
using System.Collections.Generic;
using Lumina.Excel.Extensions;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn, MissingMemberHandling = MissingMemberHandling.Ignore)]
    public class GearSet : IEnumerable<GearItem>
    {
        [JsonProperty("TimeStamp")]
        public DateTime? TimeStamp;
        [JsonProperty("EtroID")]
        public string EtroID = "";
        [JsonProperty("LastEtroFetched")]
        public DateTime EtroFetchDate;
        [JsonProperty("HrtID")]
        public string HrtID = "";
        [JsonProperty("Name")]
        public string Name = "";
        [JsonProperty("ManagedBy")]
        public GearSetManager ManagedBy;
        [JsonIgnore]
        private const int NumSlots = 12;
        [JsonIgnore]
        private readonly GearItem[] Items = new GearItem[NumSlots];
        [JsonProperty]
        public GearItem MainHand { get => this[0]; set => this[0] = value; }
        [JsonProperty]
        public GearItem Head { get => this[1]; set => this[1] = value; }
        [JsonProperty]
        public GearItem Body { get => this[2]; set => this[2] = value; }
        [JsonProperty]
        public GearItem Hands { get => this[3]; set => this[3] = value; }
        [JsonProperty]
        public GearItem Legs { get => this[4]; set => this[4] = value; }
        [JsonProperty]
        public GearItem Feet { get => this[5]; set => this[5] = value; }
        [JsonProperty]
        public GearItem Ear { get => this[6]; set => this[6] = value; }
        [JsonProperty]
        public GearItem Neck { get => this[7]; set => this[7] = value; }
        [JsonProperty]
        public GearItem Wrist { get => this[8]; set => this[8] = value; }
        [JsonProperty]
        public GearItem Ring1 { get => this[9]; set => this[9] = value; }
        [JsonProperty]
        public GearItem Ring2 { get => this[10]; set => this[10] = value; }
        [JsonProperty]
        public GearItem OffHand { get => this[11]; set => this[11] = value; }
        public bool IsEmpty => Array.TrueForAll(Items, x => x.ID == 0);
        public int ItemLevel
        {
            get
            {
                return ILevelCache ??= Calc();
                int Calc()
                {
                    uint itemLevel = 0;
                    for (int i = 0; i < NumSlots; i++)
                    {
                        if (Items[i] != null && Items[i].ItemLevel > 0)
                        {
                            itemLevel += Items[i].ItemLevel;
                            if (Items[i].Item?.EquipSlotCategory.Value?.Disallows(GearSetSlot.OffHand) ?? false)
                                itemLevel += Items[i].ItemLevel;
                        }
                    }
                    return (int)((float)itemLevel / NumSlots);
                }
            }
        }
        //Caches
        [JsonIgnore]
        private int? ILevelCache = null;
        public GearSet()
        {
            ManagedBy = GearSetManager.HRT;
            Clear();
        }
        public GearSet(GearSetManager manager, Character c, Job ac, string name = "HrtCurrent")
        {
            ManagedBy = manager;
            Name = name;
            if (ManagedBy == GearSetManager.HRT)
                HrtID = GenerateID(c, ac, this);
            Clear();
        }
        public void Clear()
        {
            for (int i = 0; i < NumSlots; i++)
            {
                this[i] = new(0);
            }
        }
        public GearItem this[GearSetSlot slot]
        {
            get => this[ToIndex(slot)];
            set => this[ToIndex(slot)] = value;
        }
        private GearItem this[int idx]
        {
            get => Items[idx];
            set
            {
                Items[idx] = value;
                InvalidateCaches();
            }
        }
        private void InvalidateCaches()
        {
            ILevelCache = null;
        }
        public bool Contains(HrtItem item) => Array.Exists(Items, x => x.Equals(item));
        public bool ContainsExact(GearItem item) => Array.Exists(Items, x => x.Equals(item, ItemComparisonMode.Full));
        /*
         * Caching stats is a problem since this needs to be invalidated when changing materia
         * At the moment all mechanisms to change materia replace the item but it could lead to an invalid state in theory
         */
        public int GetStat(StatType type)
        {
            int result = 0;
            Array.ForEach(Items, x => result += x.GetStat(type));
            return result;
        }
        private static int ToIndex(GearSetSlot slot)
        {
            return slot switch
            {
                GearSetSlot.MainHand => 0,
                GearSetSlot.OffHand => 11,
                GearSetSlot.Head => 1,
                GearSetSlot.Body => 2,
                GearSetSlot.Hands => 3,
                GearSetSlot.Legs => 4,
                GearSetSlot.Feet => 5,
                GearSetSlot.Ear => 6,
                GearSetSlot.Neck => 7,
                GearSetSlot.Wrist => 8,
                GearSetSlot.Ring1 => 9,
                GearSetSlot.Ring2 => 10,
                _ => throw new IndexOutOfRangeException("GearSlot" + slot.ToString() + "does not exist"),
            };
        }

        internal void CopyFrom(GearSet gearSet)
        {
            TimeStamp = gearSet.TimeStamp;
            EtroID = gearSet.EtroID;
            HrtID = gearSet.HrtID;
            Name = gearSet.Name;
            ManagedBy = gearSet.ManagedBy;
            gearSet.Items.CopyTo(Items, 0);
            InvalidateCaches();
        }
        public void UpdateID(Character c, Job ac) => HrtID = GenerateID(c, ac, this);
        public static string GenerateID(Character c, Job ac, GearSet g)
        {
            string result = "";
            result += string.Format("{0:X}-{1:X}-{2}-{3:X}", c.HomeWorldID, c.Name.ConsistentHash(), ac, g.Name.ConsistentHash());

            return result;

        }

        public IEnumerator<GearItem> GetEnumerator()
        {
            return ((IEnumerable<GearItem>)Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}

