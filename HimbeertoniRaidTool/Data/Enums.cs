using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.GeneratedSheets;
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
        Quest,
        Relic
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
        Custom = 999,
    }
    public enum EncounterDifficulty
    {
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
        private static readonly Dictionary<StatType, string> StatTypeNameLookup;
        private static readonly Dictionary<StatType, string> StatTypeAbbrevLookup;
        private static readonly Dictionary<GearSetSlot, string> GearSetSlotNameLookup;
        private static readonly Dictionary<GearSource, string> GearSourceNameLookup;
        static EnumExtensions()
        {
            StatTypeNameLookup = new()
            {
                [StatType.PhysicalDamage] = Localize("Physical Damage", "Physical Damage"),
                [StatType.MagicalDamage] = Localize("Magical Damage", "Magical Damage"),
                [StatType.CriticalHit] = Localize("Critical Hit", "Critical Hit"),
                [StatType.DirectHitRate] = Localize("Direct Hit", "Direct Hit"),
                [StatType.SkillSpeed] = Localize("Skill Speed", "Skill Speed"),
                [StatType.SpellSpeed] = Localize("Spell Speed", "Spell Speed"),
                [StatType.MagicDefense] = Localize("Magic Defense", "Magic Defense")
            };

            StatTypeAbbrevLookup = new()
            {
                [StatType.CriticalHit] = Localize("CRT", "CRT"),
                [StatType.DirectHitRate] = Localize("DH", "DH"),
                [StatType.SkillSpeed] = Localize("SKS", "SKS"),
                [StatType.SpellSpeed] = Localize("SPS", "SPS"),
                [StatType.Determination] = Localize("DET", "DET"),
                [StatType.Piety] = Localize("PIE", "PIE"),
                [StatType.Mind] = Localize("MND", "MND"),
                [StatType.Strength] = Localize("STR", "STR"),
                [StatType.Dexterity] = Localize("DEX", "DEX"),
                [StatType.Intelligence] = Localize("INT", "INT"),
                [StatType.Vitality] = Localize("VIT", "VIT"),
                [StatType.Tenacity] = Localize("TEN", "TEN")
            };
            GearSetSlotNameLookup = new()
            {
                [GearSetSlot.MainHand] = Localize("Weapon", "Weapon"),
                [GearSetSlot.OffHand] = Localize("Shield", "Shield"),
                [GearSetSlot.Head] = Localize("Head", "Head"),
                [GearSetSlot.Body] = Localize("Body", "Body"),
                [GearSetSlot.Hands] = Localize("Gloves", "Gloves"),
                [GearSetSlot.Waist] = Localize("NoBelts", "There no longer are belts you fuckwit"),
                [GearSetSlot.Legs] = Localize("Trousers", "Trousers"),
                [GearSetSlot.Feet] = Localize("Shoes", "Shoes"),
                [GearSetSlot.Ear] = Localize("Earrings", "Earrings"),
                [GearSetSlot.Neck] = Localize("Necklace", "Necklace"),
                [GearSetSlot.Wrist] = Localize("Bracelet", "Bracelet"),
                [GearSetSlot.Ring1] = Localize("Ring", "Ring"),
                [GearSetSlot.Ring2] = Localize("Ring", "Ring"),
                [GearSetSlot.SoulCrystal] = Localize("Soul Crystal", "Soul Crystal"),
            };
            GearSourceNameLookup = new()
            {
                [GearSource.Raid] = Localize("Raid", "Raid"),
                [GearSource.Dungeon] = Localize("Dungeon", "Dungeon"),
                [GearSource.Trial] = Localize("Trial", "Trial"),
                [GearSource.Tome] = Localize("Tome", "Tome"),
                [GearSource.Crafted] = Localize("Crafted", "Crafted"),
                [GearSource.AllianceRaid] = Localize("Alliance", "Alliance"),
                [GearSource.Quest] = Localize("Quest", "Quest"),
                [GearSource.Relic] = Localize("Relic", "Relic")
            };

        }
        public static string FriendlyName(this StatType t) => StatTypeNameLookup.ContainsKey(t) ? StatTypeNameLookup[t] : t.ToString();
        public static string Abbrev(this StatType t) => StatTypeAbbrevLookup.ContainsKey(t) ? StatTypeAbbrevLookup[t] : "XXX";
        public static Role GetRole(this Job? c) => c.HasValue ? GetRole(c.Value) : Role.None;
        public static Role GetRole(this Job c) => c switch
        {
            Job.DRK or Job.GNB or Job.PLD or Job.WAR or Job.GLA or Job.MRD => Role.Tank,
            Job.AST or Job.SCH or Job.SGE or Job.WHM or Job.CNJ => Role.Healer,
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
        public static string FriendlyName(this GearSetSlot slot) =>
            GearSetSlotNameLookup.TryGetValue(slot, out string? fromDic) ? fromDic : Localize("undefined", "undefined");
        public static string FriendlyName(this GearSource source) =>
            GearSourceNameLookup.TryGetValue(source, out string? fromDic) ? fromDic : Localize("undefined", "undefined");

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
        public static bool TryGetJob(this PlayerCharacter target, out Job result) =>
            Enum.TryParse(target.ClassJob.GameData?.Abbreviation.RawString, true, out result);
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
