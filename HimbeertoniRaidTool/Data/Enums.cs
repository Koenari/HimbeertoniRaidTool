using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.GeneratedSheets;
using static HimbeertoniRaidTool.HrtServices.Localization;
namespace HimbeertoniRaidTool.Data
{
    public enum ItemSource
    {

        undefined = 0,

        Raid = 1,
        Dungeon = 2,
        Trial = 3,
        AllianceRaid = 101,

        Tome = 10,
        Crafted = 20,
        Quest = 30,
        Relic = 40,
        Shop = 50,
        Loot = 60,
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
        None = 0,
        BISOverUpgrade = 1,
        LowestItemLevel = 2,
        HighesItemLevelGain = 3,
        RolePrio = 4,
        Random = 5,
        DPS = 6,
        NeedGreed = 997,
        Greed = 998,
        Custom = 999,
    }
    public enum EncounterDifficulty
    {
        None = 0,
        Normal,
        Hard,
        Extreme,
        Savage,
        Ultimate
    }
    public enum Job : byte
    {
        ADV = 0,
        AST = 33,
        BLM = 25,
        BLU = 36,
        BRD = 23,
        DNC = 38,
        DRG = 22,
        DRK = 32,
        GNB = 37,
        MCH = 31,
        MNK = 20,
        NIN = 30,
        PLD = 19,
        RDM = 35,
        RPR = 39,
        SAM = 34,
        SCH = 28,
        SGE = 40,
        SMN = 27,
        WAR = 21,
        WHM = 24,
        GLA = 1,
        MRD = 3,
        LNC = 4,
        PGL = 2,
        ARC = 5,
        THM = 7,
        ACN = 26,
        CNJ = 6,
        ROG = 29,
    }

    public enum Role : byte
    {
        None = 0,
        Tank = 1,
        Healer = 4,
        Melee = 2,
        Ranged = 3,
        Caster = 5,
    }
    public enum InstanceType
    {
        Unknown = 0,
        Raid = 1,
        Trial = 4,
        Dungeon = 2,
        SoloInstance = 7,

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
    //Please don't make this political this is just to "correctly" reflect game logic and made an enum in hope there will be more in the future
    public enum Gender
    {
        Unknown = 0,
        Female = 1,
        Male = 2,
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
        public static Role GetRole(this PlayableClass? c) => (c?.Job).GetRole();
        public static Role GetRole(this Job? c) => c.HasValue ? GetRole(c.Value) : Role.None;
        public static Role GetRole(this Job c) => c switch
        {
            Job.DRK or Job.GNB or Job.PLD or Job.WAR or Job.GLA or Job.MRD => Role.Tank,
            Job.AST or Job.SCH or Job.SGE or Job.WHM or Job.CNJ => Role.Healer,
            Job.DRG or Job.MNK or Job.NIN or Job.RPR or Job.SAM or Job.LNC or Job.PGL or Job.ROG => Role.Melee,
            Job.BLM or Job.BLU or Job.RDM or Job.SMN or Job.THM or Job.ACN => Role.Caster,
            Job.BRD or Job.DNC or Job.MCH or Job.ARC => Role.Ranged,
            _ => Role.None,
        };
        public static ClassJob? GetClassJob(this Job? c) =>
            c.HasValue ? Services.DataManager.GetExcelSheet<ClassJob>()?.GetRow((uint)c.Value) : null;
        public static StatType MainStat(this Job job) => job switch
        {
            Job.NIN => StatType.Dexterity,
            _ => job.GetRole().MainStat(),
        };
        public static StatType MainStat(this Role role) => role switch
        {
            Role.Tank or Role.Melee => StatType.Strength,
            Role.Healer => StatType.Mind,
            Role.Caster => StatType.Intelligence,
            Role.Ranged => StatType.Dexterity,
            _ => StatType.None,
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
        public static bool IsCombatJob(this Job j) => j.MainStat() != StatType.None;
        public static StatType GetStatType(this MateriaCategory materiaCategory) => materiaCategory switch
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
        public static ItemSource ToItemSource(this InstanceType contentType) => contentType switch
        {
            InstanceType.Raid => ItemSource.Raid,
            InstanceType.Trial => ItemSource.Trial,
            InstanceType.Dungeon => ItemSource.Dungeon,
            _ => ItemSource.undefined,
        };
    }
}
