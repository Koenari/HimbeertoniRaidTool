using System.Collections;
using System.Collections.Generic;

namespace HimbeertoniRaidTool.Data
{
    public class Loot
    {
        private List<GearItem> loot = new();


    }
    public struct LootSource
    {
        public static implicit operator LootSource((RaidTier, int) data) => new(data);
        public static implicit operator LootSource((RaidTier, int)[] data) => new(data);
        public List<(RaidTier, int)> Sources;
        public LootSource(params (RaidTier, int)[] data) { Sources = new(data); }

        public override bool Equals(object? obj)
        {
            if (obj is null || !obj.GetType().IsAssignableTo(GetType()))
                return false;
            return Equals((LootSource)obj);
        }
        public bool Equals(LootSource obj) => Sources.Contains(obj.Sources[0]);

        public override int GetHashCode() => base.GetHashCode();
    }
    
    public class LootDB
    {
        
        public static List<GearItem> GetPossibleLoot(RaidTier raidTear, int boss)
        {
            return new();
        }
    }

    public struct ItemIDRange
    {
        public static implicit operator ItemIDRange(int id) => new(id, id);
        public static implicit operator ItemIDRange((int, int) id) => new(id.Item1, id.Item2);
        public static implicit operator ItemIDRange(KeyValuePair<int, int> id) => new(id.Key, id.Value);
        private readonly int StartID;
        private readonly int EndID;
        private bool InRange(int id) => StartID <= id && id <= EndID;
        public ItemIDRange(int start, int end) => (StartID, EndID) = (start, end);
        public override bool Equals(object? obj)
        {
            if (obj == null || !obj.GetType().IsAssignableTo(typeof(ItemIDRange)))
                return false;
            return Equals((ItemIDRange)obj);
        }
        public bool Equals(int obj) => this == obj;
        public bool Equals(ItemIDRange obj) => StartID == obj.StartID && EndID == obj.EndID;
        public override int GetHashCode() => (StartID, EndID).GetHashCode(); 
        public static bool operator ==(ItemIDRange left, int right) => left.InRange(right);
        public static bool operator !=(ItemIDRange left, int right) => !left.InRange(right);

    }
}
