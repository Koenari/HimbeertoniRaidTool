using System.Collections.Generic;
using HimbeertoniRaidTool.Data;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.HrtServices
{
    /// <summary>
    /// This class/file encapsulates all data that needs to be update for new Patches of the game.
    /// It is intended to be servicable by someone not familiar with programming.
    /// First there are entries for each RaidTier. To make a new one make it in the following way:
    /// 'public static RaidTier' DescriptiveName(no spaces) ' => 
    ///     new('Expansion number, Raid number (1-3), EncounterDifficulty, ILvl of Weapons, iLvl of other Gear, "A descriptive name", the current max materia tier);
    /// Then there is a list which links loot  to boss encounter
    ///     Just add entries as a new line of format:
    ///     { ItemID(S), (RaidTier (Named like above), Encounter Number(1-4)) },
    ///AFter that information on which Slot Item Coffers belong to
    ///     Just add entries as a new line of format:
    ///     { ItemID, GearSlot.Name },
    /// </summary>
    internal class CuratedData
    {
        internal CuratedData()
        {
            CurrentExpansion = new(6, 10, 90, 2);
            CurrentExpansion.SavageRaidTiers[0] = new(CurrentExpansion, EncounterDifficulty.Savage, 605, 600,
                "Asphodelos " + Localize("Savage", "Savage"), new uint[] { 30112, 30114, 30110, 30108 });
            CurrentExpansion.SavageRaidTiers[1] = new(CurrentExpansion, EncounterDifficulty.Savage, 635, 630,
                "Abyssos " + Localize("Savage", "Savage"), new uint[] { 30117, 30121, 30119, 30123 });
            CurrentExpansion.NormalRaidTiers[0] = new(CurrentExpansion, EncounterDifficulty.Normal, 590, 580, "Asphodelos", System.Array.Empty<uint>());
            CurrentExpansion.NormalRaidTiers[1] = new(CurrentExpansion, EncounterDifficulty.Normal, 620, 610, "Abyssos", System.Array.Empty<uint>());
        }
        internal readonly GameExpansion CurrentExpansion;

        internal readonly Dictionary<uint, ItemIDCollection> ItemContainerDB = new()
        {
            //6.0
            { 35734, new ItemIDRange(35245, 35264) },//Asphodelos weapon coffer
            { 35735, new ItemIDList(35265, 35270, 35275, 35280, 35285, 35290, 35295) },//Asphodelos head gear coffer
            { 35736, new ItemIDList(35266, 35271, 35276, 35281, 35286, 35291, 35296) },//Asphodelos chest gear coffer
            { 35737, new ItemIDList(35267, 35272, 35277, 35282, 35287, 35292, 35297) },//Asphodelos hand gear coffer
            { 35738, new ItemIDList(35268, 35273, 35278, 35283, 35288, 35293, 35298) },//Asphodelos leg gear coffer
            { 35739, new ItemIDList(35269, 35274, 35279, 35284, 35289, 35294, 35299) },//Asphodelos foot gear coffer
            { 35740, new ItemIDRange(35300, 35304) },//Asphodelos earring coffer
            { 35741, new ItemIDRange(35305, 35309) },//Asphodelos necklace coffer
            { 35742, new ItemIDRange(35310, 35314) },//Asphodelos bracelet coffer
            { 35743, new ItemIDRange(35315, 35319) },//Asphodelos ring coffers
            //6.2
            { 38390, new ItemIDRange(38081, 38099) },//Abyssos weapon coffer
            { 38391, new ItemIDList(38101, 38106, 38111, 38116, 38121, 38126, 38131) },//Abyssos head gear coffer
            { 38392, new ItemIDList(38102, 38107, 38112, 38117, 38122, 38127, 38132) },//Abyssos chest gear coffer
            { 38393, new ItemIDList(38103, 38108, 38113, 38118, 38123, 38128, 38133) },//Abyssos hand gear coffer
            { 38394, new ItemIDList(38104, 38109, 38114, 38119, 38124, 38129, 38134) },//Abyssos leg gear coffer
            { 38395, new ItemIDList(38105, 38110, 38115, 38120, 38125, 38130, 38135) },//Abyssos foot gear coffer
            { 38396, new ItemIDRange(38136, 38140) },//Abyssos earring coffer
            { 38397, new ItemIDRange(38141, 38145) },//Abyssos necklace coffer
            { 38398, new ItemIDRange(38146, 38150) },//Abyssos bracelet coffer
            { 38399, new ItemIDRange(38151, 38155) },//Abyssos ring coffers
        };
        internal readonly Dictionary<uint, ItemSource> ItemSourceDB = new Dictionary<ItemIDCollection, ItemSource>()
        {
            { new ItemIDRange(34810,34829), ItemSource.Dungeon }, //Etheirys  (The Aitiascope)
            { new ItemIDRange(34830,34849), ItemSource.Dungeon }, //The last (Dead Ends)
            { new ItemIDRange(34850,34924), ItemSource.Tome }, //Moonward Tomestone
            { new ItemIDRange(34925,34944), ItemSource.Trial }, //Divine Light
            { new ItemIDRange(34945,34964), ItemSource.Trial }, //Eternal Dark
            { new ItemIDRange(34965,35019), ItemSource.Raid }, //Asphodelos
            { new ItemIDRange(35020,35094), ItemSource.Crafted }, //Classical
            { new ItemIDRange(35095,35169), ItemSource.Tome }, //Radiant Tomestone
            { new ItemIDRange(35170,35244), ItemSource.Tome }, //Aug Radiant Tomestone
            { new ItemIDRange(35245,35319), ItemSource.Raid }, //Asphodelos Savage
            { new ItemIDRange(35320,35340), ItemSource.undefined }, //High Durium
            { new ItemIDRange(35341,35361), ItemSource.undefined }, //Bismuth
            { new ItemIDRange(35362,35382), ItemSource.undefined }, //Mangaganese
            { new ItemIDRange(35383,35403), ItemSource.undefined }, //Chondrite
            //missing items
            { new ItemIDRange(36718,36792), ItemSource.Crafted }, //Augm Classical
            //missing items
            { new ItemIDRange(36923,36942), ItemSource.Trial }, //Bluefeather
            //missing items
            { new ItemIDRange(37131,37165), ItemSource.AllianceRaid }, //Panthean
            { new ItemIDRange(37166,37227), ItemSource.Dungeon }, //Darbar (alzadaals Legacy)
            //missing items
            { new ItemIDRange(37742,37816), ItemSource.Crafted }, //Rinascita
            //missing items
            { new ItemIDRange(37856,37875), ItemSource.Trial }, //Windswept
            { new ItemIDRange(37876,37930), ItemSource.Raid }, //Abyssos
            { new ItemIDRange(37931,38005), ItemSource.Tome }, //Lunar Envoy Tomestone
            { new ItemIDRange(38006,38080), ItemSource.Tome }, //Aug Lunar Envoy Tomestone
            { new ItemIDRange(38081,38155), ItemSource.Raid }, //Abyssos savage
            { new ItemIDRange(38156,38210), ItemSource.Dungeon }, //Troian
            //missing items
            { new ItemIDRange(38400,38419), ItemSource.Relic }, //Manderville

        }.ExplodeIDCollection();
        internal readonly Dictionary<uint, InstanceWithLoot> InstanceDB = new()
        {
            { 30108, new(30108, ContentType.Raid, "Hesperos (P4S)", 35826, new ItemIDList((35245, 35264),35734,35736)) },
            { 30110, new(30110, ContentType.Raid, "Phoinix (P3S)", 35825, new ItemIDList(35735,35737,35738,35739,35828,35829)) },
            { 30112, new(30112, ContentType.Raid, "Erichthonios (P1S)", 35823,new ItemIDRange(35740,35743)) },
            { 30114, new(30114, ContentType.Raid, "Hippokampos (P2S)", 35824, new ItemIDList(35735,35737,35739,35830,35831)) },
            //6.2
            { 30117, new(30117, ContentType.Raid, "Proto-Carbuncle (P5S)", 38381,new ItemIDRange(38396,38399)) },
            { 30119, new(30119, ContentType.Raid, "Agdistis (P7S)", 38383, new ItemIDList(38391,38393,38395,38386,38387)) },
            { 30121, new(30121, ContentType.Raid, "Hegemone (P6S)", 38382, new ItemIDList(38391,38393,38395,38388,38389)) },
            { 30123, new(30123, ContentType.Raid, "Hephaistos (P8S)", 38384,new ItemIDList((38081, 38099),38390,38392)) },
        };
    }
    public static class CuratedDataExtension
    {
        public static Dictionary<uint, T> ExplodeIDCollection<T>(this Dictionary<ItemIDCollection, T> source)
        {
            Dictionary<uint, T> result = new();
            foreach ((var ids, var val) in source)
                foreach (uint id in ids)
                    result.Add(id, val);
            return result;
        }
    }
}
