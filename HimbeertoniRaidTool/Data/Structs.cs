namespace HimbeertoniRaidTool.Data
{
    public readonly struct GameExpansion
    {
        public static implicit operator GameExpansion(byte v) => new(v);
        public readonly byte Value;
        public string Name { 
            get
            {
                return Value switch
                {
                    2 => "A Realm Reborn",
                    3 => "Heavensward",
                    4 => "Stormblood",
                    5 => "Shadowbringers",
                    6 => "Endwalker",
                    _ => "Unknown"
                };
            } }
        public GameExpansion(byte v) => Value = v;
    }
    public readonly struct RaidTier
    {
        public readonly GameExpansion Expansion;
        public readonly byte RaidNumber;
        public readonly EncounterDifficulty Difficulty;
        public readonly uint WeaponItemLevel;
        public readonly uint ArmorItemLevel;
        public readonly string Name;

        public RaidTier(GameExpansion expansion, byte raidNumber, EncounterDifficulty difficulty, uint weaponItemLevel, uint armorItemLevel, string name)
        {
            Expansion = expansion;
            RaidNumber = raidNumber;
            Difficulty = difficulty;
            WeaponItemLevel = weaponItemLevel;
            ArmorItemLevel = armorItemLevel;
            Name = name;
        }

        public uint ItemLevel(GearSetSlot slot) => slot == GearSetSlot.MainHand ? WeaponItemLevel : ArmorItemLevel;
    }
}
