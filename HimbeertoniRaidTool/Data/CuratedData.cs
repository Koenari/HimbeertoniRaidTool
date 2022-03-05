using System.Collections.Generic;

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
        };
    }
    public static class CuratedDataExtension
    {
    }
}
