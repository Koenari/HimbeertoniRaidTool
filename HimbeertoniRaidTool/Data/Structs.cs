using static Dalamud.Localization;
namespace HimbeertoniRaidTool.Data
{
    public readonly struct GameExpansion
    {
        public static implicit operator GameExpansion(byte v) => new(v);
        public readonly byte Value;
        public string Name => Value switch
        {
            2 => Localize("EXP_2", "A Realm Reborn"),
            3 => Localize("EXP_3", "Heavensward"),
            4 => Localize("EXP_4", "Stormblood"),
            5 => Localize("EXP_5", "Shadowbringers"),
            6 => Localize("EXP_6", "Endwalker"),
            _ => Localize("Unknown", "Unknown")
        };

        public GameExpansion(byte v) => Value = v;
    }
    public readonly struct RaidTier
    {
        public readonly GameExpansion Expansion;
        public readonly ContentType Type;
        public readonly byte RaidNumber;
        public readonly EncounterDifficulty Difficulty;
        public readonly uint WeaponItemLevel;
        public readonly uint ArmorItemLevel;
        public readonly string Name;

        public RaidTier(GameExpansion expansion, byte raidNumber, EncounterDifficulty difficulty, uint weaponItemLevel, uint armorItemLevel, string name, ContentType type = ContentType.Raid)
        {
            Expansion = expansion;
            Type = type;
            RaidNumber = raidNumber;
            Difficulty = difficulty;
            WeaponItemLevel = weaponItemLevel;
            ArmorItemLevel = armorItemLevel;
            Name = name;
        }

        public uint ItemLevel(GearSetSlot slot) => slot == GearSetSlot.MainHand ? WeaponItemLevel : ArmorItemLevel;
    }
}
