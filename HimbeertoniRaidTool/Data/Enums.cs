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
    [Obsolete]
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
        WHM,
        [Role(Role.Tank)]
        GLA,
        [Role(Role.Tank)]
        MRD,
        [Role(Role.Melee)]
        LNC,
        [Role(Role.Melee)]
        PGL,
        [Role(Role.Ranged)]
        ARC,
        [Role(Role.Caster)]
        THM,
        [Role(Role.Caster)]
        ACN,
        [Role(Role.Healer)]
        CNJ
    }

    public enum Job
    {
        ADV = 0,
        [Role(Role.Healer)]
        AST = 33,
        [Role(Role.Caster)]
        BLM = 25,
        [Role(Role.Caster)]
        BLU = 36,
        [Role(Role.Ranged)]
        BRD = 23,
        [Role(Role.Ranged)]
        DNC = 38,
        [Role(Role.Melee)]
        DRG = 22,
        [Role(Role.Tank)]
        DRK = 32,
        [Role(Role.Tank)]
        GNB = 37,
        [Role(Role.Ranged)]
        MCH = 31,
        [Role(Role.Melee)]
        MNK = 20,
        [Stat(StatType.Dexterity)]
        [Role(Role.Melee)]
        NIN = 30,
        [Role(Role.Tank)]
        PLD = 19,
        [Role(Role.Caster)]
        RDM = 35,
        [Role(Role.Melee)]
        RPR = 39,
        [Role(Role.Melee)]
        SAM = 34,
        [Role(Role.Healer)]
        SCH = 28,
        [Role(Role.Healer)]
        SGE = 40,
        [Role(Role.Caster)]
        SMN = 27,
        [Role(Role.Tank)]
        WAR = 21,
        [Role(Role.Healer)]
        WHM = 24,
        [Role(Role.Tank)]
        GLA = 1,
        [Role(Role.Tank)]
        MRD = 3,
        [Role(Role.Melee)]
        LNC = 4,
        [Role(Role.Melee)]
        PGL = 2,
        [Role(Role.Ranged)]
        ARC = 5,
        [Role(Role.Caster)]
        THM = 7,
        [Role(Role.Caster)]
        ACN = 26,
        [Role(Role.Healer)]
        CNJ = 6,
        [Role(Role.Melee)]
        ROG = 29,
    }

    public enum Role : byte
    {
        [Stat(StatType.None)]
        None = 0,
        [Stat(StatType.Strength)]
        Tank = 1,
        [Stat(StatType.Mind)]
        Healer = 4,
        [Stat(StatType.Strength)]
        Melee = 2,
        [Stat(StatType.Dexterity)]
        Ranged = 3,
        [Stat(StatType.Intelligence)]
        Caster = 5,
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
            StatType.PhysicalDamage => Localize("Physical Damage", "Physical Damage"),
            StatType.MagicalDamage => Localize("Magical Damage", "Magical Damage"),
            StatType.CriticalHit => Localize("Critical Hit", "Critical Hit"),
            StatType.DirectHitRate => Localize("Direct Hit", "Direct Hit"),
            StatType.SkillSpeed => Localize("Skill Speed", "Skill Speed"),
            StatType.SpellSpeed => Localize("Spell Speed", "Spell Speed"),
            StatType.MagicDefense => Localize("Magic Defense", "Magic Defense"),
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
        public static Role GetRole(this Job c) =>
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
        public static StatType MainStat(this Job c) =>
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
