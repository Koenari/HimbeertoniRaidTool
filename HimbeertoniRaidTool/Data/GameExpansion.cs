using System.Collections.Generic;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.Data
{
    public class GameExpansion
    {
        [JsonProperty]
        public readonly byte Number;
        [JsonProperty]
        public readonly byte MaxMateriaLevel;
        [JsonProperty]
        public readonly int MaxLevel;
        public readonly RaidTier[] SavageRaidTiers;
        public readonly RaidTier[] NormalRaidTiers;
        public RaidTier CurrentSavage => SavageRaidTiers[^1];
        [JsonIgnore]
        public string Name => Number switch
        {
            2 => Localize("EXP_2", "A Realm Reborn"),
            3 => Localize("EXP_3", "Heavensward"),
            4 => Localize("EXP_4", "Stormblood"),
            5 => Localize("EXP_5", "Shadowbringers"),
            6 => Localize("EXP_6", "Endwalker"),
            _ => Localize("Unknown", "Unknown")
        };

        public GameExpansion(byte v, int maxMatLevel, int maxLvl, int unlockedRaidTiers)
        {
            Number = v;
            MaxMateriaLevel = (byte)(maxMatLevel - 1);
            MaxLevel = maxLvl;
            NormalRaidTiers = new RaidTier[unlockedRaidTiers];
            SavageRaidTiers = new RaidTier[unlockedRaidTiers];
        }
    }
    public class RaidTier
    {
        public readonly EncounterDifficulty Difficulty;
        public readonly uint WeaponItemLevel;
        public readonly uint ArmorItemLevel;
        public readonly string Name;
        private readonly List<uint> BossIDs;
        public List<InstanceWithLoot> Bosses => BossIDs.ConvertAll(id => Services.GameInfo.GetInstance(id));

        public RaidTier(EncounterDifficulty difficulty, uint weaponItemLevel, uint armorItemLevel, string name, IEnumerable<uint> bossIDS)
        {
            Difficulty = difficulty;
            WeaponItemLevel = weaponItemLevel;
            ArmorItemLevel = armorItemLevel;
            Name = name;
            BossIDs = new(bossIDS);
        }

        public uint ItemLevel(GearSetSlot slot) => slot == GearSetSlot.MainHand ? WeaponItemLevel : ArmorItemLevel;
    }
}
