using Dalamud.Game.ClientState.Objects.SubKinds;
using HimbeertoniRaidTool.Common.Data;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.DataExtensions
{
    public static class EnumExtensions
    {
        public static string FriendlyName(this StatType t) => t switch
        {
            StatType.Strength => Localize("Strength", "Strength"),
            StatType.Dexterity => Localize("Dexterity", "Dexterity"),
            StatType.Vitality => Localize("Vitality", "Vitality"),
            StatType.Intelligence => Localize("Intelligence", "Intelligence"),
            StatType.Mind => Localize("Mind", "Mind"),
            StatType.Piety => Localize("Piety", "Piety"),
            StatType.HP => Localize("HP", "HP"),
            StatType.MP => Localize("MP", "MP"),
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
            GearSetManager.HRT => Localize("HimbeerToni Raid Tool", "HimbeerToni Raid Tool"),
            GearSetManager.Etro => Localize("etro.gg", "etro.gg"),
            _ => Localize("undefined", "undefined"),
        };

        public static string FriendlyName(this GroupType groupType) => groupType switch
        {
            GroupType.Solo => Localize("Solo", "Solo"),
            GroupType.Group => Localize("Group", "Group"),
            GroupType.Raid => Localize("FullGroup", "Full Group"),
            _ => Localize("undefined", "undefined")

        };
        public static string FriendlyName(this GearSetSlot slot) => slot switch
        {
            GearSetSlot.MainHand => Localize("Weapon", "Weapon"),
            GearSetSlot.OffHand => Localize("Shield", "Shield"),
            GearSetSlot.Head => Localize("Head", "Head"),
            GearSetSlot.Body => Localize("Body", "Body"),
            GearSetSlot.Hands => Localize("Gloves", "Gloves"),
            GearSetSlot.Waist => Localize("NoBelts", "There no longer are belts you fuckwit"),
            GearSetSlot.Legs => Localize("Trousers", "Trousers"),
            GearSetSlot.Feet => Localize("Shoes", "Shoes"),
            GearSetSlot.Ear => Localize("Earrings", "Earrings"),
            GearSetSlot.Neck => Localize("Necklace", "Necklace"),
            GearSetSlot.Wrist => Localize("Bracelet", "Bracelet"),
            GearSetSlot.Ring1 => Localize("Ring", "Ring"),
            GearSetSlot.Ring2 => Localize("Ring", "Ring"),
            GearSetSlot.SoulCrystal => Localize("Soul Crystal", "Soul Crystal"),
            _ => Localize("undefined", "undefined")

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
}
