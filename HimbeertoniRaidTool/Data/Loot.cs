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
        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < Sources.Count - 1; i++)
            {
                result += $"{Sources[i].Item1.Name} Boss {Sources[i].Item2}";
            }
            result += $"{Sources.Last().Item1.Name} Boss {Sources.Last().Item2}";
            return result;
        }

        public static bool operator ==(LootSource left, LootSource right) => left.Equals(right);
        public static bool operator !=(LootSource left, LootSource right) => !left.Equals(right);
    }

    public static class LootDB
    {
        private static readonly Dictionary<(RaidTier, int), List<HrtItem>> LootSourceDB;

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
        public static List<HrtItem> GetPossibleLoot(LootSource source)
        {
            if (!source.IsList)
                return GetPossibleLoot(source.Sources.First());
            List<HrtItem> result = new List<HrtItem>();
            foreach (var entry in source.Sources)
                if (LootSourceDB.TryGetValue(entry, out List<HrtItem>? loot))
                    result.AddRange(loot);
            return result.Distinct().ToList();
        }
        public static List<HrtItem> GetPossibleLoot((RaidTier raidTear, int boss) source) =>
            LootSourceDB.GetValueOrDefault((source.raidTear, source.boss), new());
    }
}
