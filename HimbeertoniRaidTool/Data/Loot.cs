using System;
using System.Collections.Generic;
using System.Linq;

namespace HimbeertoniRaidTool.Data
{
    public class LootSource
    {
        public static implicit operator LootSource((RaidTier, int) data) => new(data);
        public readonly List<(RaidTier, int)> Sources;
        public LootSource(RaidTier raidTier, int boss) : this((raidTier, boss)) { }
        public LootSource(params (RaidTier, int)[] data) { Sources = new(data); }
        public bool IsList => Sources.Count > 1;
        public override bool Equals(object? obj)
        {
            if (obj is null || !obj.GetType().IsAssignableTo(GetType()))
                return false;
            return Equals((LootSource)obj);
        }
        public bool Equals(LootSource obj) => Sources.Contains(obj.Sources[0]);

        public override int GetHashCode() => Sources.GetHashCode();

        public static bool operator ==(LootSource left, LootSource right) => left.Equals(right);
        public static bool operator !=(LootSource left, LootSource right) => !left.Equals(right);
    }

    public static class LootDB
    {
        private readonly static Dictionary<(RaidTier, int), List<GearItem>> LootSourceDB;

        static LootDB()
        {
            LootSourceDB = new();
            foreach (var entry in CuratedData.LootSourceDB)
            {
                foreach ((RaidTier, int) source in entry.Value.Sources)
                {
                    if (!LootSourceDB.ContainsKey(source))
                        LootSourceDB.Add(source, new());
                    foreach (uint id in entry.Key.Enumerator)
                        LootSourceDB[source].Add(new(id));
                }
            }
        }

        public static List<GearItem> GetPossibleLoot(RaidTier raidTear, int boss) => LootSourceDB.GetValueOrDefault((raidTear, boss), new());
    }

    public class ItemIDRange
    {
        public static implicit operator ItemIDRange(uint id) => new(id, id);
        public static implicit operator ItemIDRange((uint, uint) id) => new(id.Item1, id.Item2);
        public static implicit operator ItemIDRange(KeyValuePair<uint, uint> id) => new(id.Key, id.Value);
        private readonly uint StartID;
        private readonly uint EndID;
        private bool InRange(uint id) => StartID <= id && id <= EndID;
        public IEnumerable<uint> Enumerator => Enumerable.Range((int)StartID, (int)(EndID - StartID + 1)).ToList().ConvertAll(x => Convert.ToUInt32(x));
        public ItemIDRange(uint start, uint end) => (StartID, EndID) = (start, end);
        public override bool Equals(object? obj)
        {
            if (obj == null || !obj.GetType().IsAssignableTo(typeof(ItemIDRange)))
                return false;
            return Equals((ItemIDRange)obj);
        }
        public bool Equals(uint obj) => this == obj;
        public bool Equals(ItemIDRange obj) => StartID == obj.StartID && EndID == obj.EndID;
        public override int GetHashCode() => (StartID, EndID).GetHashCode();
        public static bool operator ==(ItemIDRange left, uint right) => left.InRange(right);
        public static bool operator !=(ItemIDRange left, uint right) => !left.InRange(right);

    }
}
