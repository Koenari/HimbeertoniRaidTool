using System;
using System.Collections.Generic;
using System.Linq;
using HimbeertoniRaidTool.LootMaster;
using Newtonsoft.Json;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
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
        [JsonProperty("RuleSet")]
        public List<LootRule> RuleSet = new();
        [JsonProperty("StrictRooling")]
        public bool StrictRooling = false;
    }



    [JsonObject(MemberSerialization.OptIn)]
    public class LootRule
    {
        [JsonProperty("Rule")]
        public LootRuleEnum? Rule;
        public int Compare(Player x, Player y, LootSession session, List<GearItem> currentPossibleLoot) => Rule switch
        {
            LootRuleEnum.Random => session.Rolls[y] - session.Rolls[x],
            LootRuleEnum.LowestItemLevel => x.Gear.ItemLevel - y.Gear.ItemLevel,
            LootRuleEnum.HighesItemLevelGain =>
                (int)currentPossibleLoot.ConvertAll(item => item.ItemLevel - y.Gear[item.Slot].ItemLevel).Max() -
                    (int)currentPossibleLoot.ConvertAll(item => item.ItemLevel - x.Gear[item.Slot].ItemLevel).Max(),
            LootRuleEnum.BISOverUpgrade =>
                (currentPossibleLoot.Any(item => x.BIS.Contains(item) && !x.Gear.Contains(item)) ? -1 : 1) -
                    (currentPossibleLoot.Any(item => y.BIS.Contains(item) && !y.Gear.Contains(item)) ? -1 : 1),
            LootRuleEnum.ByPosition =>
                x.Pos.LootImportance(session.RulingOptions.StrictRooling)
                    - y.Pos.LootImportance(session.RulingOptions.StrictRooling),
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
        public static int LootImportance(this PositionInRaidGroup pos, bool strict = false) => pos switch
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
