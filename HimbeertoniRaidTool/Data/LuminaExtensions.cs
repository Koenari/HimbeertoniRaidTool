using HimbeertoniRaidTool.Data;
using Lumina.Excel.GeneratedSheets;
namespace HimbeertoniRaidTool.Data
{

}
namespace Lumina.Excel.Extensions
{
    public static class EquipSlotCategoryExtensions
    {
        public static bool Contains(this EquipSlotCategory self, GearSetSlot? slot) => self.HasValueAt(slot, 1);
        public static bool Disallows(this EquipSlotCategory self, GearSetSlot? slot) => self.HasValueAt(slot, -1);
        public static bool HasValueAt(this EquipSlotCategory self, GearSetSlot? slot, sbyte value)
        {
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
        public static GearSetSlot ToSlot(this EquipSlotCategory self)
        {
            for (int i = 0; i < (int)GearSetSlot.SoulCrystal; i++)
            {
                if (self.Contains((GearSetSlot)i))
                {
                    return (GearSetSlot)i;
                }
            }
            return GearSetSlot.None;
        }
    }
    public static class ClassJobCategoryExtensions
    {
        public static Job ToJob(this ClassJobCategory self)
        {
            for (int i = 0; i < (int)Job.SGE; i++)
            {
                if (self.Contains((Job)i))
                {
                    return (Job)i;
                }
            }
            return Job.ADV;
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
