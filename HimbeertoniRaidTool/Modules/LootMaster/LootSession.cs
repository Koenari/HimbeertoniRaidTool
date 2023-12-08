using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using System.Collections;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

public class LootSession
{
    public readonly InstanceWithLoot Instance;
    public LootRuling RulingOptions { get; set; }
    public State CurrentState { get; private set; } = State.Started;
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
    
    public Dictionary<(HrtItem, int), LootResultContainer> Results { get; } = new();
    public List<Player> Excluded = new();
    public readonly RolePriority RolePriority;
    public Dictionary<HrtItem, bool> GuaranteedLoot { get; } = new();
    internal int NumLootItems => Loot.Aggregate(0, (sum, x) => sum + x.count);
    public LootSession(RaidGroup group, LootRuling rulingOptions, RolePriority defaultRolePriority, InstanceWithLoot instance)
    {
        Instance = instance;
        RulingOptions = rulingOptions.Clone();
        foreach (HrtItem item in Instance.PossibleItems)
            Loot.Add((item, 0));
        foreach (HrtItem item in instance.GuaranteedItems)
            GuaranteedLoot[item] = false;
        _group = Group = group;
        RolePriority = group.RolePriority ?? defaultRolePriority;
    }
    public void Evaluate()
    {
        if (CurrentState < State.LootChosen)
            CurrentState = State.LootChosen;
        if (Results.Count != NumLootItems && CurrentState < State.DistributionStarted)
        {
            Results.Clear();
            foreach ((HrtItem item, int count) in Loot)
                for (int i = 0; i < count; i++)
                {
                    Results.Add((item, i), ConstructLootResults(item));
                }
        }
        foreach (LootResultContainer results in Results.Values)
            results.Eval();
    }
    public bool RevertToChooseLoot()
    {
        if (CurrentState != State.LootChosen)
            return false;
        CurrentState = State.Started;
        return true;
    }
    private LootResultContainer ConstructLootResults(HrtItem droppedItem, IEnumerable<Player>? excludeAddition = null)
    {
        List<Player> excluded = new();
        LootResultContainer results = new(this);
        excluded.AddRange(Excluded);
        if (excludeAddition != null)
            excluded.AddRange(excludeAddition);
        List<GearItem> possibleItems;
        if (droppedItem.IsGear)
            possibleItems = new List<GearItem> { new(droppedItem.Id) };
        else if (droppedItem.IsContainerItem || droppedItem.IsExchangableItem)
            possibleItems = droppedItem.PossiblePurchases.ToList();
        else
            return results;
        if (_group.Type == GroupType.Solo)
        {
            Player player = _group.First();
            foreach (PlayableClass job in player.MainChar.Where(j => !j.IsEmpty))
            {
                results.Add(new LootResult(this, player, possibleItems, droppedItem, job.Job));
            }
        }
        else
        {
            foreach (Player player in Group.Where(p => p.Filled))
            {
                if (excluded.Contains(player))
                    continue;
                results.Add(new LootResult(this, player, possibleItems, droppedItem));
            }
        }
        return results;
    }
    private void EvaluateFinished()
    {
        if (Results.Values.All(l => l.Finished) && GuaranteedLoot.Values.All(t => t))
            CurrentState = State.Finished;
    }
    internal bool AwardGuaranteedLoot(HrtItem item)
    {
        if (CurrentState < State.DistributionStarted)
            CurrentState = State.DistributionStarted;
        if (GuaranteedLoot[item] || CurrentState == State.Finished)
            return false;
        GuaranteedLoot[item] = true;
        foreach (Player p in Group)
        {
            Inventory inv = p.MainChar.MainInventory;
            int idx;
            if (inv.Contains(item.Id))
                idx = inv.IndexOf(item.Id);
            else
            {
                idx = inv.FirstFreeSlot();
                inv[idx] = new InventoryEntry(item)
                {
                    Quantity = 0,
                };
            }
            inv[idx].Quantity++;
        }
        EvaluateFinished();
        return true;
    }
    internal bool AwardItem((HrtItem, int) loot, GearItem toAward, int idx, bool altSlot = false)
    {
        if (CurrentState < State.DistributionStarted)
            CurrentState = State.DistributionStarted;
        if (Results[loot].IsAwarded || CurrentState == State.Finished)
            return false;
        Results[loot].Award(idx, toAward);
        GearSetSlot slot;
        if (toAward.Slots.Count() > 1 && altSlot)
            slot = toAward.Slots.Skip(1).First();
        else
            slot = toAward.Slots.First();
        PlayableClass? c = Results[loot].AwardedTo?.ApplicableJob;
        if (c != null)
        {
            c.Gear[slot] = toAward;
            foreach (HrtMateria m in c.Bis[slot].Materia)
                c.Gear[slot].AddMateria(m);
        }
        EvaluateFinished();
        if (CurrentState != State.Finished)
            Evaluate();
        return true;
    }

    public enum State
    {
        Started,
        LootChosen,
        DistributionStarted,
        Finished,
    }
}

public static class LootSessionExtensions
{
    public static string FriendlyName(this LootSession.State state) => state switch
    {
        LootSession.State.Started => Localize("LootSession:State:STARTED", "Choosing Loot"),
        LootSession.State.LootChosen => Localize("LootSession:State:LOOT_CHOSEN", "Loot locked"),
        LootSession.State.DistributionStarted => Localize("LootSession:State:DISTRIBUTION_STARTED", "Distribution started"),
        LootSession.State.Finished => Localize("LootSession:State:FINISHED", "Finished"),
        _ => Localize("undefined", "undefined"),
    };
    public static string FriendlyName(this LootCategory cat) => cat switch
    {
        LootCategory.Need => Localize("LootCategory:Need", "Need"),
        LootCategory.Greed => Localize("LootCategory:Greed", "Greed"),
        LootCategory.Pass => Localize("LootCategory:Pass", "Pass"),
        LootCategory.Undecided => Localize("LootCategory:Undecided", "Undecided"),
        _ => Localize("undefined", "undefined"),
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
    private static readonly Random _random = new(Guid.NewGuid().GetHashCode());
    private readonly LootSession _session;
    public LootCategory Category = LootCategory.Undecided;
    public readonly Player Player;
    public readonly Job Job;
    public readonly int Roll;
    public readonly PlayableClass ApplicableJob;
    public readonly HrtItem DroppedItem;
    public IEnumerable<HrtItem> GuaranteedLoot => _session.GuaranteedLoot.Keys;
    public int RolePriority => _session.RolePriority.GetPriority(ApplicableJob.Role);
    public bool IsEvaluated { get; private set; }
    public readonly Dictionary<LootRule, (float val, string reason)> EvaluatedRules = new();
    public readonly HashSet<GearItem> ApplicableItems;
    public readonly List<GearItem> NeededItems = new();
    public GearItem? AwardedItem;
    public bool ShouldIgnore => IsEvaluated && _session.RulingOptions.ActiveRules.Any(x => x.ShouldIgnore(this));
    public LootResult(LootSession session, Player p, IEnumerable<GearItem> possibleItems, HrtItem droppedItem, Job? job = null)
    {
        _session = session;
        Player = p;
        Job = job ?? p.MainChar.MainJob ?? Job.ADV;
        DroppedItem = droppedItem;
        PlayableClass? applicableJob = Player.MainChar[Job];
        if (applicableJob == null)
        {
            applicableJob = Player.MainChar.AddClass(Job);

            ServiceManager.HrtDataManager.GearDb.TryAdd(applicableJob.Gear);
            ServiceManager.HrtDataManager.GearDb.TryAdd(applicableJob.Bis);
        }
        ApplicableJob = applicableJob;
        Roll = _random.Next(0, 101);
        //Filter items by job
        ApplicableItems = new HashSet<GearItem>(possibleItems.Where(i => i.Jobs.Contains(Job)));
    }
    public void Evaluate()
    {
        CalcNeed();
        foreach (LootRule rule in _session.RulingOptions.ActiveRules)
        {
            EvaluatedRules[rule] = rule.Eval(this);
        }
        IsEvaluated = true;
    }
    public LootRule DecidingFactor(LootResult? other)
    {
        if (other == null)
            return LootRuling.Default;
        if (Category == LootCategory.Need && other.Category == LootCategory.Greed)
            return LootRuling.NeedOverGreed;
        foreach (LootRule rule in _session.RulingOptions.ActiveRules)
        {
            if (EvaluatedRules[rule].val != other.EvaluatedRules[rule].val)
                return rule;
        }
        return LootRuling.Default;
    }
    private void CalcNeed()
    {
        NeededItems.Clear();
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (GearItem item in ApplicableItems)
            if (
                //Always need if Bis and not acquired
                ApplicableJob.Bis.Any(x => item.Equals(x,ItemComparisonMode.IdOnly)) && !ApplicableJob.Gear.Any(x => item.Equals(x,ItemComparisonMode.IdOnly))
                //No need if any of following are true
                || !(
                    //Player already has this unique item
                    item.IsUnique && ApplicableJob.Gear.Any(x => item.Equals(x, ItemComparisonMode.IdOnly))
                    //Player has Bis or higher/same iLvl for all applicable slots
                    || ApplicableJob.HaveBisOrHigherItemLevel(item.Slots, item)
                )
            )
            {
                NeededItems.Add(item);
            }
        Category = NeededItems.Count > 0 ? LootCategory.Need : LootCategory.Greed;
    }
}

public class LootResultContainer : IReadOnlyList<LootResult>
{
    private readonly List<LootResult> _participants = new();
    public int Count => _participants.Count;
    public readonly LootSession Session;
    public LootResult this[int index] => _participants[index];
    public LootResult? AwardedTo => AwardedIdx.HasValue ? this[AwardedIdx.Value] : null;
    public bool IsAwarded => AwardedTo != null;
    public bool Finished => IsAwarded || Count == 0 || this[0].Category != LootCategory.Need;
    public int? AwardedIdx { get; private set; }

    private string? _shortResultCache;
    internal bool ShowDetails = true;

    public string ShortResult
    {
        get
        {
            if (_shortResultCache != null)
                return _shortResultCache;
            if (IsAwarded)
                return _shortResultCache = $"{AwardedTo?.AwardedItem?.Name} {Localize("LootResult:ItemAwardedTo", "awarded to")} {AwardedTo?.Player.NickName} ({AwardedTo?.ApplicableJob})";
            if (Count == 0 || this[0].Category != LootCategory.Need)
                return _shortResultCache = Localize("LootResult:GreedOnly", "Greed only");
            string result = $"{this[0].Player.NickName} ({this[0].ApplicableJob.Job}) {Localize("LootResult:PlayerWon", "won")}";
            if (Count > 1)
            {
                if (this[1].Category == LootCategory.Need)
                    result += $" {Localize("LootResult:PlayerWonOver", "over")} {this[1].Player.NickName} ({this[1].ApplicableJob.Job})";
                result += $" ({this[0].DecidingFactor(this[1])})";
            }
            return _shortResultCache = result;
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
        foreach (LootResult result in this)
            result.Evaluate();
        _participants.Sort(new LootRulingComparer(Session.RulingOptions.ActiveRules));
        _shortResultCache = null;
    }
    public void Award(int idx, GearItem awarded)
    {
        AwardedIdx ??= idx;
        AwardedTo!.AwardedItem = awarded;
        _shortResultCache = null;
    }
    internal void Add(LootResult result) => _participants.Add(result);
    public IEnumerator<LootResult> GetEnumerator() => _participants.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _participants.GetEnumerator();
}

internal class LootRulingComparer : IComparer<LootResult>
{
    private readonly IEnumerable<LootRule> _rules;
    public LootRulingComparer(IEnumerable<LootRule> rules)
    {
        _rules = rules;
    }
    public int Compare(LootResult? x, LootResult? y)
    {
        if (x is null || y is null)
            return 0;
        if (x.Category - y.Category != 0)
            return x.Category - y.Category;
        foreach (LootRule rule in _rules)
        {
            float result = y.EvaluatedRules[rule].val - x.EvaluatedRules[rule].val;
            switch (result)
            {
                case < 0:
                    return -1;
                case > 0:
                    return 1;
            }
        }
        return 0;
    }
}