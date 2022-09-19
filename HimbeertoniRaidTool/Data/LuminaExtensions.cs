using HimbeertoniRaidTool.Data;
using Lumina.Excel.GeneratedSheets;
namespace Lumina.Excel.Extensions
{
    public static class EquipSlotCategoryExtensions
    {
        public static bool Contains(this EquipSlotCategory self, GearSetSlot? slot)
        {
            return slot switch
            {
                GearSetSlot.MainHand => self.MainHand != 0,
                GearSetSlot.Head => self.Head != 0,
                GearSetSlot.Body => self.Body != 0,
                GearSetSlot.Hands => self.Gloves != 0,
                GearSetSlot.Waist => self.Waist != 0,
                GearSetSlot.Legs => self.Legs != 0,
                GearSetSlot.Feet => self.Feet != 0,
                GearSetSlot.OffHand => self.OffHand != 0,
                GearSetSlot.Ear => self.Ears != 0,
                GearSetSlot.Neck => self.Neck != 0,
                GearSetSlot.Wrist => self.Wrists != 0,
                GearSetSlot.Ring1 => self.FingerR != 0,
                GearSetSlot.Ring2 => self.FingerL != 0,
                GearSetSlot.SoulCrystal => self.SoulCrystal != 0,
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
