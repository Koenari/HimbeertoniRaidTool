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
        AST,
        BLM,
        BLU,
        BRD,
        DNC,
        DRG,
        DRK,
        GNB,
        MCH,
        MNK,
        NIN,
        PLD,
        RDM,
        RPR,
        SAM,
        SCH,
        SGE,
        SMN,
        WAR,
        WHM
    }
    public enum Role
    {
        None,
        Tank,
        Healer,
        Melee,
        Ranged,
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
        None = 0,
        Piety = 7,
        DirectHit = 14,
        CriticalHit = 15,
        Determination = 16,
        Tenacity = 17,
        Gathering = 18,
        Perception = 19,
        GP = 20,
        Craftsmanship = 21,
        CP = 22,
        Control = 23,
        SkillSpeed = 24,
        SpellSpeed = 25,
    }
    public enum StatType
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
        Count
    }
    public static class EnumExtensions
    {
        public static StatType GetStatType(this MateriaCategory c) => c switch
        {
            MateriaCategory.Piety => StatType.Piety,
            MateriaCategory.DirectHit => StatType.DirectHitRate,
            MateriaCategory.CriticalHit => StatType.CriticalHit,
            MateriaCategory.Determination => StatType.Determination,
            MateriaCategory.Tenacity => StatType.Tenacity,
            MateriaCategory.Gathering => StatType.Gathering,
            MateriaCategory.Perception => StatType.Perception,
            MateriaCategory.GP => StatType.GP,
            MateriaCategory.Craftsmanship => StatType.Craftsmanship,
            MateriaCategory.CP => StatType.CP,
            MateriaCategory.Control => StatType.Control,
            MateriaCategory.SkillSpeed => StatType.SkillSpeed,
            MateriaCategory.SpellSpeed => StatType.SpellSpeed,
            _ => StatType.None,
        };
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
        public static Role GetRole(this AvailableClasses c) => c switch
        {
            AvailableClasses.AST => Role.Healer,
            AvailableClasses.BLM => Role.Caster,
            AvailableClasses.BLU => Role.Caster,
            AvailableClasses.BRD => Role.Ranged,
            AvailableClasses.DNC => Role.Ranged,
            AvailableClasses.DRG => Role.Melee,
            AvailableClasses.DRK => Role.Tank,
            AvailableClasses.GNB => Role.Tank,
            AvailableClasses.MCH => Role.Ranged,
            AvailableClasses.MNK => Role.Melee,
            AvailableClasses.NIN => Role.Melee,
            AvailableClasses.PLD => Role.Tank,
            AvailableClasses.RDM => Role.Caster,
            AvailableClasses.RPR => Role.Melee,
            AvailableClasses.SAM => Role.Melee,
            AvailableClasses.SCH => Role.Healer,
            AvailableClasses.SGE => Role.Healer,
            AvailableClasses.SMN => Role.Caster,
            AvailableClasses.WAR => Role.Tank,
            AvailableClasses.WHM => Role.Healer,
            _ => Role.None,
        };
        public static StatType MainStat(this AvailableClasses c) => c switch
        {
            AvailableClasses.AST => StatType.Mind,
            AvailableClasses.BLM => StatType.Intelligence,
            AvailableClasses.BLU => StatType.Intelligence,
            AvailableClasses.BRD => StatType.Dexterity,
            AvailableClasses.DNC => StatType.Dexterity,
            AvailableClasses.DRG => StatType.Strength,
            AvailableClasses.DRK => StatType.Strength,
            AvailableClasses.GNB => StatType.Strength,
            AvailableClasses.MCH => StatType.Dexterity,
            AvailableClasses.MNK => StatType.Strength,
            AvailableClasses.NIN => StatType.Dexterity,
            AvailableClasses.PLD => StatType.Strength,
            AvailableClasses.RDM => StatType.Intelligence,
            AvailableClasses.RPR => StatType.Strength,
            AvailableClasses.SAM => StatType.Strength,
            AvailableClasses.SCH => StatType.Mind,
            AvailableClasses.SGE => StatType.Mind,
            AvailableClasses.SMN => StatType.Intelligence,
            AvailableClasses.WAR => StatType.Strength,
            AvailableClasses.WHM => StatType.Mind,
            _ => throw new System.NotImplementedException(),
        };
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
            _ => -1
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

    }
}
