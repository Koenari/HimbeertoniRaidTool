using System.Collections.Generic;
using static HimbeertoniRaidTool.Data.AvailableClasses;

namespace HimbeertoniRaidTool.Data
{
    /// <summary>
    /// This class/file encapsulates all data that needs to be update for new Patches of the game.
    /// It is intended to be servicable by someone not familiar with programming.
    /// First there are entries for each RaidTier. To make a new one make it in the following way:
    /// 'public static RaidTier' DescriptiveName(no spaces) ' => 
    ///     new('Expansion number, Raid number (1-3), EncounterDifficulty, ILvl of Weapons, iLvl of other Gear, "A descriptive name");
    /// Then there is a list which links loot  to boss encounter
    ///     Just add entries as a new line of format:
    ///     { ItemID(S), (RaidTier (Named like above), Encounter Number(1-4)) },
    ///AFter that information on which Slot Item Coffers belong to
    ///     Just add entries as a new line of format:
    ///     { ItemID, GearSlot.Name },
    /// </summary>
    internal static class CuratedData
    {
        public static RaidTier CurrentRaidSavage => AsphodelosSavage;
        public static RaidTier CurrentRaidNormal => AsphodelosNormal;
        public static RaidTier AsphodelosNormal => new(6, 1, EncounterDifficulty.Normal, 590, 580, "Asphodelos");
        public static RaidTier AsphodelosSavage => new(6, 1, EncounterDifficulty.Savage, 605, 600, "Asphodelos Savage");

        public static readonly Dictionary<ItemIDRange, LootSource> LootSourceDB = new()
        {
            { (35245, 35264), (AsphodelosSavage, 4) },//All Asphodelos Weapons
            { 35734, (AsphodelosSavage, 4) },//Asphodelos weapon coffer
            {
                35735,
                new((AsphodelosSavage, 2),
                    (AsphodelosSavage, 3))
            },//Asphodelos head gear coffer
            { 35736, (AsphodelosSavage, 4) },//Asphodelos chest gear coffer
            {
                35737,
                new((AsphodelosSavage, 2),
                    (AsphodelosSavage, 3))
            },//Asphodelos hand gear coffer
            { 35738, (AsphodelosSavage, 3) },//Asphodelos leg gear coffer
            {
                35739,
                new((AsphodelosSavage, 2),
                    (AsphodelosSavage, 3))
            },//Asphodelos foot gear coffer
            { 35740, (AsphodelosSavage, 1) },//Asphodelos earring coffer
            { 35741, (AsphodelosSavage, 1) },//Asphodelos necklace coffer
            { 35742, (AsphodelosSavage, 1) },//Asphodelos bracelet coffer
            { 35743, (AsphodelosSavage, 1) },//Asphodelos ring coffers
            { 35828, (AsphodelosSavage, 3) },//Radiant Roborant
            { 35829, (AsphodelosSavage, 3) },//Radiant Twine
            { 35830, (AsphodelosSavage, 2) },//Radiant Coating
            { 35831, (AsphodelosSavage, 2) } //Discal Tomestone

        };
        public static readonly Dictionary<ItemIDRange, GearSetSlot> SlotOverrideDB = new()
        {
            { 35734, GearSetSlot.MainHand },//Asphodelos weapon coffer
            { 35735, GearSetSlot.Head },//Asphodelos head gear coffer
            { 35736, GearSetSlot.Body },//Asphodelos chest gear coffer
            { 35737, GearSetSlot.Hands },//Asphodelos hand gear coffer
            { 35738, GearSetSlot.Legs },//Asphodelos leg gear coffer
            { 35739, GearSetSlot.Feet },//Asphodelos foot gear coffer
            { 35740, GearSetSlot.Ear },//Asphodelos earring coffer
            { 35741, GearSetSlot.Neck },//Asphodelos necklace coffer
            { 35742, GearSetSlot.Wrist },//Asphodelos bracelet coffer
            { 35743, GearSetSlot.Ring1 },//Asphodelos ring coffers
            //Todo
            /*{ 35828, (AsphodelosSavage, 3) },//Radiant Roborant
            { 35829, (AsphodelosSavage, 3) },//Radiant Twine
            { 35830, (AsphodelosSavage, 2) },//Radiant Coating
            { 35831, (AsphodelosSavage, 2) } //Discal Tomestone*/
        };
        public static readonly KeyContainsDictionary<GearSource> GearSourceDictionary = new()
        {
            //6.0x
            { "Asphodelos", GearSource.Raid },
            { "Radiant", GearSource.Tome },
            { "Classical", GearSource.Crafted },
            { "Limbo", GearSource.Raid },
            { "Last", GearSource.Dungeon },
            { "Eternal Dark", GearSource.Trial },
            { "Moonward", GearSource.Tome },
            { "Divine Light", GearSource.Trial },
            { "Panthean", GearSource.AllianceRaid },
            { "Bluefeather", GearSource.Trial },
        };
        /// <summary>
        /// Holds a list of Etro IDs to use as BiS sets if users did not enter a preferred BiS
        /// </summary>
        public static Dictionary<AvailableClasses, string> DefaultBIS { get; set; } = new Dictionary<AvailableClasses, string>
        {
            { AST, "88647808-8a28-477b-b285-687bdcbff2d4" },
            { BLM, "327d090b-2d5a-4c3c-9eb9-8fd42342cce3" },
            { BLU, "3db73aab-2968-4eb7-b392-d524f5a1b783" },
            { BRD, "cec981af-25c7-4ffb-905e-3024411b797a" },
            { DNC, "fd333e44-0f90-42a6-a070-044b332bb54e" },
            { DRG, "8bdd42db-a318-41a0-8903-14efa5e0774b" },
            { DRK, "dda8aef5-41e4-40b6-813c-df306e1f1cee" },
            { GNB, "88fbea7d-3b43-479c-adb8-b87c9d6cb5f9" },
            { MCH, "6b4b1ba5-a821-41a0-b070-b1f50e986f85" },
            { MNK, "841ecfdb-41fe-44b4-8764-b3b08e223f8c" },
            { NIN, "b9876a4d-aba9-48f0-9c03-cb542af46a29" },
            { PLD, "38fe3778-f2c1-4300-99e4-b58a0445e969" },
            { RDM, "80fdec19-1109-4ca2-8172-53d4dda44144" },
            { RPR, "b301e789-96da-42f2-9628-95f68345e35b" },
            { SAM, "3a7c7f45-b715-465d-a377-db458045506a" },
            { SCH, "f1802c19-d766-40f0-b781-f5b965cb964e" },
            { SGE, "287bf053-05aa-4762-8275-b0fd9b13702a" },
            { SMN, "840a5088-23fa-49c5-a12a-3731ca55b4a6" },
            { WAR, "6d0d2d4d-a477-44ea-8002-862eca8ef91d" },
            { WHM, "9d1d3b92-9d02-4844-be4f-7622d69de67b" },
        };
    }
    public static class CuratedDataExtension
    {
    }
}
