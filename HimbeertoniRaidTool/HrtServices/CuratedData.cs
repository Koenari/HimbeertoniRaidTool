using System.Collections.Generic;
using HimbeertoniRaidTool.Data;
using static HimbeertoniRaidTool.Data.EncounterDifficulty;
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
            CurrentExpansion.SavageRaidTiers[0] = new(Savage, 605, 600,
                "Asphodelos " + Localize("Savage", "Savage"), new uint[] { 30112, 30114, 30110, 30108 });
            CurrentExpansion.SavageRaidTiers[1] = new(Savage, 635, 630,
                "Abyssos " + Localize("Savage", "Savage"), new uint[] { 30117, 30121, 30119, 30123 });
            CurrentExpansion.NormalRaidTiers[0] = new(Normal, 590, 580, "Asphodelos", System.Array.Empty<uint>());
            CurrentExpansion.NormalRaidTiers[1] = new(Normal, 620, 610, "Abyssos", System.Array.Empty<uint>());
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
        //I only record Gear and items used to get gear
        internal readonly HashSet<InstanceWithLoot> InstanceDB = new()
        {
            //6.0
            new(78   , Normal , (34155,34229)),
            new(79   , Normal , (34830,34849)),
            new(80   , Normal , (34305,34379)),
            new(81   , Normal , (34810,34829)),
            new(82   , Normal , (34605,34679)),
            new(83   , Normal , (34455,34529)),
            new(84   , Normal , (34830,34849)),
            new(85   , Normal , (34830,34849)),
            new(20077, Normal , 36283),
            new(20078, Extreme, (34925,34944)),
            new(20079, Normal , 36275),
            new(20080, Normal , 36282),
            new(20081, Extreme, (34945,34964)),
            new(30107, Normal , (35817, 35822)),
            new(30108, Savage , new ItemIDList((35245, 35264),35734,35736),35826),
            new(30109, Normal , (35817, 35822)),
            new(30110, Savage , new ItemIDList(35735, 35737, 35738, 35739, 35828, 35829), 35825),
            new(30111, Normal , (35817, 35822)),
            new(30112, Savage , new ItemIDRange(35740, 35743), 35823),
            new(30113, Normal , (35817, 35822)),
            new(30114, Savage , new ItemIDList(35735, 35737, 35739, 35830, 35831), 35824),
            //6.1
            new(87   , Normal  , (37166, 37227)),
            new(20083, Extreme , (36923,36942), 36809),
            new(30106, Ultimate, 36810),
            new(30115, Normal  , (37131,37165),36820),
            //6.2
            new(88   , Normal , (38156, 38210)),
            new(20084, Normal , 38437),
            new(20085, Extreme, (37856,37875),38374),
            new(30116, Normal , (38375, 38380)),
            new(30117, Savage , new ItemIDRange(38396, 38399), 38381),
            new(30118, Normal , (38375, 38380)),
            new(30119, Savage , new ItemIDList(38391, 38393,38394, 38395, 38386, 38387), 38383),
            new(30120, Normal , (38375, 38380)),
            new(30121, Savage , new ItemIDList(38391, 38393, 38395, 38388, 38389), 38382),
            new(30122, Normal , (38375, 38380),38385),
            new(30123, Savage , new ItemIDList((38081, 38099), 38390, 38392), 38384),
        };
    }
}
