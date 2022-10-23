using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.Extensions;
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
        public Dictionary<(HrtItem, int), LootResult> Results = new();
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
            Rolls.Clear();
            foreach (var p in Group)
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
                    Results.Add((item, i), Evaluate(item, Excluded));
                }
        }
        private LootResult Evaluate(HrtItem droppedItem, IEnumerable<Player> excludeAddition)
        {
            List<Player> excluded = new();
            excluded.AddRange(Excluded);
            excluded.AddRange(excludeAddition);
            IEnumerable<GearItem> possibleItems;
            if (droppedItem.IsGear)
                possibleItems = new List<GearItem> { new(droppedItem.ID) };
            else if (droppedItem.IsContainerItem)
                possibleItems = new ContainerItem(droppedItem.ID).PossiblePurchases;
            else if (droppedItem.IsExhangableItem)
                possibleItems = new ExchangableItem(droppedItem.ID).PossiblePurchases;
            else
                return new();
            LootResult result = new();
            foreach (var p in Group)
            {
                if (excluded.Contains(p))
                    continue;
                //Pre filter items by job
                result.ApplicableItems[p] = possibleItems.Where(i => (i.Item?.ClassJobCategory.Value).Contains(p.MainChar.MainJob));
                result.NeededItems[p] = new List<GearItem>();
                //Calculate need for each item
                foreach (var item in result.ApplicableItems[p])

                    if (
                        //Always need if Bis and not aquired
                        (p.BIS.Contains(item) && !p.Gear.Contains(item))
                        //No need if any of following are true
                        || !(
                            //Player already has this unique item
                            ((item.Item?.IsUnique ?? true) && p.Gear.Contains(item))
                            //Player has Bis or higher/same iLvl for all aplicable slots
                            || (p.MainChar.MainClass?.HaveBisOrHigherItemLevel(item.Slots, item) ?? false)
                        )
                    )
                    {
                        result.NeededItems[p].Add(item);
                    }
                if(result.NeededItems[p].Count > 0)
                    result.Needer.Add(p);
                else
                    result.Greeder.Add(p);
            }
            var comparer = GetComparer(possibleItems);
            result.Needer.Sort(comparer);
            result.Fill(comparer);
            return result;
        }
        public class LootResult : IEnumerable<Player>
        {
            public List<Player> Needer = new();
            public List<Player> Greeder = new();
            public Dictionary<Player, LootRule> DecidingFactors = new();
            public Dictionary<Player, Dictionary<LootRule, string>> EvaluatedRules = new();
            public Dictionary<Player, IEnumerable<GearItem>> ApplicableItems = new();
            public Dictionary<Player, List<GearItem>> NeededItems = new();
            internal void Fill(LootRulingComparer comparer)
            {
                foreach (Player p in this)
                {
                    EvaluatedRules[p] = new();
                    if (!comparer.EvaluatedRules.TryGetValue(p, out var evals))                        
                        evals = comparer.EvaluatePlayer(p);
                    foreach ((LootRule rule, (_, string reason)) in evals)
                        EvaluatedRules[p].Add(rule, reason);
                    EvaluatedRules[p].Add(new(LootRuleEnum.Greed), "");
                }
                for (int i = 0; i < Needer.Count - 1; i++)
                    DecidingFactors[Needer[i]] = comparer.DecidingRule[(Needer[i], Needer[i+1])];
                DecidingFactors[Needer.Last()] = new(LootRuleEnum.NeedGreed);
                foreach(var p in Greeder)
                    DecidingFactors[p] = new(LootRuleEnum.Greed);
            }
            private IEnumerable<Player> GetPlayers()
            {
                foreach (var player in Needer)
                    yield return player;
                foreach (var player in Greeder)
                    yield return player;
            }

            public IEnumerator<Player> GetEnumerator() => GetPlayers().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetPlayers().GetEnumerator();
        }
        private LootRulingComparer GetComparer(IEnumerable<GearItem> possibleItems) => new(this, possibleItems);
        internal class LootRulingComparer : IComparer<Player>
        {
            private readonly LootSession _session;
            private readonly IEnumerable<GearItem> _possibleItems;
            public Dictionary<(Player, Player), LootRule> DecidingRule = new();
            public Dictionary<Player, Dictionary<LootRule, (int val, string reason)>> EvaluatedRules = new();
            public LootRulingComparer(LootSession session, IEnumerable<GearItem> possibleItems)
            {
                (_session, _possibleItems) = (session, possibleItems);
            }
                
            internal Dictionary<LootRule, (int val, string reason)> EvaluatePlayer(Player p)
            {
                if (EvaluatedRules.TryGetValue(p, out var result))
                    return result;
                result = new();
                foreach (LootRule rule in _session.RulingOptions.RuleSet)
                {
                    result[rule] = rule.Eval(p, _session, _possibleItems);
                }
                EvaluatedRules.Add(p, result);
                return result;
            }
            private int Compare(Player x, Player y,LootRule r) => EvaluatedRules[y][r].val - EvaluatedRules[x][r].val;
            public int Compare(Player? x, Player? y)
            {
                if (x is null || y is null)
                    return 0;
                EvaluatePlayer(x);
                EvaluatePlayer(y);
                if (DecidingRule.TryGetValue((x, y), out LootRule? cachedRule))
                {
                    return Compare(x, y,cachedRule);
                }
                foreach (var rule in _session.RulingOptions.RuleSet)
                {
                    int result = Compare(x, y, rule);
                    if (result != 0)
                    {
                        DecidingRule.Add((x, y), rule);
                        DecidingRule.Add((y, x), rule);
                        return result;
                    }
                }
                DecidingRule.Add((x, y), LootRuling.Default);
                return 0;
            }
        }
    }

}
