using System.Collections;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using Item = HimbeertoniRaidTool.Common.Data.Item;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

public class LootSession
{

    public enum State
    {
        Started,
        LootChosen,
        DistributionStarted,
        Finished,
    }

    public readonly InstanceWithLoot Instance;
    internal readonly List<(Item item, int count)> Loot = new();
    public readonly RolePriority RolePriority;
    private RaidGroup _group;
    private readonly LootMasterModule _module;
    private LootMasterConfiguration.ConfigData CurConfig => _module.ConfigImpl.Data;
    internal LootSession(LootMasterModule module, InstanceWithLoot instance, RaidGroup group)
    {
        _module = module;
        Instance = instance;
        RulingOptions = CurConfig.LootRuling.Clone();
        foreach (var item in Instance.PossibleItems)
        {
            Loot.Add((item, 0));
        }
        foreach (var item in instance.GuaranteedItems)
        {
            GuaranteedLoot[item] = false;
        }
        _group = Group = group;
        RolePriority = group.RolePriority ?? CurConfig.RolePriority;
    }
    public LootRuling RulingOptions { get; set; }
    public State CurrentState { get; private set; } = State.Started;
    internal RaidGroup Group
    {
        get => _group;
        set
        {
            _group = value;
            Results.Clear();
        }
    }

    public Dictionary<(Item, int), LootResultContainer> Results { get; } = new();
    public Dictionary<Item, bool> GuaranteedLoot { get; } = new();
    private int NumLootItems => Loot.Aggregate(0, (sum, x) => sum + x.count);
    public void Evaluate()
    {
        if (CurrentState < State.LootChosen)
            CurrentState = State.LootChosen;
        if (Results.Count != NumLootItems && CurrentState < State.DistributionStarted)
        {
            Results.Clear();
            foreach ((var item, int count) in Loot)
            {
                for (int i = 0; i < count; i++)
                {
                    Results.Add((item, i), ConstructLootResults(item));
                }
            }
        }
        foreach (var results in Results.Values)
        {
            results.Eval();
        }
    }
    public bool RevertToChooseLoot()
    {
        if (CurrentState != State.LootChosen)
            return false;
        CurrentState = State.Started;
        return true;
    }
    private LootResultContainer ConstructLootResults(Item droppedItem, IEnumerable<Player>? excludeArg = null)
    {
        var excluded = excludeArg?.ToList() ?? [];
        LootResultContainer results = new(this);
        List<GearItem> possibleItems;
        if (droppedItem.IsGear)
            possibleItems = [new GearItem(droppedItem.Id)];
        else if (droppedItem.IsContainerItem() || droppedItem.IsExchangableItem())
            possibleItems = droppedItem.PossiblePurchases().ToList();
        else
            return results;
        if (_group.Type == GroupType.Solo)
        {
            var player = _group.First();
            foreach (var job in player.MainChar.Where(j => !j.IsEmpty))
            {
                results.Add(new LootResult(this, player, possibleItems, droppedItem, job.Job));
            }
        }
        else
        {
            foreach (var player in Group.Where(p => p.Filled))
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
    internal bool AwardGuaranteedLoot(Item item)
    {
        if (CurrentState < State.DistributionStarted)
            CurrentState = State.DistributionStarted;
        if (GuaranteedLoot[item] || CurrentState == State.Finished)
            return false;
        GuaranteedLoot[item] = true;
        foreach (var p in Group)
        {
            var inv = p.MainChar.MainInventory;
            inv[inv.ReserveSlot(item)].Quantity++;
        }
        EvaluateFinished();
        return true;
    }
    internal bool AwardItem((Item, int) loot, GearItem toAward, int idx, bool altSlot = false)
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
            slot = toAward.Slots.FirstOrDefault(GearSetSlot.None);
        var c = Results[loot].AwardedTo?.ApplicableJob;
        if (c != null)
        {
            c.CurGear[slot] = toAward;
            foreach (var m in c.CurBis[slot].Materia)
            {
                c.CurGear[slot].AddMateria(m);
            }
        }
        EvaluateFinished();
        if (CurrentState != State.Finished)
            Evaluate();
        return true;
    }
}

public static class LootSessionExtensions
{
    public static string FriendlyName(this LootSession.State state) => state switch
    {
        LootSession.State.Started             => LootmasterLoc.LootSession_State_STARTED,
        LootSession.State.LootChosen          => LootmasterLoc.LootSession_State_LOOT_CHOSEN,
        LootSession.State.DistributionStarted => LootmasterLoc.LootSession_State_DISTRIBUTION_STARTED,
        LootSession.State.Finished            => LootmasterLoc.LootSession_State_FINISHED,
        _                                     => GeneralLoc.CommonTerms_undefined,
    };
    public static string FriendlyName(this LootCategory cat) => cat switch
    {
        LootCategory.Need      => LootmasterLoc.LootCategory_Need,
        LootCategory.Greed     => LootmasterLoc.LootCategory_Greed,
        LootCategory.Pass      => LootmasterLoc.LootCategory_Pass,
        LootCategory.Undecided => LootmasterLoc.LootCategory_Undecided,
        _                      => GeneralLoc.CommonTerms_undefined,
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
    private readonly HashSet<GearItem> _applicableItems;
    public readonly PlayableClass? ApplicableJob;
    private readonly Item _droppedItem;
    public readonly Dictionary<LootRule, (float val, string reason)> EvaluatedRules = new();
    public readonly List<GearItem> NeededItems = [];
    public readonly Player Player;
    public readonly int Roll;
    public GearItem? AwardedItem;
    public LootCategory Category = LootCategory.Undecided;
    public LootResult(LootSession session, Player p, IEnumerable<GearItem> possibleItems, Item droppedItem,
                      Job? job = null)
    {
        _session = session;
        Player = p;
        var job1 = job ?? p.MainChar.MainJob ?? Job.ADV;
        _droppedItem = droppedItem;
        ApplicableJob = Player.MainChar[job1];
        Roll = Random.Next(0, 101);
        //Filter items by job
        _applicableItems = [..possibleItems.Where(i => i.Jobs.Contains(job1))];
    }
    private IEnumerable<Item> GuaranteedLoot => _session.GuaranteedLoot.Keys;
    private bool IsEvaluated { get; set; }
    public bool ShouldIgnore => IsEvaluated && _session.RulingOptions.ActiveRules.Any(x => x.ShouldIgnore(this));
    public void Evaluate()
    {
        CalcNeed();
        foreach (var rule in _session.RulingOptions.ActiveRules)
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
        foreach (var rule in _session.RulingOptions.ActiveRules)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (EvaluatedRules[rule].val != other.EvaluatedRules[rule].val)
                return rule;
        }
        return LootRuling.Default;
    }
    private void CalcNeed()
    {
        NeededItems.Clear();
        if (ApplicableJob is null) return;
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var item in _applicableItems)
        {
            if (
                //Always need if Bis and not acquired
                ApplicableJob.CurBis.Any(x => item.Equals(x, ItemComparisonMode.IdOnly))
             && !ApplicableJob.CurGear.Any(x => item.Equals(x, ItemComparisonMode.IdOnly))
                //No need if any of following are true
             || !(
                    //Player already has this unique item
                    item.IsUnique && ApplicableJob.CurGear.Any(x => item.Equals(x, ItemComparisonMode.IdOnly))
                    //Player has Bis or higher/same iLvl for all applicable slots
                 || ApplicableJob.HaveBisOrHigherItemLevel(item.Slots, item)
                )
            )
            {
                NeededItems.Add(item);
            }
        }
        Category = NeededItems.Count > 0 ? LootCategory.Need : LootCategory.Greed;
    }


    public int RolePriority => _session.RolePriority.GetPriority(ApplicableJob?.Role);
    public int ItemLevel => ApplicableJob?.CurGear.ItemLevel ?? 0;
    public int ItemLevelGain()
    {
        if (ApplicableJob is null) return 0;
        return NeededItems.Select(
                              item => (int)item.ItemLevel - ApplicableJob.CurGear
                                                                         .Where(
                                                                             i => i.Slots.Intersect(item.Slots).Any())
                                                                         .Aggregate((int)item.ItemLevel,
                                                                             (min, i) => Math.Min(
                                                                                 (int)i.ItemLevel, min))).Prepend(0)
                          .Max();
    }
    public float DpsGain()
    {
        if (ApplicableJob is null) return 0;
        var stats = ApplicableJob.CurGear.GetStatBlock(ApplicableJob, Player.MainChar.Tribe).StatEquations;
        double baseDps = stats.AverageSkillDamage(100) / stats.Gcd();
        double newDps = double.NegativeInfinity;
        foreach (var i in _applicableItems)
        {
            GearItem? item = null;
            foreach (var bisItem in ApplicableJob.CurBis)
            {
                if (bisItem.Equals(i, ItemComparisonMode.IdOnly))
                    item = bisItem.Clone();
            }
            if (item is null)
            {
                item ??= i.Clone();
                foreach (var mat in ApplicableJob.CurGear[i.Slots.FirstOrDefault(GearSetSlot.None)].Materia)
                {
                    item.AddMateria(mat);
                }
            }
            var curStats =
                ApplicableJob.CurGear.With(item).GetStatBlock(ApplicableJob, Player.MainChar.Tribe).StatEquations;
            double cur = curStats.AverageSkillDamage(100) / curStats.Gcd();
            if (cur > newDps)
                newDps = cur;
        }
        return (float)((newDps - baseDps) / baseDps);
    }
    public bool IsBiS() => ApplicableJob is not null &&
                           NeededItems.Any(i => ApplicableJob.CurBis.Count(x => x.Equals(i, ItemComparisonMode.IdOnly))
                                             != ApplicableJob.CurGear.Count(
                                                    x => x.Equals(i, ItemComparisonMode.IdOnly)));
    public bool CanUse() =>
        //Direct gear or coffer drops are always usable
        ApplicableJob is not null && (
            !_droppedItem.IsExchangableItem()
         || NeededItems.Any(
                item => item.PurchasedFrom().Any(shopEntry =>
                {
                    foreach (var cost in shopEntry.entry.ItemCosts)
                    {
                        if (cost.CurrencyCost == 0) continue;
                        if (cost.ItemCost.RowId == _droppedItem.Id) continue;
                        var costItem = cost.ItemCost.AdjustItemCost(shopEntry.entry.PatchNumber);
                        if (costItem.IsCurrency()) continue;
                        if (costItem.IsTomeStone()) continue;
                        if (ApplicableJob.CurGear.Contains(new Item(costItem.RowId))) continue;
                        if (Player.MainChar.MainInventory.ItemCount(costItem.RowId)
                         >= cost.CurrencyCost) continue;
                        return false;
                    }
                    return true;
                })
            ));
    public bool CanBuy()
    {
        if (ApplicableJob is null) return false;
        var shops = _droppedItem.PurchasedFrom();
        return shops.Any(
            shopEntry =>
            {
                foreach (var cost in shopEntry.entry.ItemCosts)
                {
                    if (cost.CurrencyCost == 0) continue;
                    var costItem = cost.ItemCost.AdjustItemCost(shopEntry.entry.PatchNumber);
                    if (costItem.IsCurrency()) continue;
                    if (costItem.IsTomeStone()) continue;
                    if (ApplicableJob.CurGear.Contains(new Item(costItem.RowId))) continue;
                    if (Player.MainChar.MainInventory.ItemCount(costItem.RowId)
                      + (GuaranteedLoot.Any(loot => loot.Id == costItem.RowId) ? 1 : 0)
                     >= cost.CurrencyCost) continue;
                    return false;
                }
                return true;
            }
        );
    }
}

public class LootResultContainer(LootSession session) : IReadOnlyList<LootResult>
{
    private readonly List<LootResult> _participants = [];

    private string? _shortResultCache;

    public LootResult? AwardedTo => AwardedIdx.HasValue ? this[AwardedIdx.Value] : null;
    public bool IsAwarded => AwardedTo != null;
    public bool Finished => IsAwarded || Count == 0 || this[0].Category != LootCategory.Need;
    public int? AwardedIdx { get; private set; }

    public string ShortResult
    {
        get
        {
            if (_shortResultCache != null)
                return _shortResultCache;
            if (IsAwarded)
                return _shortResultCache =
                    $"{AwardedTo?.AwardedItem?.Name} {LootmasterLoc.LootUi_Results_ItemAwardedTo} {AwardedTo?.Player.NickName} ({AwardedTo?.ApplicableJob})";
            if (Count == 0 || this[0].Category != LootCategory.Need)
                return _shortResultCache = LootmasterLoc.LootUi_Results_GreedOnly;
            string result =
                $"{this[0].Player.NickName} ({this[0].ApplicableJob?.Job}) {LootmasterLoc.LootUi_Results_PlayerWon}";
            if (Count > 1)
            {
                if (this[1].Category == LootCategory.Need)
                    result +=
                        $" {LootmasterLoc.LootUi_Results_PlayerWonOver} {this[1].Player.NickName} ({this[1].ApplicableJob?.Job})";
                result += $" ({this[0].DecidingFactor(this[1])})";
            }
            return _shortResultCache = result;
        }
    }
    public int Count => _participants.Count;
    public LootResult this[int index] => _participants[index];
    public IEnumerator<LootResult> GetEnumerator() => _participants.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _participants.GetEnumerator();
    internal void Eval()
    {
        if (IsAwarded)
            return;
        foreach (var result in this)
        {
            result.Evaluate();
        }
        _participants.Sort(new LootRulingComparer(session.RulingOptions.ActiveRules));
        _shortResultCache = null;
    }
    public void Award(int idx, GearItem awarded)
    {
        AwardedIdx ??= idx;
        AwardedTo!.AwardedItem = awarded;
        _shortResultCache = null;
    }
    internal void Add(LootResult result) => _participants.Add(result);
}

internal class LootRulingComparer(IEnumerable<LootRule> rules) : IComparer<LootResult>
{
    public int Compare(LootResult? x, LootResult? y)
    {
        if (x is null || y is null)
            return 0;
        if (x.Category - y.Category != 0)
            return x.Category - y.Category;
        foreach (var rule in rules)
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