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
    public static class EnumExtensions
    {
        public static string FriendlyName(this GearSetSlot slot)
        {

            return slot switch
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
        }
        public static string FriendlyName(this GearSource source) => source switch
        {
            GearSource.Raid => Localize("Raid", "Raid"),
            GearSource.Dungeon => Localize("Dungeon", "Dungeon"),
            GearSource.Trial => Localize("Trial", "Trial"),
            GearSource.Tome => Localize("Tome", "Tome"),
            GearSource.Crafted => Localize("Crafted", "Crafted"),
            _ => Localize("undefined", "undefined")

        };

    }
}
