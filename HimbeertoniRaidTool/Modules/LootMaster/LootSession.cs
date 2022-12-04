using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using Lumina.Excel.Extensions;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

public class LootSession
{
    public readonly InstanceWithLoot Instance;
    public LootRuling RulingOptions { get; set; }
    public State CurrentState { get; private set; } = State.STARTED;
    internal readonly List<(HrtItem item, int count)> Loot = new();
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
    public Dictionary<(HrtItem, int), LootResultContainer> Results { get; private set; } = new();
    public List<Player> Excluded = new();
    public readonly RolePriority RolePriority;
    public Dictionary<HrtItem, bool> GuaranteedLoot { get; private set; } = new();
    internal int NumLootItems => Loot.Aggregate(0, (sum, x) => sum + x.count);
    public LootSession(RaidGroup group, LootRuling rulingOptions, RolePriority defaultRolePriority, InstanceWithLoot instance)
    {
        Instance = instance;
        RulingOptions = rulingOptions.Clone();
        foreach (var item in Instance.PossibleItems)
            Loot.Add((item, 0));
        foreach (var item in instance.GuaranteedItems)
            GuaranteedLoot[item] = false;
        _group = Group = group;
        RolePriority = group.RolePriority ?? defaultRolePriority;
    }
    public void Evaluate()
    {
        if (CurrentState < State.LOOT_CHOSEN)
            CurrentState = State.LOOT_CHOSEN;
        if (Results.Count != NumLootItems && CurrentState < State.DISTRIBUTION_STARTED)
        {
            Results.Clear();
            foreach ((var item, int count) in Loot)
                for (int i = 0; i < count; i++)
                {
                    Results.Add((item, i), ConstructLootResults(item));
                }
        }
        foreach (var results in Results.Values)
            results.Eval();
    }
    public bool RevertToChooseLoot()
    {
        if (CurrentState != State.LOOT_CHOSEN)
            return false;
        CurrentState = State.STARTED;
        return true;
    }
    private LootResultContainer ConstructLootResults(HrtItem droppedItem, IEnumerable<Player>? excludeAddition = null)
    {
        List<Player> excluded = new();
        LootResultContainer results = new(this);
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
            foreach (var job in player.MainChar.Where(j => !j.IsEmpty))
            {
                results.Add(new(this, player, possibleItems, job.Job));
            }
        }
        else
        {
            foreach (var player in Group.Where(p => p.Filled))
            {
                if (excluded.Contains(player))
                    continue;
                results.Add(new(this, player, possibleItems));
            }
        }
        return results;
    }
    private void EvaluateFinished()
    {
        if (Results.Values.All(l => l.Finished) && GuaranteedLoot.Values.All(t => t))
            CurrentState = State.FINISHED;
    }
    internal bool AwardGuaranteedLoot(HrtItem item)
    {
        if (CurrentState < State.DISTRIBUTION_STARTED)
            CurrentState = State.DISTRIBUTION_STARTED;
        if (GuaranteedLoot[item] || CurrentState == State.FINISHED)
            return false;
        GuaranteedLoot[item] = true;
        foreach (var p in Group)
        {
            var inv = p.MainChar.MainInventory;
            int idx;
            if (inv.Contains(item.ID))
                idx = inv.IndexOf(item.ID);
            else
            {
                idx = inv.FirstFreeSlot();
                inv[idx] = new(item)
                {
                    quantity = 0,
                };
            }
            inv[idx].quantity++;
        }
        EvaluateFinished();
        return true;
    }
    internal bool AwardItem((HrtItem, int) loot, GearItem toAward, int idx)
    {
        if (CurrentState < State.DISTRIBUTION_STARTED)
            CurrentState = State.DISTRIBUTION_STARTED;
        if (Results[loot].IsAwarded || CurrentState == State.FINISHED)
            return false;
        Results[loot].Award(idx, toAward);
        var slot = toAward.Slots.First();
        var c = Results[loot].AwardedTo?.AplicableJob;
        if (c != null)
        {
            c.Gear[slot] = toAward;
            foreach (var m in c.BIS[slot].Materia)
                c.Gear[slot].Materia.Add(m);
        }
        EvaluateFinished();
        if (CurrentState != State.FINISHED)
            Evaluate();
        return true;
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
        LootSession.State.STARTED => Localize("LootSession:State:STARTED", "Chosing Loot"),
        LootSession.State.LOOT_CHOSEN => Localize("LootSession:State:LOOT_CHOSEN", "Loot locked"),
        LootSession.State.DISTRIBUTION_STARTED => Localize("LootSession:State:DISTRIBUTION_STARTED", "Distribution started"),
        LootSession.State.FINISHED => Localize("LootSession:State:FINISHED", "Finished"),
        _ => Localize("undefinded", "undefinded")
    };
    public static string FriendlyName(this LootCategory cat) => cat switch
    {
        LootCategory.Need => Localize("LootCategory:Need", "Need"),
        LootCategory.Greed => Localize("LootCategory:Greed", "Greed"),
        LootCategory.Pass => Localize("LootCategory:Pass", "Pass"),
        LootCategory.Undecided => Localize("LootCategory:Undecided", "Undecided"),
        _ => Localize("undefinded", "undefinded"),
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
    public readonly PlayableClass AplicableJob;
    public int RolePrio => _session.RolePriority.GetPriority(Player.CurJob.GetRole());
    public bool IsEvaluated { get; private set; } = false;
    public readonly Dictionary<LootRule, (int val, string reason)> EvaluatedRules = new();
    public readonly HashSet<GearItem> ApplicableItems;
    public readonly List<GearItem> NeededItems = new();
    public GearItem? AwardedItem;
    public LootResult(LootSession session, Player p, IEnumerable<GearItem> possibleItems, Job? job = null)
    {
        _session = session;
        Player = p;
        Job = job ?? p.MainChar.MainJob ?? Job.ADV;
        var applicableJob = Player.MainChar[Job];
        if (applicableJob == null)
        {
            applicableJob = Player.MainChar.AddClass(Job);
            Services.HrtDataManager.GetManagedGearSet(ref applicableJob.Gear);
            Services.HrtDataManager.GetManagedGearSet(ref applicableJob.BIS);
        }
        AplicableJob = applicableJob;
        Roll = Random.Next(0, 101);
        //Filter items by job
        ApplicableItems = new(possibleItems.Where(i => (i.Item?.ClassJobCategory.Value).Contains(Job)));
    }
    public void Evaluate()
    {
        CalcNeed();
        foreach (var rule in _session.RulingOptions.RuleSet)
        {
            EvaluatedRules[rule] = rule.Eval(this, _session);
        }
        IsEvaluated = true;
    }
    public LootRule DecidingFactor(LootResult? other)
    {
        if (other == null)
            return LootRuling.Default;
        if (Category == LootCategory.Need && other.Category == LootCategory.Greed)
            return LootRuling.NeedOverGreed;
        foreach (var rule in _session.RulingOptions.RuleSet)
        {
            if (EvaluatedRules[rule].val != other.EvaluatedRules[rule].val)
                return rule;
        }
        return LootRuling.Default;
    }
    private void CalcNeed()
    {
        NeededItems.Clear();
        foreach (var item in ApplicableItems)
            if (
                //Always need if Bis and not aquired
                AplicableJob.BIS.Contains(item) && !AplicableJob.Gear.Contains(item)
                //No need if any of following are true
                || !(
                    //Player already has this unique item
                    (item.Item?.IsUnique ?? true) && AplicableJob.Gear.Contains(item)
                    //Player has Bis or higher/same iLvl for all aplicable slots
                    || AplicableJob.HaveBisOrHigherItemLevel(item.Slots, item)
                )
            )
            { NeededItems.Add(item); }
        Category = NeededItems.Count > 0 ? LootCategory.Need : LootCategory.Greed;
    }
}
public class LootResultContainer : IReadOnlyList<LootResult>
{
    private readonly List<LootResult> Participants = new();
    public int Count => Participants.Count;
    public readonly LootSession Session;
    public LootResult this[int index] => Participants[index];
    public LootResult? AwardedTo => AwardedIdx.HasValue ? this[AwardedIdx.Value] : null;
    public bool IsAwarded => AwardedTo != null;
    public bool Finished => IsAwarded || Count == 0 || this[0].Category != LootCategory.Need;
    public int? AwardedIdx { get; private set; }

    private string? ShortResultCache;
    internal bool ShowDetails = true;

    public string ShortResult
    {
        get
        {
            if (ShortResultCache != null)
                return ShortResultCache;
            if (IsAwarded)
                return ShortResultCache = $"{AwardedTo?.AwardedItem?.Name} {Localize("LootResult:ItemAwardedTo", "awarded to")} {AwardedTo?.Player.NickName} ({AwardedTo?.AplicableJob})";
            if (Count == 0 || this[0].Category != LootCategory.Need)
                return ShortResultCache = Localize("LootResult:GreedOnly", "Greed only");
            string result = $"{this[0].Player.NickName} ({this[0].AplicableJob.Job}) {Localize("LootResult:PlayerWon", "won")}";
            if (Count > 1)
            {
                if (this[1].Category == LootCategory.Need)
                    result += $" {Localize("LootResult:PlayerWonOver", "over")} {this[1].Player.NickName} ({this[1].AplicableJob.Job})";
                result += $" ({this[0].DecidingFactor(this[1])})";
            }
            return ShortResultCache = result;
        }
    }

    public LootResultContainer(LootSession session)
    {
        Session = session;
    }
    internal void Eval()
    {
        if (IsAwarded)
            return;
        foreach (var result in this)
            result.Evaluate();
        Participants.Sort(new LootRulingComparer(Session.RulingOptions.RuleSet));
        ShortResultCache = null;
    }
    public void Award(int idx, GearItem awarded)
    {
        AwardedIdx ??= idx;
        AwardedTo!.AwardedItem = awarded;
        ShortResultCache = null;
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

