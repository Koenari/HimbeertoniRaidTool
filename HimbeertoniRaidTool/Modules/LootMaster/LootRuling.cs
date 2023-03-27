using System.Data;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Calculations;
using HimbeertoniRaidTool.Common.Data;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class LootRuling
{
    public static readonly LootRule Default = new(LootRuleEnum.None);
    public static readonly LootRule NeedOverGreed = new(LootRuleEnum.NeedGreed);
    public static IEnumerable<LootRule> PossibleRules
    {
        get
        {
            foreach (LootRuleEnum rule in Enum.GetValues(typeof(LootRuleEnum)))
            {
                if (rule is LootRuleEnum.None)
                    continue;
                //Special Rules only used internally
                if ((int)rule > 900)
                    continue;
                yield return new(rule);
            }
        }
    }
    [JsonProperty("RuleSet")]
    public List<LootRule> RuleSet = new();
}


[JsonObject(MemberSerialization.OptIn)]
public class LootRule : IEquatable<LootRule>
{
    [JsonProperty("Rule")]
    public readonly LootRuleEnum Rule;
    public string Name => GetName();
    /// <summary>
    /// Evaluates this LootRule for given player
    /// </summary>
    /// <param name="x">The player to evaluate for</param>
    /// <param name="session">Loot session to evaluate for</param>
    /// <param name="applicableItems">List of items to evaluate for. These need to be filtered to be equippable by the players MainJob</param>
    /// <returns>A tuple of int (can be used for Compare like (right - left)) and a string describing the value</returns>
    public (float, string) Eval(LootResult x, LootSession session)
    {
        (float val, string? reason) = InternalEval(x, session);
        return (val, reason ?? val.ToString());
    }
    private (float, string?) InternalEval(LootResult x, LootSession session) => Rule switch
    {
        LootRuleEnum.Random => (x.Roll(), null),
        LootRuleEnum.LowestItemLevel => (-x.ItemLevel(), x.ItemLevel().ToString()),
        LootRuleEnum.HighesItemLevelGain => (x.ItemLevelGain(), null),
        LootRuleEnum.BISOverUpgrade => x.IsBiS() ? (1, "y") : (-1, "n"),
        LootRuleEnum.RolePrio => (x.RolePrio(session), x.AplicableJob.Role.ToString()),
        LootRuleEnum.DPS => (x.Player.AdditionalData.ManualDPS, null),
        LootRuleEnum.DPSGain => (x.DPSGain(), $"{(x.DPSGain() * 100):f1} %%"),
        _ => (0, "none"),
    };
    public override string ToString() => Name;
    private string GetName() => Rule switch
    {
        LootRuleEnum.BISOverUpgrade => Localize("BISOverUpgrade", "BIS > Upgrade"),
        LootRuleEnum.LowestItemLevel => Localize("LowestItemLevel", "Lowest overall ItemLevel"),
        LootRuleEnum.HighesItemLevelGain => Localize("HighesItemLevelGain", "Highest ItemLevel Gain"),
        LootRuleEnum.RolePrio => Localize("ByRole", "Prioritise by role"),
        LootRuleEnum.Random => Localize("Rolling", "Rolling"),
        LootRuleEnum.DPS => Localize("DPS", "DPS"),
        LootRuleEnum.DPSGain => Localize("DPSGain", "% DPS gained"),
        LootRuleEnum.None => Localize("None", "None"),
        LootRuleEnum.Greed => Localize("Greed", "Greed"),
        LootRuleEnum.NeedGreed => Localize("Need over Greed", "Need over Greed"),
        _ => Localize("Not defined", "Not defined"),
    };
    [JsonConstructor]
    public LootRule(LootRuleEnum rule)
    {
        Rule = rule;
    }

    public override int GetHashCode() => Rule.GetHashCode();
    public override bool Equals(object? obj) => Equals(obj as LootRule);
    public bool Equals(LootRule? obj) => obj?.Rule == Rule;
    public static bool operator ==(LootRule l, LootRule r) => l.Equals(r);
    public static bool operator !=(LootRule l, LootRule r) => !l.Equals(r);

}

public static class LootRulesExtension
{
    public static int RolePrio(this LootResult p, LootSession s) => -s.RolePriority.GetPriority(p.AplicableJob.Role);
    public static int Roll(this LootResult p) => p.Roll;
    public static int ItemLevel(this LootResult p) => p.AplicableJob.Gear.ItemLevel;
    public static int ItemLevelGain(this LootResult p)
    {
        int result = 0;
        foreach (var item in p.NeededItems)
        {
            result = Math.Max(result, (int)item.ItemLevel -
                p.AplicableJob.Gear.
                    Where(i => i.Slots.Intersect(item.Slots).Any()).
                    Aggregate((int)item.ItemLevel, (min, i) => Math.Min((int)i.ItemLevel, min))
                );
        }
        return result;
    }
    public static float DPSGain(this LootResult p)
    {
        var curClass = p.AplicableJob;
        double baseDPS = AllaganLibrary.EvaluateStat(StatType.PhysicalDamage, curClass, curClass.Gear);
        double newDps = double.NegativeInfinity;
        foreach (var i in p.ApplicableItems)
        {
            var item = i.Clone();
            foreach (var mat in curClass.Gear[i.Slots.First()].Materia)
                item.AddMateria(mat);
            double cur = AllaganLibrary.EvaluateStat(StatType.PhysicalDamage, curClass, curClass.Gear.With(item));
            if (cur > newDps)
                newDps = cur;
        }
        return (float)((newDps - baseDPS) / baseDPS);
    }
    //IS broken for non unique items
    public static bool IsBiS(this LootResult p) =>
        p.NeededItems.Any(i => p.AplicableJob.BIS.Contains(i) && !p.AplicableJob.Gear.Contains(i));
}
