using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace HimbeertoniRaidTool.Data
{
    public class LootRuling
    {
        private readonly List<LootRule> _Rules = new();

        private readonly ObservableCollection<LootRules> _RuleSet = new();
        public ObservableCollection<LootRules> RuleSet => _RuleSet; 

        public LootRuling()
        {
            _RuleSet.CollectionChanged += UpdateRules;
        }
        private void UpdateRules(object? sender, NotifyCollectionChangedEventArgs e) => UpdateRules();
        private void UpdateRules()
        {
            throw new NotImplementedException();
        }

        public List<Player> Evaluate(RaidGroup group, GearSetSlot slot)
        {
            List<Player> result = new();
            foreach(Player p in group.Players)
            {
                if (p.MainChar.MainClass.Gear.Get(slot).ItemLevel < p.MainChar.MainClass.BIS.Get(slot).ItemLevel)
                    result.Add(p);
            }
            result.Sort(GetComparer(slot));
            return result;
            
        }
        private LootRulingComparer GetComparer(GearSetSlot slot) => new(_Rules,slot);
        class LootRulingComparer:  IComparer<Player>
        {
            private readonly GearSetSlot Slot;
            private readonly List<LootRule> RuleSet;
            public LootRulingComparer(List<LootRule> ruleSet, GearSetSlot slot) => (RuleSet,Slot) = (ruleSet,slot);

            public int Compare(Player? x, Player? y)
            {
                if (x is null && y is null)
                    return 0;
                if (x is null)
                    return 1;
                if (y is null)
                    return -1;
                foreach(LootRule rule in RuleSet)
                {
                    int result = rule.Compare(x, y, Slot);
                    if (result != 0)
                        return result;
                }
                return 0;
            }
        }
    }
    public class LootRule 
    {
        public LootRules Rule;
        [JsonIgnore]
        public string Name => Rule.AsString();

        public int Compare(Player x, Player y, GearSetSlot slot)
        {
            
            throw new System.NotImplementedException();
        }
    }
    public enum LootRules
    {
        BISOverUpgrade,
        LowestItemLevel,
        HighesItemLevelGain,
        ByPosition,
        Random
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
                        _ => "",
            };
        }

        public static uint ItemLevel(this RaidTier rt)
        {
            return (uint)rt;
        }

    }
}
