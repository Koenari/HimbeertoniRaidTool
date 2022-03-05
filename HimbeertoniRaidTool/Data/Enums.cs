
using System;

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
                GearSetSlot.MainHand => "Weapon",
                GearSetSlot.OffHand => "Shield",
                GearSetSlot.Head => "Head",
                GearSetSlot.Body => "Body",
                GearSetSlot.Hands => "Gloves",
                GearSetSlot.Waist => "THere no longer are belts you fuckwit",
                GearSetSlot.Legs => "Trousers",
                GearSetSlot.Feet => "Shoes",
                GearSetSlot.Ear => "Earrings",
                GearSetSlot.Neck => "Necklace",
                GearSetSlot.Wrist => "Bracelet",
                GearSetSlot.Ring1 => "Ring",
                GearSetSlot.Ring2 => "Ring",
                GearSetSlot.SoulCrystal => "Soul Crystal",
                _ => "undefined"

            };
        }

    }
}
