using System;
using System.Linq;
using System.Reflection;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Data
{
    public enum GearSource
    {
        Raid,
        Dungeon,
        Trial,
        Tome,
        Crafted,
        undefined,
        AllianceRaid,
    }
    public enum GearSetSlot : short
    {
        MainHand = 0,
        OffHand = 1,
        Head = 2,
        Body = 3,
        Hands = 4,
        Waist = 5,
        Legs = 6,
        Feet = 7,
        Ear = 8,
        Neck = 9,
        Wrist = 10,
        Ring1 = 11,
        Ring2 = 12,
        SoulCrystal = 13,
        None = 999
    }
    public enum LootRuleEnum
    {
        BISOverUpgrade = 1,
        LowestItemLevel = 2,
        HighesItemLevelGain = 3,
        ByPosition = 4,
        Random = 5
    }
    public enum EncounterDifficulty
    {
        Normal,
        Hard,
        Extreme,
        Savage,
        Ultimate
    }
    public enum AvailableClasses
    {
        [Role(Role.Healer)]
        AST,
        [Role(Role.Caster)]
        BLM,
        [Role(Role.Caster)]
        BLU,
        [Role(Role.Ranged)]
        BRD,
        [Role(Role.Ranged)]
        DNC,
        [Role(Role.Melee)]
        DRG,
        [Role(Role.Tank)]
        DRK,
        [Role(Role.Tank)]
        GNB,
        [Role(Role.Ranged)]
        MCH,
        [Role(Role.Melee)]
        MNK,
        [Stat(StatType.Dexterity)]
        [Role(Role.Melee)]
        NIN,
        [Role(Role.Tank)]
        PLD,
        [Role(Role.Caster)]
        RDM,
        [Role(Role.Melee)]
        RPR,
        [Role(Role.Melee)]
        SAM,
        [Role(Role.Healer)]
        SCH,
        [Role(Role.Healer)]
        SGE,
        [Role(Role.Caster)]
        SMN,
        [Role(Role.Tank)]
        WAR,
        [Role(Role.Healer)]
        WHM
    }
    public enum Role
    {
        [Stat(StatType.None)]
        None,
        [Stat(StatType.Strength)]
        Tank,
        [Stat(StatType.Mind)]
        Healer,
        [Stat(StatType.Strength)]
        Melee,
        [Stat(StatType.Dexterity)]
        Ranged,
        [Stat(StatType.Intelligence)]
        Caster
    }
    public enum PositionInRaidGroup : byte
    {
        Tank1 = 0,
        Tank2 = 1,
        Heal1 = 2,
        Heal2 = 3,
        Melee1 = 4,
        Melee2 = 5,
        Ranged = 6,
        Caster = 7
    }
    public enum ContentType
    {
        Raid,
        AllianceRaid,
        Trial,
        Dungeon
    }
    public enum GroupType
    {
        Solo,
        Group,
        Raid
    }
    public enum GearSetManager
    {
        HRT,
        Etro
    }
    public enum MateriaCategory : ushort
    {
        [Stat(StatType.None)]
        None = 0,
        [Stat(StatType.Piety)]
        Piety = 7,
        [Stat(StatType.DirectHitRate)]
        DirectHit = 14,
        [Stat(StatType.CriticalHit)]
        CriticalHit = 15,
        [Stat(StatType.Determination)]
        Determination = 16,
        [Stat(StatType.Tenacity)]
        Tenacity = 17,
        [Stat(StatType.Gathering)]
        Gathering = 18,
        [Stat(StatType.Perception)]
        Perception = 19,
        [Stat(StatType.GP)]
        GP = 20,
        [Stat(StatType.Craftsmanship)]
        Craftsmanship = 21,
        [Stat(StatType.CP)]
        CP = 22,
        [Stat(StatType.Control)]
        Control = 23,
        [Stat(StatType.SkillSpeed)]
        SkillSpeed = 24,
        [Stat(StatType.SpellSpeed)]
        SpellSpeed = 25,
    }
    public enum StatType : uint
    {
        None,
        Strength,
        Dexterity,
        Vitality,
        Intelligence,
        Mind,
        Piety,
        HP,
        MP,
        TP,
        GP,
        CP,
        PhysicalDamage,
        MagicalDamage,
        Delay,
        AdditionalEffect,
        AttackSpeed,
        BlockRate,
        BlockStrength,
        Tenacity,
        AttackPower,
        Defense,
        DirectHitRate,
        Evasion,
        MagicDefense,
        CriticalHitPower,
        CriticalHitResilience,
        CriticalHit,
        CriticalHitEvasion,
        SlashingResistance,
        PiercingResistance,
        BluntResistance,
        ProjectileResistance,
        AttackMagicPotency,
        HealingMagicPotency,
        EnhancementMagicPotency,
        EnfeeblingMagicPotency,
        FireResistance,
        IceResistance,
        WindResistance,
        EarthResistance,
        LightningResistance,
        WaterResistance,
        MagicResistance,
        Determination,
        SkillSpeed,
        SpellSpeed,
        Haste,
        Morale,
        Enmity,
        EnmityReduction,
        CarefulDesynthesis,
        EXPBonus,
        Regen,
        Refresh,
        MovementSpeed,
        Spikes,
        SlowResistance,
        PetrificationResistance,
        ParalysisResistance,
        SilenceResistance,
        BlindResistance,
        PoisonResistance,
        StunResistance,
        SleepResistance,
        BindResistance,
        HeavyResistance,
        DoomResistance,
        ReducedDurabilityLoss,
        IncreasedSpiritbondGain,
        Craftsmanship,
        Control,
        Gathering,
        Perception,
        Unknown73,
        Count,
    }
    public static class EnumExtensions
    {
        public static StatType GetStatType(this MateriaCategory c) =>
            c.GetAttribute<StatAttribute>()?.StatType ?? StatType.None;
        public static string FriendlyName(this StatType t) => t switch
        {
            StatType.PhysicalDamage => "Physical Damage",
            StatType.MagicalDamage => "Magical Damage",
            StatType.CriticalHit => "Critical Hit",
            StatType.DirectHitRate => "Direct Hit",
            StatType.SkillSpeed => "Skill Speed",
            StatType.SpellSpeed => "Spell Speed",
            StatType.MagicDefense => "Magic Defense",
            _ => t.ToString(),
        };
        public static string Abbrev(this StatType t) => t switch
        {
            StatType.CriticalHit => "CRT",
            StatType.DirectHitRate => "DH",
            StatType.SkillSpeed => "SKS",
            StatType.SpellSpeed => "SPS",
            StatType.Determination => "DET",
            StatType.Piety => "PIE",
            StatType.Mind => "MND",
            StatType.Strength => "STR",
            StatType.Dexterity => "DEX",
            StatType.Intelligence => "INT",
            StatType.Vitality => "VIT",
            _ => "XXX",
        };
        public static Role GetRole(this AvailableClasses c) =>
            c.GetAttribute<RoleAttribute>()?.Role ?? Role.None;

        public static T? GetAttribute<T>(this Enum field) where T : Attribute
        {
            return
                field.GetType().GetMember(field.ToString())
                .Where(member => member.MemberType == MemberTypes.Field)
                .FirstOrDefault()?
                .GetCustomAttributes<T>(false)
                .SingleOrDefault();
        }
        public static StatType MainStat(this AvailableClasses c) =>
            c.GetAttribute<StatAttribute>()?.StatType
            ?? c.GetAttribute<RoleAttribute>()?.Role.GetAttribute<StatAttribute>()?.StatType
            ?? StatType.None;

        public static string FriendlyName(this GearSetManager manager) => manager switch
        {
            GearSetManager.HRT => Localize("HimbeerToni Raid Tool", "HimbeerToni Raid Tool"),
            GearSetManager.Etro => Localize("etro.gg", "etro.gg"),
            _ => Localize("undefined", "undefined"),
        };
        public static int GroupSize(this GroupType groupType) => groupType switch
        {
            GroupType.Solo => 1,
            GroupType.Group => 4,
            GroupType.Raid => 8,
            _ => 0
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
        public static string FriendlyName(this GearSource source) => source switch
        {
            GearSource.Raid => Localize("Raid", "Raid"),
            GearSource.Dungeon => Localize("Dungeon", "Dungeon"),
            GearSource.Trial => Localize("Trial", "Trial"),
            GearSource.Tome => Localize("Tome", "Tome"),
            GearSource.Crafted => Localize("Crafted", "Crafted"),
            GearSource.AllianceRaid => Localize("Alliance", "Alliance"),
            _ => Localize("undefined", "undefined")
        };

        public static bool IsPartOf(this PositionInRaidGroup pos, GroupType type) => pos switch
        {
            PositionInRaidGroup.Tank1 => true,
            PositionInRaidGroup.Tank2 => type == GroupType.Raid,
            PositionInRaidGroup.Heal1 => type == GroupType.Group || type == GroupType.Raid,
            PositionInRaidGroup.Heal2 => type == GroupType.Raid,
            PositionInRaidGroup.Melee1 => type == GroupType.Group || type == GroupType.Raid,
            PositionInRaidGroup.Melee2 => type == GroupType.Raid,
            PositionInRaidGroup.Ranged => type == GroupType.Group || type == GroupType.Raid,
            PositionInRaidGroup.Caster => type == GroupType.Raid,
            _ => false,
        };


    }
    [AttributeUsage(AttributeTargets.Field)]
    class StatAttribute : Attribute
    {
        public StatType StatType;
        public StatAttribute(StatType t) => StatType = t;
    }
    [AttributeUsage(AttributeTargets.Field)]
    class RoleAttribute : Attribute
    {
        public Role Role;
        public RoleAttribute(Role r) => Role = r;
    }
}
