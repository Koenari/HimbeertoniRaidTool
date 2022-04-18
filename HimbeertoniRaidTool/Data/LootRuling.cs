using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class LootRuling
    {
        public static Random Random = new Random(Guid.NewGuid().GetHashCode());
        public static List<LootRule> PossibleRules
        {
            get
            {
                List<LootRule> result = new();
                foreach (LootRuleEnum rule in Enum.GetValues(typeof(LootRuleEnum)))
                    result.Add(new(rule));
                return result;
            }
        }
        [JsonProperty("RuleSet")]
        public List<LootRule> RuleSet = new();
        [JsonProperty("StrictRooling")]
        public bool StrictRooling = false;
        public List<(Player, string)> Evaluate(RaidGroup group, HrtItem inItem, List<Player>? excluded = null)
        {
            excluded ??= new();
            if (inItem.IsExhangableItem)
                return Evaluate(group, new ExchangableItem(inItem.ID).PossiblePurchases, excluded);
            else if (inItem.IsContainerItem)
                return Evaluate(group, new ContainerItem(inItem.ID).PossiblePurchases, excluded);
            else if (inItem.IsGear)
                return Evaluate(group, new List<GearItem> { new(inItem.ID) }, excluded);
            else
                return new();

        }
        private List<(Player, string)> Evaluate(RaidGroup group, List<GearItem> possibleItems, List<Player> excluded)
        {
            List<Player> need = new();
            List<Player> greed = new();
            foreach (Player p in group.Players)
            {
                if (excluded.Contains(p))
                    continue;
                foreach (GearItem item in possibleItems)
                {
                    if (p.Gear[item.Slot].ItemLevel < item.ItemLevel)
                    {
                        need.Add(p);
                        break;
                    }
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
        private LootRulingComparer GetComparer(List<GearItem> possibleItems) => new(RuleSet, possibleItems, StrictRooling);

        private class LootRulingComparer : IComparer<Player>
        {
            private readonly List<GearItem> PossibleItems;
            private readonly List<LootRule> RuleSet;
            public Dictionary<(Player, Player), LootRule> RulingReason = new();
            private readonly bool StrictRuling;
            public LootRulingComparer(List<LootRule> ruleSet, List<GearItem> possibleItems, bool strict)
            => (RuleSet, PossibleItems, StrictRuling) = (ruleSet, possibleItems, strict);

            public int Compare(Player? x, Player? y)
            {
                if (x is null && y is null)
                    return 0;
                if (x is null)
                    return 1;
                if (y is null)
                    return -1;
                if (RulingReason.ContainsKey((x, y)))
                    return RulingReason[(x, y)].Compare(x, y, PossibleItems, StrictRuling);
                foreach (LootRule rule in RuleSet)
                {
                    int result = rule.Compare(x, y, PossibleItems, StrictRuling);
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
    [JsonObject(MemberSerialization.OptIn)]
    public class LootRule
    {
        [JsonProperty("Rule")]
        public LootRuleEnum? Rule;
        public int Compare(Player x, Player y, List<GearItem> loot, bool strict) => Rule switch
        {
            LootRuleEnum.Random => LootRuling.Random.Next(0, 2) > 0 ? 1 : -1,
            LootRuleEnum.LowestItemLevel => x.Gear.ItemLevel - y.Gear.ItemLevel,
            LootRuleEnum.HighesItemLevelGain =>
                (int)loot.ConvertAll(item => item.ItemLevel - y.Gear[item.Slot].ItemLevel).Max() -
                    (int)loot.ConvertAll(item => item.ItemLevel - x.Gear[item.Slot].ItemLevel).Max(),
            LootRuleEnum.BISOverUpgrade =>
                (loot.Any(item => x.BIS.Contains(item) && !x.Gear.Contains(item)) ? -1 : 1) -
                    (loot.Any(item => y.BIS.Contains(item) && !y.Gear.Contains(item)) ? -1 : 1),
            LootRuleEnum.ByPosition => x.Pos.LootImportance(strict) - y.Pos.LootImportance(strict),
            _ => 0
        };
        public override string ToString() => Rule switch
        {
            LootRuleEnum.BISOverUpgrade => Localize("BISOverUpgrade", "BIS > Upgrade"),
            LootRuleEnum.LowestItemLevel => Localize("LowestItemLevel", "Lowest overall ItemLevel"),
            LootRuleEnum.HighesItemLevelGain => Localize("HighesItemLevelGain", "Highest ItemLevel Gain"),
            LootRuleEnum.ByPosition => Localize("ByPosition", "DPS > Tank > Heal"),
            LootRuleEnum.Random => Localize("Rolling", "Rolling"),
            null => Localize("None", "None"),
            _ => Localize("Not defined", "Not defined"),
        };

        public override bool Equals(object? obj)
        {
            if (obj is null || !obj.GetType().Equals(typeof(LootRule)))
                return false;
            return ((LootRule)obj).Rule == Rule;
        }

        [JsonConstructor]
        public LootRule(LootRuleEnum? rule = null) => Rule = rule;

        public override int GetHashCode() => Rule.GetHashCode();
    }

    public static class LootRulesExtension
    {
        public static int LootImportance(this PositionInRaidGroup pos, bool strict = false)
        {
            return pos switch
            {
                PositionInRaidGroup.Melee1 => strict ? 0 : 0,
                PositionInRaidGroup.Melee2 => strict ? 1 : 0,
                PositionInRaidGroup.Caster => strict ? 2 : 0,
                PositionInRaidGroup.Ranged => strict ? 3 : 0,
                PositionInRaidGroup.Tank1 => strict ? 4 : 4,
                PositionInRaidGroup.Tank2 => strict ? 5 : 4,
                PositionInRaidGroup.Heal1 => strict ? 6 : 6,
                PositionInRaidGroup.Heal2 => strict ? 7 : 6,
                _ => 8
            };
        }
    }
}
