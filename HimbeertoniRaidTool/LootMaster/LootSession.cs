using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Data;
using static Dalamud.Localization;


namespace HimbeertoniRaidTool.LootMaster
{
    public class LootSession
    {
        private Random Random = new Random(Guid.NewGuid().GetHashCode());
        public Dictionary<Player, int> Rolls;
        public LootRuling RulingOptions { get; private set; }
        internal readonly (HrtItem, int)[] Loot;
        private readonly RaidGroup _group;
        public Dictionary<(HrtItem, int), List<(Player, string)>> Results;
        public List<Player> Excluded = new();
        private int NumLootItems => Loot.Aggregate(0, (sum, x) => sum + x.Item2);
        public LootSession(RaidGroup group, LootRuling rulingOptions, (HrtItem, int)[] items)
        {
            RulingOptions = rulingOptions.Clone();
            Loot = items;
            _group = group;
            Rolls = new();
            foreach (var p in _group.Players)
                Rolls.Add(p, Random.Next(0, 101));
            Results = new();
        }
        public void EvaluateAll(bool reevaluate = false)
        {
            if (Results.Count == NumLootItems && !reevaluate)
                return;
            Results.Clear();
            foreach (var item in Loot)
                for (int i = 0; i < item.Item2; i++)
                {
                    if (item.Item1.IsExhangableItem)
                        Results.Add((item.Item1, i), Evaluate(new ExchangableItem(item.Item1.ID).PossiblePurchases, Excluded));
                    else if (item.Item1.IsContainerItem)
                        Results.Add((item.Item1, i), Evaluate(new ContainerItem(item.Item1.ID).PossiblePurchases, Excluded));
                    else if (item.Item1.IsGear)
                        Results.Add((item.Item1, i), Evaluate(new List<GearItem> { new(item.Item1.ID) }, Excluded));
                    else
                        Results.Add((item.Item1, i), new());
                }
        }
        private List<(Player, string)> Evaluate(List<GearItem> possibleItems, List<Player> excludeAddition)
        {
            List<Player> excluded = new();
            excluded.AddRange(Excluded);
            excluded.AddRange(excludeAddition);
            List<Player> need = new();
            List<Player> greed = new();
            foreach (Player p in _group.Players)
            {
                if (excluded.Contains(p))
                    continue;
                foreach (GearItem item in possibleItems)
                    if (p.Gear[item.Slot].ID != p.BIS[item.Slot].ID)
                    {
                        need.Add(p);
                        break;
                    }
                if (!need.Contains(p))
                    greed.Add(p);
            }
            LootRulingComparer comparer = GetComparer(possibleItems);
            need.Sort(comparer);
            List<(Player, string)> result = new();
            for (int i = 0; i < need.Count - 1; i++)
            {
                result.Add((need[i],
                    comparer.RulingReason.GetValueOrDefault((need[i], need[i + 1]), new()).ToString()));
            }
            if (need.Count > 0)
                result.Add((need[^1], Localize("Need > Greed", "Need > Greed")));
            foreach (Player p in greed)
            {
                result.Add((p, Localize("Greed", "Greed")));
            }
            return result;
        }
        private LootRulingComparer GetComparer(List<GearItem> possibleItems) => new(this, possibleItems);
        private class LootRulingComparer : IComparer<Player>
        {
            private readonly LootSession _session;
            private readonly List<GearItem> _possibleItems;
            public Dictionary<(Player, Player), LootRule> RulingReason = new();
            public LootRulingComparer(LootSession session, List<GearItem> possibleItems)
                => (_session, _possibleItems) = (session, possibleItems);

            public int Compare(Player? x, Player? y)
            {
                if (x is null || y is null)
                    return 0;
                if (RulingReason.ContainsKey((x, y)))
                    return RulingReason[(x, y)].Compare(x, y, _session, _possibleItems);
                foreach (LootRule rule in _session.RulingOptions.RuleSet)
                {
                    int result = rule.Compare(x, y, _session, _possibleItems);
                    if (result != 0)
                    {
                        RulingReason.Add((x, y), rule);
                        RulingReason.Add((y, x), rule);
                        return result;
                    }
                }
                RulingReason.Add((x, y), new());
                return 0;
            }
        }
    }

}
