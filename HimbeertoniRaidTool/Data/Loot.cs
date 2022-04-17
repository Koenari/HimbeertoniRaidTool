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
        private readonly static Dictionary<(RaidTier, int), List<HrtItem>> LootSourceDB;

        static LootDB()
        {
            LootSourceDB = new();
            foreach (var entry in CuratedData.LootSourceDB)
            {
                foreach ((RaidTier, int) source in entry.Value.Sources)
                {
                    if (!LootSourceDB.ContainsKey(source))
                        LootSourceDB.Add(source, new());
                    foreach (uint id in entry.Key.AsList)
                        LootSourceDB[source].Add(new(id));


                }
            }
        }

        public static List<HrtItem> GetPossibleLoot(RaidTier raidTear, int boss) => LootSourceDB.GetValueOrDefault((raidTear, boss), new());
    }
}
