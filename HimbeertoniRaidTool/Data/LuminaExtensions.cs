using System;
using System.Collections.Generic;
using System.Linq;
using HimbeertoniRaidTool.Data;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace HimbeertoniRaidTool.Data
{

}
namespace Lumina.Excel.Extensions
{
    public static class EquipSlotCategoryExtensions
    {
        public static bool Contains(this EquipSlotCategory? self, GearSetSlot? slot) => self.HasValueAt(slot, 1);
        public static bool Disallows(this EquipSlotCategory? self, GearSetSlot? slot) => self.HasValueAt(slot, -1);
        public static bool HasValueAt(this EquipSlotCategory? self, GearSetSlot? slot, sbyte value)
        {
            if (self == null)
                return false;
            return slot switch
            {
                GearSetSlot.MainHand => self.MainHand == value,
                GearSetSlot.Head => self.Head == value,
                GearSetSlot.Body => self.Body == value,
                GearSetSlot.Hands => self.Gloves == value,
                GearSetSlot.Waist => self.Waist == value,
                GearSetSlot.Legs => self.Legs == value,
                GearSetSlot.Feet => self.Feet == value,
                GearSetSlot.OffHand => self.OffHand == value,
                GearSetSlot.Ear => self.Ears == value,
                GearSetSlot.Neck => self.Neck == value,
                GearSetSlot.Wrist => self.Wrists == value,
                GearSetSlot.Ring1 => self.FingerR == value,
                GearSetSlot.Ring2 => self.FingerL == value,
                GearSetSlot.SoulCrystal => self.SoulCrystal == value,
                _ => false,
            };
        }
        public static IEnumerable<GearSetSlot> AvailableSlots(this EquipSlotCategory? self)
        {
            for (int i = 0; i < (int)GearSetSlot.SoulCrystal; i++)
                if (self?.Contains((GearSetSlot)i) ?? false)
                    yield return (GearSetSlot)i;
        }
        [Obsolete("Evaluate for all available slots")]
        public static GearSetSlot ToSlot(this EquipSlotCategory? self)
            => self.AvailableSlots().FirstOrDefault(GearSetSlot.None);
    }
    public static class ClassJobCategoryExtensions
    {
        public static List<Job> ToJob(this ClassJobCategory self)
        {
            List<Job> jobs = new List<Job>();
            for (int i = 0; i < (int)Job.SGE; i++)
            {
                if (self.Contains((Job)i))
                {
                    jobs.Add((Job)i);
                }
            }
            return jobs;
        }

        public static bool Contains(this ClassJobCategory? cat, Job? job) => cat is not null && job switch
        {
            Job.ADV => cat.ADV,
            Job.AST => cat.AST,
            Job.BLM => cat.BLM,
            Job.BLU => cat.BLU,
            Job.BRD => cat.BRD,
            Job.DNC => cat.DNC,
            Job.DRG => cat.DRG,
            Job.DRK => cat.DRK,
            Job.GNB => cat.GNB,
            Job.MCH => cat.MCH,
            Job.MNK => cat.MNK,
            Job.NIN => cat.NIN,
            Job.PLD => cat.PLD,
            Job.RDM => cat.RDM,
            Job.RPR => cat.RPR,
            Job.SAM => cat.SAM,
            Job.SCH => cat.SCH,
            Job.SGE => cat.SGE,
            Job.SMN => cat.SMN,
            Job.WAR => cat.WAR,
            Job.WHM => cat.WHM,
            Job.GLA => cat.GLA,
            Job.MRD => cat.MRD,
            Job.LNC => cat.LNC,
            Job.PGL => cat.PGL,
            Job.ARC => cat.ARC,
            Job.THM => cat.THM,
            Job.ACN => cat.ACN,
            Job.CNJ => cat.CNJ,
            Job.ROG => cat.ROG,
            _ => false,
        };
    }
}
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Lumina.Excel.CustomSheets
{
    [Sheet("SpecialShop", columnHash: 0x24d7a0ef)]
    public class CustomSpecialShop : ExcelRow
    {
        public const int NUM_ENTRIES = 60;
        public const int GLOBAL_OFFSET = 1;
        public const int NUM_RECEIVE = 2;
        public const int RECEIVE_SIZE = 4;
        public const int NUM_COST = 3;
        public const int COST_SIZE = 4;
        public const int ADDITIONAL_SHOPENTRY_FIELDS = 5;
        public const int COST_OFFSET = GLOBAL_OFFSET + NUM_ENTRIES * NUM_RECEIVE * RECEIVE_SIZE;
        public const int AFTER_COST_OFFSET = COST_OFFSET + NUM_ENTRIES * NUM_COST * COST_SIZE;
        public const int AFTER_ENTRIES_OFFSET = AFTER_COST_OFFSET + NUM_ENTRIES * ADDITIONAL_SHOPENTRY_FIELDS;


        public SeString Name { get; set; }
        public ShopEntry[] ShopEntries = new ShopEntry[NUM_ENTRIES];
        public byte UseCurrencyType { get; set; }
        public LazyRow<Quest> UnlockQuest { get; set; }
        public LazyRow<DefaultTalk> CompleteText { get; set; }
        public LazyRow<DefaultTalk> NotCompleteText { get; set; }
        public uint UnknownData1505 { get; set; }
        public bool UnknownData1506 { get; set; }
        public ushort UnknownData1507 { get; set; }
        public uint UnknownData1508 { get; set; }
        public bool UnknownData1509 { get; set; }
        public class ShopEntry
        {
            public ItemReceiveEntry[] ItemReceiveEntries { get; set; } = new ItemReceiveEntry[NUM_RECEIVE];
            public ItemCostEntry[] ItemCostEntries { get; set; } = new ItemCostEntry[NUM_COST];
            public LazyRow<Quest> Quest { get; set; }
            public int UnknownData1261 { get; set; }
            public LazyRow<Achievement> AchievementUnlock { get; set; }
            public byte UnknownData1381 { get; set; }
            public ushort PatchNumber { get; set; }
        }
        public class ItemReceiveEntry
        {

            public LazyRow<Item> Item { get; set; }
            public uint Count { get; set; }
            public LazyRow<SpecialShopItemCategory> SpecialShopItemCategory { get; set; }
            public bool HQ { get; set; }
        }
        public class ItemCostEntry
        {
            public LazyRow<Item> Item { get; set; }
            public uint Count { get; set; }
            public bool HQ { get; set; }
            public ushort CollectabilityRatingCost { get; set; }
        }
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);

            Name = parser.ReadColumn<SeString>(0)!;
            for (int i = 0; i < ShopEntries.Length; i++)
            {
                ShopEntries[i] = new();
                //Receives
                for (int j = 0; j < NUM_RECEIVE; j++)
                {
                    ShopEntries[i].ItemReceiveEntries[j] = new();
                    ShopEntries[i].ItemReceiveEntries[j].Item = new LazyRow<Item>(gameData, parser.ReadColumn<int>(GLOBAL_OFFSET + j * NUM_ENTRIES * RECEIVE_SIZE + 0 * NUM_ENTRIES + i), language);
                    ShopEntries[i].ItemReceiveEntries[j].Count = parser.ReadColumn<uint>(GLOBAL_OFFSET + j * NUM_ENTRIES * RECEIVE_SIZE + 1 * NUM_ENTRIES + i);
                    ShopEntries[i].ItemReceiveEntries[j].SpecialShopItemCategory =
                        new LazyRow<SpecialShopItemCategory>(gameData, parser.ReadColumn<int>(GLOBAL_OFFSET + j * NUM_ENTRIES * RECEIVE_SIZE + 2 * NUM_ENTRIES + i), language);
                    ShopEntries[i].ItemReceiveEntries[j].HQ = parser.ReadColumn<bool>(GLOBAL_OFFSET + j * NUM_ENTRIES * RECEIVE_SIZE + 3 * NUM_ENTRIES + i);
                }
                //COSTS
                for (int j = 0; j < NUM_COST; j++)
                {
                    ShopEntries[i].ItemCostEntries[j] = new();
                    ShopEntries[i].ItemCostEntries[j].Item = new LazyRow<Item>(gameData, parser.ReadColumn<int>(COST_OFFSET + j * NUM_ENTRIES * COST_SIZE + 0 * NUM_ENTRIES + i), language);
                    ShopEntries[i].ItemCostEntries[j].Count = parser.ReadColumn<uint>(COST_OFFSET + j * NUM_ENTRIES * COST_SIZE + 1 * NUM_ENTRIES + i);
                    ShopEntries[i].ItemCostEntries[j].HQ = parser.ReadColumn<bool>(COST_OFFSET + j * NUM_ENTRIES * COST_SIZE + 2 * NUM_ENTRIES + i);
                    ShopEntries[i].ItemCostEntries[j].CollectabilityRatingCost = parser.ReadColumn<ushort>(COST_OFFSET + j * NUM_ENTRIES * COST_SIZE + 3 * NUM_ENTRIES + i);
                }
                ShopEntries[i].Quest =
                    new LazyRow<Quest>(gameData, parser.ReadColumn<int>(AFTER_COST_OFFSET + 0 * NUM_ENTRIES + i), language);
                ShopEntries[i].UnknownData1261 = parser.ReadColumn<int>(AFTER_COST_OFFSET + 1 * NUM_ENTRIES + i);
                ShopEntries[i].AchievementUnlock =
                    new LazyRow<Achievement>(gameData, parser.ReadColumn<int>(AFTER_COST_OFFSET + 2 * NUM_ENTRIES + i), language);
                ShopEntries[i].UnknownData1381 = parser.ReadColumn<byte>(AFTER_COST_OFFSET + 3 * NUM_ENTRIES + i);
                ShopEntries[i].PatchNumber = parser.ReadColumn<ushort>(AFTER_COST_OFFSET + 4 * NUM_ENTRIES + i);
            }
            this.UseCurrencyType = parser.ReadColumn<byte>(AFTER_ENTRIES_OFFSET);
            this.UnlockQuest = new LazyRow<Quest>(gameData, parser.ReadColumn<int>(AFTER_ENTRIES_OFFSET + 1), language);
            this.CompleteText = new LazyRow<DefaultTalk>(gameData, parser.ReadColumn<int>(AFTER_ENTRIES_OFFSET + 2), language);
            this.NotCompleteText = new LazyRow<DefaultTalk>(gameData, parser.ReadColumn<int>(AFTER_ENTRIES_OFFSET + 3), language);
            this.UnknownData1505 = parser.ReadColumn<uint>(AFTER_ENTRIES_OFFSET + 4);
            this.UnknownData1506 = parser.ReadColumn<bool>(AFTER_ENTRIES_OFFSET + 5);
            this.UnknownData1507 = parser.ReadColumn<ushort>(AFTER_ENTRIES_OFFSET + 6);
            this.UnknownData1508 = parser.ReadColumn<uint>(AFTER_ENTRIES_OFFSET + 7);
            this.UnknownData1509 = parser.ReadColumn<bool>(AFTER_ENTRIES_OFFSET + 8);
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
