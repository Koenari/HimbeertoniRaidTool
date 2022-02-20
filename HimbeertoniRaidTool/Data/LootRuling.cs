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
        private readonly List<LootRule> _Rules = new();

        private ObservableCollection<LootRules> _RuleSet = new();
        public bool StrictRooling = false;
        public IEnumerable<LootRules> RuleSet
        {
            get => _RuleSet;
            set
            {
                _RuleSet.CollectionChanged -= UpdateRules;
                _RuleSet= new(value.Distinct());
                _RuleSet.CollectionChanged += UpdateRules;
                UpdateRules();
            }
        }

        public LootRuling()
        {
            _RuleSet.CollectionChanged += UpdateRules;
        }
        private void UpdateRules(object? sender, NotifyCollectionChangedEventArgs e) => UpdateRules();
        private void UpdateRules()
        {
            _RuleSet.CollectionChanged -= UpdateRules;
            if (_RuleSet.Count != _RuleSet.Distinct().Count()) 
            {
                _RuleSet = new(_RuleSet.Distinct());
            }
            _Rules.Clear();
            foreach (LootRules lr in _RuleSet)
                _Rules.Add(new(lr));
            _RuleSet.CollectionChanged += UpdateRules;
        }

        public List<(Player,LootRules)> Evaluate(RaidGroup group, GearSetSlot slot)
        {
            List<(Player, LootRules)> result = new();
            foreach (Player p in group.Players)
            {
                if (p.MainChar.MainClass.Gear[slot].ItemLevel < p.MainChar.MainClass.BIS[slot].ItemLevel)
                    result.Add((p,LootRules.Null));
            }
            result.Sort(GetComparer(slot));
            return result;

        }
        private LootRulingComparer GetComparer(GearSetSlot slot) => new(_Rules, slot);

        private class LootRulingComparer : IComparer<(Player, LootRules)>
        {
            private readonly GearSetSlot Slot;
            private readonly List<LootRule> RuleSet;
            public LootRulingComparer(List<LootRule> ruleSet, GearSetSlot slot) => (RuleSet, Slot) = (ruleSet, slot);

            public int Compare((Player, LootRules) x, (Player, LootRules) y)
            {
                if (x.Item1 is null && y.Item1 is null)
                    return 0;
                if (x.Item1 is null)
                    return 1;
                if (y.Item1 is null)
                    return -1;
                foreach (LootRule rule in RuleSet)
                {
                    int result = rule.Compare(x.Item1, y.Item1, Slot);
                    if (result != 0)
                    {
                        x.Item2 = rule.Rule;
                        return result;
                    }
                }
                x.Item2 = LootRules.Null;
                return 0;
            }
        }
    }
    public class LootRule
    {
        private readonly static Dictionary<LootRules, Func<Player, Player, GearSetSlot, int>> CompareDic = new()
        {
            { LootRules.Null, (x, y, slot) => 0 },
            { LootRules.Random, (x, y, slot) => new Random().Next(0, 1) > 0 ? 1 : -1 },
            { LootRules.LowestItemLevel, (x, y, slot) => x.Gear.ItemLevel - y.Gear.ItemLevel },
            { LootRules.HighesItemLevelGain, (x, y, slot) => ((int)x.Gear[slot].ItemLevel) - (int)y.Gear[slot].ItemLevel },
            {
                LootRules.BISOverUpgrade,
                (x, y, slot) =>
                {
                    int xBIS = x.BIS[slot].Source == GearSource.Raid ? -1 : 1;
                    int yBIS = y.BIS[slot].Source == GearSource.Raid ? -1 : 1;
                    return xBIS - yBIS;
                }
            },
            {LootRules.ByPosition, (x, y, slot) => x.Pos.LootImportance(slot, StrictRooling) - y.Pos.LootImportance(slot, StrictRooling) }
        };
        public LootRules Rule;
        
        [JsonIgnore]
        public string Name => Rule.AsString();
        private static bool StrictRooling;

        public Func<Player, Player, GearSetSlot, int> Compare;
        public LootRule() : this(LootRules.Null) { }
        public LootRule(LootRules rule, bool strict = false) => (Compare, StrictRooling) = (CompareDic.GetValueOrDefault(rule, (x, y, z) => 0), strict);
    }
    public enum LootRules
    {
        Null = 0,
        BISOverUpgrade = 1,
        LowestItemLevel = 2,
        HighesItemLevelGain = 3,
        ByPosition = 4,
        Random = 5,
    }
    public enum RaidTier
    {
        Asphodelos = 600
    }
    public static class LootRulesExtension
    {
        public static string AsString(this LootRules lr)
        {
            return lr switch
            {
                LootRules.BISOverUpgrade => "BIS > Upgrade",
                LootRules.LowestItemLevel => "Lowest overall ItemLevel",
                LootRules.HighesItemLevelGain => "Highest ItemLevel Gain",
                LootRules.ByPosition => "DPS > Tank > Heal",
                LootRules.Random => "Rolling",
                LootRules.Null => "None",
                _ => "",
            };
        }

        public static uint ItemLevel(this RaidTier rt)
        {
            return (uint)rt;
        }
        public static int LootImportance(this Position pos, GearSetSlot? slot = null, bool strict = false)
        {
            return pos switch
            {
                Position.Melee1 => strict ? 0 : 0,
                Position.Melee2 => strict ? 1 : 0,
                Position.Caster => strict ? 2 : 0,
                Position.Ranged => strict ? 3 : 0,
                Position.Tank1 => slot != GearSetSlot.MainHand ? (strict ? 4 : 4) : (strict ? 6 : 6),
                Position.Tank2 => slot != GearSetSlot.MainHand ? (strict ? 5 : 4) : (strict ? 7 : 6),
                Position.Heal1 => slot != GearSetSlot.MainHand ? (strict ? 6 : 6) : (strict ? 4 : 4),
                Position.Heal2 => slot != GearSetSlot.MainHand ? (strict ? 7 : 6) : (strict ? 5 : 4),
                _ => 8
            };
        }
    }
}
