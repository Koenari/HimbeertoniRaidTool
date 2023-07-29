using System.Globalization;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Calculations;
using HimbeertoniRaidTool.Common.Data;
using Newtonsoft.Json;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.CustomSheets;
using static HimbeertoniRaidTool.Plugin.Services.Localization;
using ServiceManager = HimbeertoniRaidTool.Plugin.Services.ServiceManager;

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
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (LootRuleEnum rule in Enum.GetValues(typeof(LootRuleEnum)))
            {
#pragma warning disable CS0618
                if (rule is LootRuleEnum.None or LootRuleEnum.DPS)
#pragma warning restore CS0618
                    continue;
                //Special Rules only used internally
                if ((int)rule > 900)
                    continue;
                yield return new LootRule(rule);
            }
        }
    }
    [JsonProperty("RuleSet")]
    public List<LootRule> RuleSet = new();
    
    [JsonIgnore]
    public IEnumerable<LootRule> ActiveRules => RuleSet.Where(r => r.Active);
}


[JsonObject(MemberSerialization.OptIn)]
public class LootRule : IEquatable<LootRule>, IDrawable
{
    [JsonProperty("Rule")] public readonly LootRuleEnum Rule;

    [JsonProperty("Active")] public bool Active = true;

    [JsonProperty("IgnoreActive")] public bool IgnorePlayers;

    public bool CanIgnore=>
        Rule switch
        {
            LootRuleEnum.BISOverUpgrade => true,
            LootRuleEnum.CanUse => true,
            LootRuleEnum.CanBuy => true,
            LootRuleEnum.NeedGreed => true,
            _ => false,
        };

    public string Name => GetName();

    public void Draw()
    {
        ImGui.Checkbox("##active", ref Active);
        ImGuiHelper.AddTooltip(Localize("ui:loot_rule:active:tooltip","Activated"));
        ImGui.SameLine();
        ImGui.BeginDisabled(!Active);
        ImGui.Text(Name);
        if (CanIgnore)
        {
            ImGui.SameLine();
            ImGui.Checkbox($"{Localize("ui:loot_rule:ignore","ShouldIgnore")}##ignore", ref IgnorePlayers);
            ImGuiHelper.AddTooltip(IgnoreTooltip);
        }
        ImGui.EndDisabled();
    }

    private string IgnoreTooltip =>
        Rule switch
        {
            LootRuleEnum.BISOverUpgrade => Localize("loot_rule:ignore:tooltip:bis","ShouldIgnore players/jobs not using this in BiS "),
            LootRuleEnum.CanUse => Localize("loot_rule:ignore:tooltip:can_use","ShouldIgnore players/jobs not able to use"),
            LootRuleEnum.CanBuy => Localize("loot_rule:ignore:tooltip:can_buy","ShouldIgnore players/jobs that could buy these"),
            LootRuleEnum.NeedGreed => Localize("loot_rule:ignore:tooltip:need_greed","ShouldIgnore players that have no need"),
            _ => "",
        };

    /// <summary>
    /// Evaluates this LootRule for given player
    /// </summary>
    /// <param name="x">The player to evaluate for</param>
    /// <returns>A tuple of int (can be used for Compare like (right - left)) and a string describing the value</returns>
    public (float, string) Eval(LootResult x)
    {
        (float val, string? reason) = InternalEval(x);
        return (val, reason ?? val.ToString(CultureInfo.CurrentCulture));
    }

    public bool ShouldIgnore(LootResult x) => CanIgnore && IgnorePlayers && Rule switch
    {
        LootRuleEnum.BISOverUpgrade => !x.IsBiS(),
        LootRuleEnum.CanUse => !x.CanUse(),
        LootRuleEnum.CanBuy => x.CanBuy(), 
        LootRuleEnum.Greed => true,
        _ => false,
    };

    private (float, string?) InternalEval(LootResult x) => Rule switch
    {
        LootRuleEnum.Random => (x.Roll(), null),
        LootRuleEnum.LowestItemLevel => (-x.ItemLevel(), x.ItemLevel().ToString()),
        LootRuleEnum.HighestItemLevelGain => (x.ItemLevelGain(), null),
        LootRuleEnum.BISOverUpgrade => x.IsBiS() ? (1, "y") : (-1, "n"),
        LootRuleEnum.RolePrio => (x.RolePriority(), x.ApplicableJob.Role.ToString()),
        LootRuleEnum.DPSGain => (x.DpsGain(), $"{x.DpsGain() * 100:f1} %%"),
        LootRuleEnum.CanUse => x.CanUse() ? (1, "y") : (-1, "n"),
        LootRuleEnum.CanBuy => x.CanBuy() ? (-1, "y") : (1, "n"),
        _ => (0, "none"),
    };
    public override string ToString() => Name;
    private string GetName() => Rule switch
    {
        LootRuleEnum.BISOverUpgrade => Localize("BISOverUpgrade", "BIS > Upgrade"),
        LootRuleEnum.LowestItemLevel => Localize("LowestItemLevel", "Lowest overall ItemLevel"),
        LootRuleEnum.HighestItemLevelGain => Localize("HighestItemLevelGain", "Highest ItemLevel Gain"),
        LootRuleEnum.RolePrio => Localize("ByRole", "Prioritize by role"),
        LootRuleEnum.Random => Localize("Rolling", "Rolling"),
        LootRuleEnum.DPSGain => Localize("DPSGain", "% DPS gained"),
        LootRuleEnum.CanUse => Localize("LootRule:CanUse","Can use now"),
        LootRuleEnum.CanBuy => Localize("LootRule:CanBuy", "Can buy"),
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
    public static int RolePriority(this LootResult p) => -p.RolePriority;
    public static int Roll(this LootResult p) => p.Roll;
    public static int ItemLevel(this LootResult p) => p.ApplicableJob.Gear.ItemLevel;
    public static int ItemLevelGain(this LootResult p)
    {
        return p.NeededItems.Select(item => (int)item.ItemLevel - p.ApplicableJob.Gear
            .Where(i => i.Slots.Intersect(item.Slots).Any())
            .Aggregate((int)item.ItemLevel, (min, i) => Math.Min((int)i.ItemLevel, min))).Prepend(0).Max();
    }
    public static float DpsGain(this LootResult p)
    {
        var curClass = p.ApplicableJob;
        double baseDps = AllaganLibrary.EvaluateStat(StatType.PhysicalDamage, curClass, curClass.Gear);
        double newDps = double.NegativeInfinity;
        foreach (var i in p.ApplicableItems)
        {
            GearItem? item = null;
            foreach (var bisItem in curClass.BIS)
            {
                if (bisItem.Equals(i, ItemComparisonMode.IdOnly))
                    item = bisItem.Clone();
            }
            if (item is null)
            {
                item ??= i.Clone();
                foreach (var mat in curClass.Gear[i.Slots.First()].Materia)
                    item.AddMateria(mat);
            }
            double cur = AllaganLibrary.EvaluateStat(StatType.PhysicalDamage, curClass, curClass.Gear.With(item));
            if (cur > newDps)
                newDps = cur;
        }
        return (float)((newDps - baseDps) / baseDps);
    }
    public static bool IsBiS(this LootResult p) =>
        p.NeededItems.Any(i => p.ApplicableJob.BIS.Count(i) != p.ApplicableJob.Gear.Count(i));
    public static bool CanUse(this LootResult p)
    {
        //Direct gear or coffer drops are always usable
        return !p.DroppedItem.IsExchangableItem
               || p.NeededItems.Any(
                   item => ServiceManager.ItemInfo.GetShopEntriesForItem(item.ID).Any(shopEntry =>
                       {
                           for (int i = 0; i < SpecialShop.NUM_COST; i++)
                           {
                               SpecialShop.ItemCostEntry cost = shopEntry.entry.ItemCostEntries[i];
                               if(cost.Count == 0) continue;
                               if (cost.Item.Row == p.DroppedItem.ID) continue;
                               if (ItemInfo.IsCurrency(cost.Item.Row)) continue;
                               if (ItemInfo.IsTomeStone(cost.Item.Row)) continue;
                               if (p.ApplicableJob.Gear.Contains(new HrtItem(cost.Item.Row))) continue;
                               if(p.ApplicableJob.Parent!.MainInventory.ItemCount(cost.Item.Row) >= cost.Count) continue;
                               return false;
                           }
                           return true;
                       })
                   );
    }

    public static bool CanBuy(this LootResult p)
    {
        return ServiceManager.ItemInfo.GetShopEntriesForItem(p.DroppedItem.ID).Any(
            shopEntry =>
            {
                for (int i = 0; i < SpecialShop.NUM_COST; i++)
                {
                    SpecialShop.ItemCostEntry cost = shopEntry.entry.ItemCostEntries[i];
                    if (cost.Count == 0) continue;
                    if (ItemInfo.IsCurrency(cost.Item.Row)) continue;
                    if (ItemInfo.IsTomeStone(cost.Item.Row)) continue;
                    if (p.ApplicableJob.Gear.Contains(new HrtItem(cost.Item.Row))) continue;
                    if (p.ApplicableJob.Parent!.MainInventory.ItemCount(cost.Item.Row)
                        + (p.GuaranteedLoot.Any(loot => loot.ID == cost.Item.Row) ? 1 :0)
                        >= cost.Count) continue;
                    return false;
                }
                return true;
            }
        );
    }
}
