using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            => (obj is LootSource lootSource) && Equals(lootSource);

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
    public readonly struct Loot
    {
        public readonly ReadOnlyCollection<HrtItem> PossibleItems { get; }
        public readonly ReadOnlyCollection<HrtItem> GuaranteedItems { get; }

        public Loot(IList<HrtItem> guaranteedLoot, IList<HrtItem> possibleLoot)
        {
            PossibleItems = new(possibleLoot);
            GuaranteedItems = new(guaranteedLoot);
        }
    }

    public static class LootDB
    {
        private static readonly Dictionary<(RaidTier, int), Loot> LootSourceDB;

        static LootDB()
        {
            var PossibleDB = new Dictionary<(RaidTier, int), List<HrtItem>>();
            var GuaranteedDB = new Dictionary<(RaidTier, int), List<HrtItem>>();
            LootSourceDB = new();
            foreach (var entry in CuratedData.PossibleLootSourceDB)
            {
                foreach ((RaidTier, int) source in entry.Value.Sources)
                {
                    if (!PossibleDB.ContainsKey(source))
                        PossibleDB.Add(source, new());
                    foreach (uint id in entry.Key)
                        PossibleDB[source].Add(new(id));


                }
            }
            foreach (var entry in CuratedData.GuaranteedLootSourceDB)
            {
                foreach ((RaidTier, int) source in entry.Value.Sources)
                {
                    if (!GuaranteedDB.ContainsKey(source))
                        GuaranteedDB.Add(source, new());
                    foreach (uint id in entry.Key)
                        GuaranteedDB[source].Add(new(id));


                }
            }
            foreach (var entry in PossibleDB)
            {
                GuaranteedDB.TryGetValue(entry.Key, out List<HrtItem>? gI);
                LootSourceDB[entry.Key] = new(gI ?? Enumerable.Empty<HrtItem>().ToList(), entry.Value);
            }
        }
        public static IEnumerable<HrtItem> GetPossibleLoot(LootSource source)
        {
            if (!source.IsList)
                return GetPossibleLoot(source.Sources.First());
            List<HrtItem> result = new();
            foreach (var entry in source.Sources)
                if (LootSourceDB.TryGetValue(entry, out Loot loot))
                    result.AddRange(loot.PossibleItems);
            return result.Distinct();
        }
        public static IEnumerable<HrtItem> GetPossibleLoot((RaidTier raidTear, int boss) source) =>
            LootSourceDB.GetValueOrDefault((source.raidTear, source.boss), new()).PossibleItems;
        public static IEnumerable<HrtItem> GetGuaranteedLoot((RaidTier raidTear, int boss) source) =>
            LootSourceDB.GetValueOrDefault((source.raidTear, source.boss), new()).GuaranteedItems;
    }
}
