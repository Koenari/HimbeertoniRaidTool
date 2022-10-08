using System;
using System.Collections.Generic;
using System.Linq;
using HimbeertoniRaidTool.Modules.LootMaster;
using Lumina.Excel.Extensions;
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
                {
                    if (rule == LootRuleEnum.None)
                        continue;
                    //Not yet functional
                    if (rule == LootRuleEnum.Custom)
                        continue;
                    result.Add(new(rule));
                }
                return result;
            }
        }
        [JsonProperty("RuleSet")]
        public List<LootRule> RuleSet = new();
    }



    [JsonObject(MemberSerialization.OptIn)]
    public class LootRule
    {
        [JsonProperty("Rule")]
        public readonly LootRuleEnum Rule;
        [JsonProperty("Name")]
        public readonly string Name;
        [JsonProperty("Expression")]
        public string Expression;
        public (int, string, string) Compare(Player x, Player y, LootSession session, List<GearItem> currentPossibleLoot)
        {
            (int lVal, string lReason) = Eval(x, session, currentPossibleLoot);
            (int rVal, string rReason) = Eval(y, session, currentPossibleLoot);
            return ((this.FavorsLowValue() ? -1 : 1) * (rVal - lVal), lReason, rReason);
        }
        private (int, string) Eval(Player x, LootSession session, List<GearItem> currentPossibleLoot) => Rule switch
        {
            LootRuleEnum.Random => DuplicateToString(x.Roll(session)),
            LootRuleEnum.LowestItemLevel => DuplicateToString(x.ItemLevel()),
            LootRuleEnum.HighesItemLevelGain => DuplicateToString(x.ItemLevelGain(x.ApplicableItem(currentPossibleLoot))),
            LootRuleEnum.BISOverUpgrade => x.IsBiS(currentPossibleLoot) ? (1, "y") : (-1, "n"),
            LootRuleEnum.ByPosition => (x.RolePriority(session._group), x.MainChar.MainJob.GetRole().ToString()),
            _ => (0, "none")
        };
        private static (int, string) DuplicateToString(int val) => (val, $"{val}");
        public override string ToString() => Name;
        private string GetName() => Rule switch
        {
            LootRuleEnum.BISOverUpgrade => Localize("BISOverUpgrade", "BIS > Upgrade"),
            LootRuleEnum.LowestItemLevel => Localize("LowestItemLevel", "Lowest overall ItemLevel"),
            LootRuleEnum.HighesItemLevelGain => Localize("HighesItemLevelGain", "Highest ItemLevel Gain"),
            LootRuleEnum.ByPosition => Localize("ByPosition", "DPS > Tank > Heal"),
            LootRuleEnum.Random => Localize("Rolling", "Rolling"),
            LootRuleEnum.None => Localize("None", "None"),
            _ => Localize("Not defined", "Not defined"),
        };

        public override bool Equals(object? obj)
        {
            if (obj is null || !obj.GetType().Equals(typeof(LootRule)))
                return false;
            return ((LootRule)obj).Rule == Rule;
        }

        [JsonConstructor]
        public LootRule(LootRuleEnum rule, string? name = null)
        {
            Rule = rule;
            //TODO: implement correctly
            Expression = "";
            Name = name ?? GetName();
        }

        public override int GetHashCode() => Rule.GetHashCode();
    }

    public static class LootRulesExtension
    {
        public static bool FavorsLowValue(this LootRule rule) => rule.Rule switch
        {
            LootRuleEnum.LowestItemLevel => true,
            LootRuleEnum.ByPosition => true,
            _ => false,
        };
        public static int Roll(this Player p, LootSession session) => session.Rolls[p];
        public static int ItemLevel(this Player p) => p.Gear.ItemLevel;
        public static int ItemLevelGain(this Player p, GearItem? newItem) => newItem == null ? 0 : (int)newItem.ItemLevel - (int)p.Gear[newItem.Slot].ItemLevel;
        public static bool IsBiS(this Player p, List<GearItem> items) => p.BIS.Any(bisItem => items.Any(i => i.ID == bisItem.ID));
        public static GearItem? ApplicableItem(this Player p, List<GearItem> possibleItems)
        {
            if (!possibleItems.Any(i => i.Item?.ClassJobCategory.Value.Contains(p.MainChar.MainJob) ?? false))
                return null;
            return possibleItems.First(i => i.Item?.ClassJobCategory.Value.Contains(p.MainChar.MainJob) ?? false);
        }
        public static int RolePriority(this Player p, RaidGroup g) => p.MainChar.MainJob.GetRole() switch
        {
            Role.Melee => 0,
            Role.Caster => 2,
            Role.Ranged => 2,
            Role.Tank => 4,
            Role.Healer => 6,
            _ => 8
        };
    }
}
