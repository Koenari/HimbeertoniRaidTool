using System;
using System.Collections.Generic;
using System.Linq;
using HimbeertoniRaidTool.Modules.LootMaster;
using ImGuiNET;
using Lumina.Excel.Extensions;
using Newtonsoft.Json;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class LootRuling
    {
        public static IEnumerable<LootRule> PossibleRules
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
    [JsonDictionary]
    public class RolePriority : Dictionary<Role, int>
    {
        private int Max => this.Aggregate(0, (sum, x) => Math.Max(sum, x.Value));
        public int GetPriority(Role r) => ContainsKey(r) ? this[r] : Max + 1;

        public void DrawEdit()
        {
            foreach (Role r in Enum.GetValues<Role>())
            {
                if (r == Role.None)
                    continue;
                if (!ContainsKey(r))
                {
                    Add(r, Max + 1);
                }
                int val = this[r];
                if (ImGui.InputInt($"{r}##RolePriority", ref val))
                {
                    this[r] = Math.Max(val, 0);
                }
            }
        }
        public override string ToString()
        {

            if (Count == 0)
                return string.Join(" = ", Enum.GetNames<Role>());
            List<KeyValuePair<Role, int>> ordered = this.ToList();
            ordered.Sort((l, r) => l.Value - r.Value);
            string result = "";
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                result += $"{ordered[i].Key} {(ordered[i].Value < ordered[i + 1].Value ? ">" : "=")} ";
            }
            result += ordered[^1].Key;
            List<Role> missing = Enum.GetValues<Role>().Where(r => !ContainsKey(r)).Where(r => r != Role.None).ToList();
            if (missing.Any())
            {
                result += " > ";
                for (int j = 0; j < missing.Count - 1; j++)
                {
                    result += $"{missing[j]} = ";
                }
                result += missing[^1].ToString();
            }
            return result;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class LootRule
    {
        [JsonProperty("Rule")]
        public readonly LootRuleEnum Rule;
        [JsonProperty("Name")]
        public readonly string Name;
        [JsonIgnore]
        public string Expression;
        public (int, string, string) Compare(Player x, Player y, LootSession session, IEnumerable<GearItem> currentPossibleLoot)
        {
            (int lVal, string? lReason) = Eval(x, session, currentPossibleLoot);
            (int rVal, string? rReason) = Eval(y, session, currentPossibleLoot);
            return ((this.FavorsLowValue() ? -1 : 1) * (rVal - lVal), lReason ?? lVal.ToString(), rReason ?? rVal.ToString());
        }
        private (int, string?) Eval(Player x, LootSession session, IEnumerable<GearItem> currentPossibleLoot) => Rule switch
        {
            LootRuleEnum.Random => (x.Roll(session), null),
            LootRuleEnum.LowestItemLevel => (x.ItemLevel(), null),
            LootRuleEnum.HighesItemLevelGain => (x.ItemLevelGain(x.ApplicableItem(currentPossibleLoot)), null),
            LootRuleEnum.BISOverUpgrade => x.IsBiS(currentPossibleLoot) ? (1, "y") : (-1, "n"),
            LootRuleEnum.RolePrio => (session.RolePriority.GetPriority(x.MainChar.MainJob.GetRole()), x.MainChar.MainJob.GetRole().ToString()),
            LootRuleEnum.DPS => (x.AdditionalData.ManualDPS, null),
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
            LootRuleEnum.RolePrio => true,
            _ => false,
        };
        public static int Roll(this Player p, LootSession session) => session.Rolls[p];
        public static int ItemLevel(this Player p) => p.Gear.ItemLevel;
        public static int ItemLevelGain(this Player p, GearItem? newItem) => newItem == null ? 0 : (int)newItem.ItemLevel - (int)p.Gear[newItem.Slot].ItemLevel;
        public static bool IsBiS(this Player p, IEnumerable<GearItem> items) => p.BIS.Any(bisItem => items.Any(i => i.ID == bisItem.ID));
        public static GearItem? ApplicableItem(this Player p, IEnumerable<GearItem> possibleItems)
        {
            if (!possibleItems.Any(i => i.Item?.ClassJobCategory.Value.Contains(p.MainChar.MainJob) ?? false))
                return null;
            return possibleItems.First(i => i.Item?.ClassJobCategory.Value.Contains(p.MainChar.MainJob) ?? false);
        }
    }
}
