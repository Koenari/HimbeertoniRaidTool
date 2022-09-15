using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.GeneratedSheets;
namespace Lumina.Excel.Extensions
{
    public static class EquipSlotCategoryExtensions
    {
        public static bool Contains(this EquipSlotCategory self, GearSetSlot slot)
        {
            switch (slot)
            {
                case GearSetSlot.MainHand: return self.MainHand != 0;
                case GearSetSlot.Head: return self.Head != 0;
                case GearSetSlot.Body: return self.Body != 0;
                case GearSetSlot.Hands: return self.Gloves != 0;
                case GearSetSlot.Waist: return self.Waist != 0;
                case GearSetSlot.Legs: return self.Legs != 0;
                case GearSetSlot.Feet: return self.Feet != 0;
                case GearSetSlot.OffHand: return self.OffHand != 0;
                case GearSetSlot.Ear: return self.Ears != 0;
                case GearSetSlot.Neck: return self.Neck != 0;
                case GearSetSlot.Wrist: return self.Wrists != 0;
                case GearSetSlot.Ring1: return self.FingerR != 0;
                case GearSetSlot.Ring2: return self.FingerL != 0;
                case GearSetSlot.SoulCrystal: return self.SoulCrystal != 0;
            }

            return false;
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
        public static bool Contains(this ClassJobCategory? cat, Job? job) => cat is null ? false : job switch
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
