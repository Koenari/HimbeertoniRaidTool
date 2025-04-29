using System.Globalization;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using ICloneable = HimbeertoniRaidTool.Common.Data.ICloneable;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class LootRuling : ICloneable
{
    public static readonly LootRule Default = new(LootRuleEnum.None);
    public static readonly LootRule NeedOverGreed = new(LootRuleEnum.NeedGreed);
    [JsonProperty("RuleSet")]
    public List<LootRule> RuleSet = [];
    public static IEnumerable<LootRule> PossibleRules
    {
        get
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (LootRuleEnum rule in Enum.GetValues(typeof(LootRuleEnum)))
            {
#pragma warning disable CS0618
                if (rule is LootRuleEnum.None or LootRuleEnum.Dps)
#pragma warning restore CS0618
                    continue;
                //Special Rules only used internally
                if ((int)rule > 900)
                    continue;
                yield return new LootRule(rule);
            }
        }
    }

    [JsonIgnore]
    public IEnumerable<LootRule> ActiveRules => RuleSet.Where(r => r.Active);
}

[JsonObject(MemberSerialization.OptIn)]
[method: JsonConstructor]
public class LootRule(LootRuleEnum rule) : IEquatable<LootRule>, IDrawable, IHrtDataType
{
    [JsonProperty("Rule")] public readonly LootRuleEnum Rule = rule;

    [JsonProperty("Active")] public bool Active = true;

    [JsonProperty("IgnoreActive")] public bool IgnorePlayers;
    public static string DataTypeNameStatic => LootmasterLoc.LootRule_DataTypeName;

    public bool CanIgnore =>
        Rule switch
        {
            LootRuleEnum.BisOverUpgrade => true,
            LootRuleEnum.CanUse         => true,
            LootRuleEnum.CanBuy         => true,
            LootRuleEnum.NeedGreed      => true,
            _                           => false,
        };

    private string IgnoreTooltip =>
        Rule switch
        {
            LootRuleEnum.BisOverUpgrade => LootmasterLoc.LootRule_draw_cb_tt_ignore_bis,
            LootRuleEnum.CanUse         => LootmasterLoc.LootRule_draw_cb_tt_ignore_canUse,
            LootRuleEnum.CanBuy         => LootmasterLoc.LootRule_draw_cb_tt_ignore_canBuy,
            LootRuleEnum.NeedGreed      => LootmasterLoc.LootRule_draw_cb_tt_ignore_needGreed,
            _                           => "",
        };

    public void Draw()
    {
        ImGui.Checkbox("##active", ref Active);
        ImGuiHelper.AddTooltip(LootmasterLoc.LootRule_Draw_cb_tt_active);
        ImGui.SameLine();
        using var disabled = ImRaii.Disabled(!Active);
        ImGui.Text(Name);
        if (CanIgnore)
        {
            ImGui.SameLine();
            ImGui.Checkbox($"{LootmasterLoc.LootRule_draw_cb_ignore}##ignore", ref IgnorePlayers);
            ImGuiHelper.AddTooltip(IgnoreTooltip);
        }
    }
    public bool Equals(LootRule? obj) => obj?.Rule == Rule;
    string IHrtDataType.DataTypeName => DataTypeNameStatic;

    public string Name => GetName();

    /// <summary>
    ///     Evaluates this LootRule for given player
    /// </summary>
    /// <param name="x">The player to evaluate for</param>
    /// <returns>
    ///     A tuple of int (can be used for Compare like (right - left)) and a string describing the
    ///     value
    /// </returns>
    public (float, string) Eval(LootResult x)
    {
        (float val, string? reason) = InternalEval(x);
        return (val, reason ?? val.ToString(CultureInfo.CurrentCulture));
    }

    public bool ShouldIgnore(LootResult x) => CanIgnore && IgnorePlayers && Rule switch
    {
        LootRuleEnum.BisOverUpgrade => !x.IsBiS(),
        LootRuleEnum.CanUse         => !x.CanUse(),
        LootRuleEnum.CanBuy         => x.CanBuy(),
        LootRuleEnum.Greed          => true,
        _                           => false,
    };

    private (float, string?) InternalEval(LootResult x) => Rule switch
    {
        LootRuleEnum.Random               => (x.Roll, null),
        LootRuleEnum.LowestItemLevel      => (-x.ItemLevel, x.ItemLevel.ToString()),
        LootRuleEnum.HighestItemLevelGain => (x.ItemLevelGain(), null),
        LootRuleEnum.BisOverUpgrade => x.IsBiS() ? (1, GeneralLoc.CommonTerms_Yes_Abbrev)
            : (-1, GeneralLoc.CommonTerms_No_Abbrev),
        LootRuleEnum.RolePrio => (-x.RolePriority, $"{x.ApplicableJob?.Role}"),
        LootRuleEnum.DpsGain  => (x.DpsGain(), $"{x.DpsGain() * 100:f1} %%"),
        LootRuleEnum.CanUse => x.CanUse() ? (1, GeneralLoc.CommonTerms_Yes_Abbrev)
            : (-1, GeneralLoc.CommonTerms_No_Abbrev),
        LootRuleEnum.CanBuy => x.CanBuy() ? (-1, GeneralLoc.CommonTerms_Yes_Abbrev)
            : (1, GeneralLoc.CommonTerms_No_Abbrev),
        _ => (0, GeneralLoc.CommonTerms_None),
    };
    public override string ToString() => Name;
    private string GetName() => Rule switch
    {
        LootRuleEnum.BisOverUpgrade       => LootmasterLoc.LootRule_BISOverUpgrade,
        LootRuleEnum.LowestItemLevel      => LootmasterLoc.LootRule_LowestItemLevel,
        LootRuleEnum.HighestItemLevelGain => LootmasterLoc.LootRule_HighestItemLevelGain,
        LootRuleEnum.RolePrio             => LootmasterLoc.LootRule_ByRole,
        LootRuleEnum.Random               => LootmasterLoc.LootRule_Rolling,
        LootRuleEnum.DpsGain              => LootmasterLoc.LootRule_DPSGain,
        LootRuleEnum.CanUse               => LootmasterLoc.LootRule_CanUse,
        LootRuleEnum.CanBuy               => LootmasterLoc.LootRule_CanBuy,
        LootRuleEnum.None                 => GeneralLoc.CommonTerms_None,
        LootRuleEnum.Greed                => LootmasterLoc.LootRule_Greed,
        LootRuleEnum.NeedGreed            => LootmasterLoc.LootRule_Need,
        _                                 => GeneralLoc.CommonTerms_undefined,
    };

    public override int GetHashCode() => Rule.GetHashCode();
    public override bool Equals(object? obj) => Equals(obj as LootRule);
    public static bool operator ==(LootRule l, LootRule r)
    {
        return l.Equals(r);
    }
    public static bool operator !=(LootRule l, LootRule r)
    {
        return !l.Equals(r);
    }
}