using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.Extensions;
using static HimbeertoniRaidTool.HrtServices.Localization;

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
            LootResults results = new(this);
            excluded.AddRange(Excluded);
            if (excludeAddition != null)
                excluded.AddRange(excludeAddition);
            IEnumerable<GearItem> possibleItems;
            if (droppedItem.IsGear)
                possibleItems = new List<GearItem> { new(droppedItem.ID) };
            else if (droppedItem.IsContainerItem || droppedItem.IsExchangableItem)
                possibleItems = droppedItem.PossiblePurchases;
            else
                return results;
            if (_group.Type == GroupType.Solo)
            {
                var player = _group.First();
                foreach (var job in player.MainChar)
                {
                    results.Add(new(this, player, possibleItems, job.Job));
                }
            }
            else
            {
                foreach (var player in Group)
                {
                    if (excluded.Contains(player))
                        continue;
                    results.Add(new(this, player, possibleItems));
                }
            }
            results.Eval();
            return results;
        }
        public enum State
        {
            STARTED,
            LOOT_CHOSEN,
            DISTRIBUTION_STARTED,
            FINISHED,
        }
    }
    public static class LootSessionExtensions
    {
        public static string FriendlyName(this LootSession.State state) => state switch
        {
            LootSession.State.STARTED => Localize("LootSession:State:STARTED", "Waiting for Loot"),
            LootSession.State.LOOT_CHOSEN => Localize("LootSession:State:LOOT_CHOSEN", "Loot chosen"),
            LootSession.State.DISTRIBUTION_STARTED => Localize("LootSession:State:DISTRIBUTION_STARTED", "Dsitribution started"),
            LootSession.State.FINISHED => Localize("LootSession:State:FINISHED", "Finished"),
            _ => Localize("undefinded", "undefinded")
        };

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
        public readonly Job Job;
        public readonly int Roll;
        public PlayableClass AplicableJob => Player.MainChar[Job];
        public int RolePrio => _session.RolePriority.GetPriority(Player.CurJob.GetRole());
        public bool IsEvaluated { get; private set; } = false;
        public readonly Dictionary<LootRule, (int val, string reason)> EvaluatedRules = new();
        public readonly HashSet<GearItem> ApplicableItems;
        public readonly List<GearItem> NeededItems = new();
        public LootResult(LootSession session, Player p, IEnumerable<GearItem> possibleItems, Job? job = null)
        {
            _session = session;
            Player = p;
            Job = job ?? p.MainChar.MainJob ?? Job.ADV;
            Roll = Random.Next(0, 101);
            //Filter items by job
            ApplicableItems = new(possibleItems.Where(i => (i.Item?.ClassJobCategory.Value).Contains(Job)));
            CalcNeed();
        }
        public void Evaluate()
        {
            if (IsEvaluated)
                return;
            foreach (LootRule rule in _session.RulingOptions.RuleSet)
            {
                EvaluatedRules[rule] = rule.Eval(this, _session, NeededItems);
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
        private void CalcNeed()
        {
            foreach (var item in ApplicableItems)
                if (
                    //Always need if Bis and not aquired
                    (AplicableJob.BIS.Contains(item) && !AplicableJob.Gear.Contains(item))
                    //No need if any of following are true
                    || !(
                        //Player already has this unique item
                        ((item.Item?.IsUnique ?? true) && AplicableJob.Gear.Contains(item))
                        //Player has Bis or higher/same iLvl for all aplicable slots
                        || AplicableJob.HaveBisOrHigherItemLevel(item.Slots, item)
                    )
                )
                { NeededItems.Add(item); }
            Category = NeededItems.Count > 0 ? LootCategory.Need : LootCategory.Greed;
        }
    }
    public class LootResults : IReadOnlyList<LootResult>
    {
        private readonly List<LootResult> Participants = new();
        public int Count => Participants.Count;
        public readonly LootSession Session;
        public LootResult this[int index] => Participants[index];

        public LootResults(LootSession session)
        {
            Session = session;
        }
        internal void Eval()
        {

            foreach (LootResult result in this.Where(r => !r.IsEvaluated))
                result.Evaluate();
            Participants.Sort(new LootRulingComparer(Session.RulingOptions.RuleSet));
        }
        internal void Add(LootResult result) => Participants.Add(result);
        public IEnumerator<LootResult> GetEnumerator() => Participants.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Participants.GetEnumerator();
    }
    internal class LootRulingComparer : IComparer<LootResult>
    {
        private readonly List<LootRule> Rules;
        public LootRulingComparer(List<LootRule> rules)
        {
            Rules = rules;
        }
        public int Compare(LootResult? x, LootResult? y)
        {
            if (x is null || y is null)
                return 0;
            if (x.Category - y.Category != 0)
                return x.Category - y.Category;
            foreach (var rule in Rules)
            {
                int result = y.EvaluatedRules[rule].val - x.EvaluatedRules[rule].val;
                if (result != 0)
                {
                    return result;
                }
            }
            return 0;
        }
    }
}

