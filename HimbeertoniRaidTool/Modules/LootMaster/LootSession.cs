using System;
using System.Collections.Generic;
using System.Linq;
using HimbeertoniRaidTool.Data;
using static Dalamud.Localization;


namespace HimbeertoniRaidTool.Modules.LootMaster
{
    public class LootSession
    {
        private readonly Random Random = new(Guid.NewGuid().GetHashCode());
        public Dictionary<Player, int> Rolls = new();
        public LootRuling RulingOptions { get; private set; }
        internal readonly (HrtItem item, int count)[] Loot;
        private RaidGroup _group;
        internal RaidGroup Group
        {
            get => _group;
            set
            {
                _group = value;
                ReRoll();
                Results.Clear();
            }
        }
        public Dictionary<(HrtItem, int), List<(Player, string)>> Results = new();
        public List<Player> Excluded = new();
        public readonly RolePriority RolePriority;
        internal int NumLootItems => Loot.Aggregate(0, (sum, x) => sum + x.count);
        public LootSession(RaidGroup group, LootRuling rulingOptions, RolePriority rolePriority, (HrtItem, int)[] items)
        {
            RulingOptions = rulingOptions.Clone();
            Loot = items;
            _group = Group = group;
            RolePriority = rolePriority;
        }
        private void ReRoll()
        {
            foreach (var p in Group.Players)
                Rolls.Add(p, Random.Next(0, 101));
        }
        public void EvaluateAll(bool reevaluate = false)
        {
            if (Results.Count == NumLootItems && !reevaluate)
                return;
            Results.Clear();
            ReRoll();
            foreach ((HrtItem item, int count) in Loot)
                for (int i = 0; i < count; i++)
                {
                    if (item.IsExhangableItem)
                        Results.Add((item, i), Evaluate(new ExchangableItem(item.ID).PossiblePurchases, Excluded));
                    else if (item.IsContainerItem)
                        Results.Add((item, i), Evaluate(new ContainerItem(item.ID).PossiblePurchases, Excluded));
                    else if (item.IsGear)
                        Results.Add((item, i), Evaluate(new List<GearItem> { new(item.ID) }, Excluded));
                    else
                        Results.Add((item, i), new());
                }
        }
        private List<(Player, string)> Evaluate(List<GearItem> possibleItems, List<Player> excludeAddition)
        {
            List<Player> excluded = new();
            excluded.AddRange(Excluded);
            excluded.AddRange(excludeAddition);
            List<Player> need = new();
            List<Player> greed = new();
            foreach (var p in Group.Players)
            {
                if (excluded.Contains(p))
                    continue;
                foreach (var item in possibleItems)
                    if (p.Gear[item.Slot].ID != p.BIS[item.Slot].ID && !possibleItems.Any(x => x.ID == p.Gear[item.Slot].ID))
                    {
                        need.Add(p);
                        break;
                    }
                if (!need.Contains(p))
                    greed.Add(p);
            }
            var comparer = GetComparer(possibleItems);
            need.Sort(comparer);
            List<(Player, string)> result = new();
            for (int i = 0; i < need.Count - 1; i++)
            {
                result.Add((need[i],
                    comparer.RulingReason.TryGetValue((need[i], need[i + 1]), out var reasoning) ? $"{reasoning.rule} ({reasoning.valueL} over {reasoning.valueR})" : "None"));
            }
            if (need.Count > 0)
                result.Add((need[^1], Localize("Need > Greed", "Need > Greed")));
            foreach (var p in greed)
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
            public Dictionary<(Player, Player), (LootRule rule, int result, string valueL, string valueR)> RulingReason = new();
            public LootRulingComparer(LootSession session, List<GearItem> possibleItems)
                => (_session, _possibleItems) = (session, possibleItems);

            public int Compare(Player? x, Player? y)
            {
                if (x is null || y is null)
                    return 0;
                if (RulingReason.ContainsKey((x, y)))
                    return RulingReason[(x, y)].result;
                foreach (var rule in _session.RulingOptions.RuleSet)
                {
                    (int result, string forX, string forY) = rule.Compare(x, y, _session, _possibleItems);
                    if (result != 0)
                    {
                        RulingReason.Add((x, y), (rule, result, forX, forY));
                        RulingReason.Add((y, x), (rule, -result, forY, forX));
                        return result;
                    }
                }
                RulingReason.Add((x, y), (new(LootRuleEnum.None), 0, string.Empty, string.Empty));
                return 0;
            }
        }
    }

}
