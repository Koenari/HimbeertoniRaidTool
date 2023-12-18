using Dalamud.Game.ClientState.Objects.SubKinds;
using HimbeertoniRaidTool.Common.Data;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.DataExtensions;

public static class EnumExtensions
{
    public static string PrefixName(this MateriaCategory cat)
    {
        HrtMateria mat = new(cat, MateriaLevel.I);
        return mat.Name.Length > 9 ? mat.Name[..^9] : "";
    }

    public static string FriendlyName(this StatType t) => t switch
    {
        StatType.Strength => Localize("Strength", "Strength"),
        StatType.Dexterity => Localize("Dexterity", "Dexterity"),
        StatType.Vitality => Localize("Vitality", "Vitality"),
        StatType.Intelligence => Localize("Intelligence", "Intelligence"),
        StatType.Mind => Localize("Mind", "Mind"),
        StatType.Piety => Localize("Piety", "Piety"),
        StatType.Hp => Localize("HP", "HP"),
        StatType.Mp => Localize("MP", "MP"),
        StatType.PhysicalDamage => Localize("Physical Damage", "Physical Damage"),
        StatType.MagicalDamage => Localize("Magical Damage", "Magical Damage"),
        StatType.Tenacity => Localize("Tenacity", "Tenacity"),
        StatType.AttackPower => Localize("AttackPower", "AttackPower"),
        StatType.Defense => Localize("Defense", "Defense"),
        StatType.DirectHitRate => Localize("Direct Hit", "Direct Hit"),
        StatType.MagicDefense => Localize("Magic Defense", "Magic Defense"),
        StatType.CriticalHitPower => Localize("Critical Hit Power", "Critical Hit Power"),
        StatType.CriticalHit => Localize("Critical Hit", "Critical Hit"),
        StatType.AttackMagicPotency => Localize("AttackMagicPotency", "Attack Magic Potency"),
        StatType.HealingMagicPotency => Localize("HealingMagicPotency", "Healing Magic Potency"),
        StatType.Determination => Localize("Determination", "Determination"),
        StatType.SkillSpeed => Localize("Skill Speed", "Skill Speed"),
        StatType.SpellSpeed => Localize("Spell Speed", "Spell Speed"),
        _ => t.ToString(),
    };

    public static string Abbrev(this StatType t) => t switch
    {
        StatType.CriticalHit => Localize("CRT", "CRT"),
        StatType.DirectHitRate => Localize("DH", "DH"),
        StatType.SkillSpeed => Localize("SKS", "SKS"),
        StatType.SpellSpeed => Localize("SPS", "SPS"),
        StatType.Determination => Localize("DET", "DET"),
        StatType.Piety => Localize("PIE", "PIE"),
        StatType.Mind => Localize("MND", "MND"),
        StatType.Strength => Localize("STR", "STR"),
        StatType.Dexterity => Localize("DEX", "DEX"),
        StatType.Intelligence => Localize("INT", "INT"),
        StatType.Vitality => Localize("VIT", "VIT"),
        StatType.Tenacity => Localize("TEN", "TEN"),
        _ => "XXX",
    };

    public static string FriendlyName(this GearSetManager manager) => manager switch
    {
        GearSetManager.Hrt => Localize("HimbeerToni Raid Tool", "HimbeerToni Raid Tool"),
        GearSetManager.Etro => Localize("etro.gg", "etro.gg"),
        _ => Localize("undefined", "undefined"),
    };

    public static string FriendlyName(this GroupType groupType) => groupType switch
    {
        GroupType.Solo => Localize("Solo", "Solo"),
        GroupType.Group => Localize("Group", "Group"),
        GroupType.Raid => Localize("FullGroup", "Full Group"),
        _ => Localize("undefined", "undefined"),
    };

    public static string FriendlyName(this GearSetSlot slot, bool detailed = false) => (slot, detailed) switch
    {
        (GearSetSlot.MainHand, _) => Localize("Weapon", "Weapon"),
        (GearSetSlot.OffHand, _) => Localize("Shield", "Shield"),
        (GearSetSlot.Head, _) => Localize("Head", "Head"),
        (GearSetSlot.Body, _) => Localize("Body", "Body"),
        (GearSetSlot.Hands, _) => Localize("Gloves", "Gloves"),
        (GearSetSlot.Waist, _) => Localize("NoBelts", "There no longer are belts you fuckwit"),
        (GearSetSlot.Legs, _) => Localize("Trousers", "Trousers"),
        (GearSetSlot.Feet, _) => Localize("Shoes", "Shoes"),
        (GearSetSlot.Ear, _) => Localize("Earrings", "Earrings"),
        (GearSetSlot.Neck, _) => Localize("Necklace", "Necklace"),
        (GearSetSlot.Wrist, _) => Localize("Bracelet", "Bracelet"),
        (GearSetSlot.Ring1, true) => Localize("Ring (R)", "Ring (R)"),
        (GearSetSlot.Ring2, true) => Localize("Ring (L)", "Ring (L)"),
        (GearSetSlot.Ring1 or GearSetSlot.Ring2, false) => Localize("Ring", "Ring"),
        (GearSetSlot.SoulCrystal, _) => Localize("Soul Crystal", "Soul Crystal"),
        _ => Localize("undefined", "undefined"),
    };

    public static string FriendlyName(this ItemSource source) => source switch
    {
        ItemSource.Raid => Localize("Raid", "Raid"),
        ItemSource.Dungeon => Localize("Dungeon", "Dungeon"),
        ItemSource.Trial => Localize("Trial", "Trial"),
        ItemSource.Tome => Localize("Tome", "Tome"),
        ItemSource.Crafted => Localize("Crafted", "Crafted"),
        ItemSource.AllianceRaid => Localize("Alliance", "Alliance"),
        ItemSource.Quest => Localize("Quest", "Quest"),
        ItemSource.Relic => Localize("Relic", "Relic"),
        ItemSource.Shop => Localize("Shop", "Shop"),
        ItemSource.Loot => Localize("ItemSource:Loot", "Looted"),
        _ => Localize("undefined", "undefined"),
    };

    public static string FriendlyName(this InstanceType source) => source switch
    {
        InstanceType.Raid => Localize("Raid", "Raid"),
        InstanceType.Trial => Localize("Trial", "Trial"),
        InstanceType.Dungeon => Localize("Dungeon", "Dungeon"),
        InstanceType.SoloInstance => Localize("Solo", "Solo"),
        _ => Localize("undefined", "undefined"),
    };

    public static string FriendlyName(this Role role) => role switch
    {
        Role.Tank => Localize("Tank", "Tank"),
        Role.Healer => Localize("Healer", "Healer"),
        Role.Melee => Localize("Melee", "Melee"),
        Role.Ranged => Localize("Ranged", "Ranged"),
        Role.Caster => Localize("Caster", "Caster"),
        _ => Localize("undefined", "undefined"),
    };

    public static Job GetJob(this PlayerCharacter target) => (Job)target.ClassJob.Id;
}