using HimbeertoniRaidTool.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using static HimbeertoniRaidTool.Data.Player;

namespace HimbeertoniRaidTool.Data
{
    public class LootRuling
    {
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
        public List<LootRule> RuleSet = new();
        public bool StrictRooling = false;
        
        public List<(Player,LootRule)> Evaluate(RaidGroup group, GearSetSlot slot, List<Player> excluded = null)
        {
            excluded ??= new();
            List<(Player, LootRule)> result = new();
            List<Player> greed = new();
            foreach (Player p in group.Players)
            {
                if (excluded.Contains(p))
                    continue;
                if (p.MainChar.MainClass.Gear[slot].ItemLevel < p.MainChar.MainClass.BIS[slot].ItemLevel)
                {
                    result.Add((p, null!));
                }else
                {
                    greed.Add(p);
                }
                    
            }
            result.Sort(GetComparer(slot));
            foreach(Player p in greed)
            {
                result.Add((p, new(null)));
            }
            return result;

        }
        private LootRulingComparer GetComparer(GearSetSlot slot) => new(RuleSet, slot, StrictRooling);

        private class LootRulingComparer : IComparer<(Player, LootRule)>
        {
            private readonly GearSetSlot Slot;
            private readonly List<LootRule> RuleSet;
            private readonly bool StrictRuling;
            public LootRulingComparer(List<LootRule> ruleSet, GearSetSlot slot, bool strict) => (RuleSet, Slot, StrictRuling) = (ruleSet, slot, strict);

            public int Compare((Player, LootRule) x, (Player, LootRule) y)
            {
                if (x.Item1 is null && y.Item1 is null)
                    return 0;
                if (x.Item1 is null)
                    return 1;
                if (y.Item1 is null)
                    return -1;
                foreach (LootRule rule in RuleSet)
                {
                    int result = rule.Compare(x.Item1, y.Item1, Slot, StrictRuling);
                    if (result != 0)
                    {
                        x.Item2 = rule;
                        return result;
                    }
                }
                x.Item2 = new();
                return 0;
            }
        }
    }
    public class LootRule
    {
        public LootRuleEnum? Rule;
        [JsonIgnore]
        public Func<Player, Player, GearSetSlot, bool, int> Compare
        {
            get
            {
                if (Rule == null)
                    return (x, y, z, strict) => 0;
                 return CompareDic.GetValueOrDefault((LootRuleEnum)Rule!) ?? ((x, y, z, strict) => 0);
            }
        }
        private readonly static Dictionary<LootRuleEnum, Func<Player, Player, GearSetSlot, bool, int>> CompareDic = new()
        {
            { LootRuleEnum.Random, (x, y, slot, strict) => new Random().Next(0, 1) > 0 ? 1 : -1 },
            { LootRuleEnum.LowestItemLevel, (x, y, slot, strict) => x.Gear.ItemLevel - y.Gear.ItemLevel },
            { LootRuleEnum.HighesItemLevelGain, (x, y, slot, strict) => ((int)x.Gear[slot].ItemLevel) - (int)y.Gear[slot].ItemLevel },
            {
                LootRuleEnum.BISOverUpgrade,
                (x, y, slot, strict) =>
                {
                    int xBIS = x.BIS[slot].Source == GearSource.Raid ? -1 : 1;
                    int yBIS = y.BIS[slot].Source == GearSource.Raid ? -1 : 1;
                    return xBIS - yBIS;
                }
            },
            {LootRuleEnum.ByPosition, (x, y, slot, strict) => x.Pos.LootImportance(slot, strict) - y.Pos.LootImportance(slot, strict) }
        };
        public override string ToString()
        {
            return Rule switch
            {
                LootRuleEnum.BISOverUpgrade => "BIS > Upgrade",
                LootRuleEnum.LowestItemLevel => "Lowest overall ItemLevel",
                LootRuleEnum.HighesItemLevelGain => "Highest ItemLevel Gain",
                LootRuleEnum.ByPosition => "DPS > Tank > Heal",
                LootRuleEnum.Random => "Rolling",
                null => "None",
                _ => "Not defined",
            };
        }

        public override bool Equals(object? obj)
        {
            if (obj is null || !obj.GetType().Equals(typeof(LootRule)))
                return false;
             return ((LootRule)obj).Rule.Equals(Rule);
        }


        public LootRule(LootRuleEnum? rule = null) => Rule = rule;

        public override int GetHashCode() => Rule.GetHashCode();
    }
   
    public static class LootRulesExtension
    {
        public static int LootImportance(this PositionInRaidGroup pos, GearSetSlot? slot = null, bool strict = false)
        {
            return pos switch
            {
                PositionInRaidGroup.Melee1 => strict ? 0 : 0,
                PositionInRaidGroup.Melee2 => strict ? 1 : 0,
                PositionInRaidGroup.Caster => strict ? 2 : 0,
                PositionInRaidGroup.Ranged => strict ? 3 : 0,
                PositionInRaidGroup.Tank1 => slot != GearSetSlot.MainHand ? (strict ? 4 : 4) : (strict ? 6 : 6),
                PositionInRaidGroup.Tank2 => slot != GearSetSlot.MainHand ? (strict ? 5 : 4) : (strict ? 7 : 6),
                PositionInRaidGroup.Heal1 => slot != GearSetSlot.MainHand ? (strict ? 6 : 6) : (strict ? 4 : 4),
                PositionInRaidGroup.Heal2 => slot != GearSetSlot.MainHand ? (strict ? 7 : 6) : (strict ? 5 : 4),
                _ => 8
            };
        }
    }
}
