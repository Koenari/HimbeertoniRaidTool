using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.Extensions;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    public class LootSession
    {
        public LootRuling RulingOptions { get; private set; }
        internal readonly Dictionary<HrtItem, int> Loot;
        private RaidGroup _group;
        internal RaidGroup Group
        {
            get => _group;
            set
            {
                _group = value;
                Results.Clear();
            }
        }
        public Dictionary<(HrtItem, int), LootResults> Results = new();
        public List<Player> Excluded = new();
        public readonly RolePriority RolePriority;
        internal int NumLootItems => Loot.Values.Aggregate(0, (sum, x) => sum + x);
        public LootSession(RaidGroup group, LootRuling rulingOptions, RolePriority rolePriority, IEnumerable<HrtItem> possibleItems)
        {
            RulingOptions = rulingOptions.Clone();
            Loot = new();
            foreach (var item in possibleItems)
                Loot[item] = 0;
            _group = Group = group;
            RolePriority = rolePriority;
        }
        public void EvaluateAll(bool reevaluate = false)
        {
            if (Results.Count == NumLootItems && !reevaluate)
                return;
            Results.Clear();
            foreach ((HrtItem item, int count) in Loot)
                for (int i = 0; i < count; i++)
                {
                    Results.Add((item, i), Evaluate(item));
                }
        }
        private LootResults Evaluate(HrtItem droppedItem, IEnumerable<Player>? excludeAddition = null)
        {
            List<Player> excluded = new();
            excluded.AddRange(Excluded);
            if (excludeAddition != null)
                excluded.AddRange(excludeAddition);
            IEnumerable<GearItem> possibleItems;
            if (droppedItem.IsGear)
                possibleItems = new List<GearItem> { new(droppedItem.ID) };
            else if (droppedItem.IsContainerItem || droppedItem.IsExchangableItem)
                possibleItems = droppedItem.PossiblePurchases;
            else
                return new();
            LootResults results = new();
            foreach (var player in Group)
            {

                if (excluded.Contains(player))
                    continue;
                //Pre filter items by job
                LootResult result = new(
                        this,
                        player,
                        possibleItems.Where(i => (i.Item?.ClassJobCategory.Value).Contains(player.MainChar.MainJob))
                    );
                //Calculate need for each item
                foreach (var item in result.ApplicableItems)

                    if (
                        //Always need if Bis and not aquired
                        ((player.CurJob?.BIS.Contains(item)).GetValueOrDefault()
                        && !(player.CurJob?.Gear.Contains(item)).GetValueOrDefault())
                        //No need if any of following are true
                        || !(
                            //Player already has this unique item
                            ((item.Item?.IsUnique ?? true) && (player.CurJob?.Gear.Contains(item)).GetValueOrDefault())
                            //Player has Bis or higher/same iLvl for all aplicable slots
                            || (player.MainChar.MainClass?.HaveBisOrHigherItemLevel(item.Slots, item) ?? false)
                        )
                    )
                    {
                        result.NeededItems.Add(item);
                    }
                if (result.NeededItems.Count > 0)
                    result.Category = LootCategory.Need;
                else
                    result.Category = LootCategory.Greed;
                results.Players.Add(result);
            }
            results.Eval(this);
            return results;
        }
    }
    public enum LootCategory
    {
        Need = 0,
        Greed = 10,
        Pass = 20,
        Undecided = 30,
    }
    public class LootResult
    {
        private static readonly Random Random = new(Guid.NewGuid().GetHashCode());
        private readonly LootSession _session;
        public LootCategory Category = LootCategory.Undecided;
        public readonly Player Player;
        public readonly int Roll;
        public int RolePrio => _session.RolePriority.GetPriority(Player.CurJob.GetRole());
        public bool IsEvaluated { get; private set; } = false;
        public readonly Dictionary<LootRule, (int val, string reason)> EvaluatedRules = new();
        public readonly HashSet<GearItem> ApplicableItems;
        public readonly List<GearItem> NeededItems = new();
        public LootResult(LootSession session, Player p, IEnumerable<GearItem> applicableItems)
        {
            _session = session;
            Player = p;
            Roll = Random.Next(0, 101);
            ApplicableItems = new(applicableItems);
        }
        public void Evaluate(LootSession session)
        {
            if (IsEvaluated)
                return;
            foreach (LootRule rule in session.RulingOptions.RuleSet)
            {
                EvaluatedRules[rule] = rule.Eval(this, session, NeededItems);
            }
            IsEvaluated = true;
        }
        public LootRule DecidingFactor(LootResult other)
        {
            foreach ((LootRule rule, (int val, string _)) in EvaluatedRules)
            {
                if (val != other.EvaluatedRules[rule].val)
                    return rule;
            }
            return LootRuling.Default;
        }

    }
    public class LootResults : IReadOnlyList<LootResult>
    {
        public readonly List<LootResult> Players = new();
        public int Count => Players.Count;

        public LootResult this[int index] => Players[index];

        internal void Eval(LootSession session)
        {

            foreach (LootResult result in this.Where(r => !r.IsEvaluated))
                result.Evaluate(session);
            Players.Sort(new LootRulingComparer(session));
        }
        public IEnumerator<LootResult> GetEnumerator() => Players.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Players.GetEnumerator();
    }
    internal class LootRulingComparer : IComparer<LootResult>
    {
        private readonly LootSession _session;
        public LootRulingComparer(LootSession session)
        {
            _session = session;
        }
        private static int Compare(LootResult x, LootResult y, LootRule r) => y.EvaluatedRules[r].val - x.EvaluatedRules[r].val;
        public int Compare(LootResult? x, LootResult? y)
        {
            if (x is null || y is null)
                return 0;
            if (x.Category - y.Category != 0)
                return x.Category - y.Category;
            foreach (var rule in _session.RulingOptions.RuleSet)
            {
                int result = Compare(x, y, rule);
                if (result != 0)
                {
                    return result;
                }
            }
            return 0;
        }
    }
}

