using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Data
{
    public static class AllaganLibrary
    {
        private readonly static Dictionary<int, (int MP, int MAIN, int SUB, int DIV, int HP, int ELMT, int THREAT)> LevelTable;
        static AllaganLibrary()
        {
            LevelTable = new();
            LevelTable[1] = (10000, 20, 56, 56, 86, 52, 2);
            LevelTable[2] = (10000, 21, 57, 57, 101, 54, 2);
            LevelTable[3] = (10000, 22, 60, 60, 109, 56, 3);
            LevelTable[4] = (10000, 24, 62, 62, 116, 58, 3);
            LevelTable[5] = (10000, 26, 65, 65, 123, 60, 3);
            LevelTable[6] = (10000, 27, 68, 68, 131, 62, 3);
            LevelTable[7] = (10000, 29, 70, 70, 138, 64, 4);
            LevelTable[8] = (10000, 31, 73, 73, 145, 66, 4);
            LevelTable[9] = (10000, 33, 76, 76, 153, 68, 4);
            LevelTable[10] = (10000, 35, 78, 78, 160, 70, 5);
            LevelTable[11] = (10000, 36, 82, 82, 174, 73, 5);
            LevelTable[12] = (10000, 38, 85, 85, 188, 75, 5);
            LevelTable[13] = (10000, 41, 89, 89, 202, 78, 6);
            LevelTable[14] = (10000, 44, 93, 93, 216, 81, 6);
            LevelTable[15] = (10000, 46, 96, 96, 230, 84, 7);
            LevelTable[16] = (10000, 49, 100, 100, 244, 86, 7);
            LevelTable[17] = (10000, 52, 104, 104, 258, 89, 8);
            LevelTable[18] = (10000, 54, 109, 109, 272, 93, 9);
            LevelTable[19] = (10000, 57, 113, 113, 286, 95, 9);
            LevelTable[20] = (10000, 60, 116, 116, 300, 98, 10);
            LevelTable[21] = (10000, 63, 122, 122, 333, 102, 10);
            LevelTable[22] = (10000, 67, 127, 127, 366, 105, 11);
            LevelTable[23] = (10000, 71, 133, 133, 399, 109, 12);
            LevelTable[24] = (10000, 74, 138, 138, 432, 113, 13);
            LevelTable[25] = (10000, 78, 144, 144, 465, 117, 14);
            LevelTable[26] = (10000, 81, 150, 150, 498, 121, 15);
            LevelTable[27] = (10000, 85, 155, 155, 531, 125, 16);
            LevelTable[28] = (10000, 89, 162, 162, 564, 129, 17);
            LevelTable[29] = (10000, 92, 168, 168, 597, 133, 18);
            LevelTable[30] = (10000, 97, 173, 173, 630, 137, 19);
            LevelTable[31] = (10000, 101, 181, 181, 669, 143, 20);
            LevelTable[32] = (10000, 106, 188, 188, 708, 148, 22);
            LevelTable[33] = (10000, 110, 194, 194, 747, 153, 23);
            LevelTable[34] = (10000, 115, 202, 202, 786, 159, 25);
            LevelTable[35] = (10000, 119, 209, 209, 825, 165, 27);
            LevelTable[36] = (10000, 124, 215, 215, 864, 170, 29);
            LevelTable[37] = (10000, 128, 223, 223, 903, 176, 31);
            LevelTable[38] = (10000, 134, 229, 229, 942, 181, 33);
            LevelTable[39] = (10000, 139, 236, 236, 981, 186, 35);
            LevelTable[40] = (10000, 144, 244, 244, 1020, 192, 38);
            LevelTable[41] = (10000, 150, 253, 253, 1088, 200, 40);
            LevelTable[42] = (10000, 155, 263, 263, 1156, 207, 43);
            LevelTable[43] = (10000, 161, 272, 272, 1224, 215, 46);
            LevelTable[44] = (10000, 166, 283, 283, 1292, 223, 49);
            LevelTable[45] = (10000, 171, 292, 292, 1360, 231, 52);
            LevelTable[46] = (10000, 177, 302, 302, 1428, 238, 55);
            LevelTable[47] = (10000, 183, 311, 311, 1496, 246, 58);
            LevelTable[48] = (10000, 189, 322, 322, 1564, 254, 62);
            LevelTable[49] = (10000, 196, 331, 331, 1632, 261, 66);
            LevelTable[50] = (10000, 202, 341, 341, 1700, 269, 70);
            LevelTable[51] = (10000, 204, 342, 366, 0, 0, 0);
            LevelTable[52] = (10000, 205, 344, 392, 0, 0, 0);
            LevelTable[53] = (10000, 207, 345, 418, 0, 0, 0);
            LevelTable[54] = (10000, 209, 346, 444, 0, 0, 0);
            LevelTable[55] = (10000, 210, 347, 470, 0, 0, 0);
            LevelTable[56] = (10000, 212, 349, 496, 0, 0, 0);
            LevelTable[57] = (10000, 214, 350, 522, 0, 0, 0);
            LevelTable[58] = (10000, 215, 351, 548, 0, 0, 0);
            LevelTable[59] = (10000, 217, 352, 574, 0, 0, 0);
            LevelTable[60] = (10000, 218, 354, 600, 0, 0, 0);
            LevelTable[61] = (10000, 224, 355, 630, 0, 0, 0);
            LevelTable[62] = (10000, 228, 356, 660, 0, 0, 0);
            LevelTable[63] = (10000, 236, 357, 690, 1700, 0, 0);
            LevelTable[64] = (10000, 244, 358, 720, 0, 0, 0);
            LevelTable[65] = (10000, 252, 359, 750, 0, 0, 0);
            LevelTable[66] = (10000, 260, 360, 780, 0, 0, 0);
            LevelTable[67] = (10000, 268, 361, 810, 0, 0, 0);
            LevelTable[68] = (10000, 276, 362, 840, 0, 0, 0);
            LevelTable[69] = (10000, 284, 363, 870, 0, 0, 0);
            LevelTable[70] = (10000, 292, 364, 900, 1700, 0, 0);
            LevelTable[71] = (10000, 296, 365, 940, 0, 0, 0);
            LevelTable[72] = (10000, 300, 366, 980, 0, 0, 0);
            LevelTable[73] = (10000, 305, 367, 1020, 0, 0, 0);
            LevelTable[74] = (10000, 310, 368, 1060, 0, 0, 0);
            LevelTable[75] = (10000, 315, 370, 1100, 0, 0, 0);
            LevelTable[76] = (10000, 320, 372, 1140, 0, 0, 0);
            LevelTable[77] = (10000, 325, 374, 1180, 0, 0, 0);
            LevelTable[78] = (10000, 330, 376, 1220, 0, 0, 0);
            LevelTable[79] = (10000, 335, 378, 1260, 0, 0, 0);
            LevelTable[80] = (10000, 340, 380, 1300, 2000, 0, 0);
            LevelTable[81] = (10000, 345, 382, 1360, 0, 0, 0);
            LevelTable[82] = (10000, 350, 384, 1420, 0, 0, 0);
            LevelTable[83] = (10000, 355, 386, 1480, 0, 0, 0);
            LevelTable[84] = (10000, 360, 388, 1540, 0, 0, 0);
            LevelTable[85] = (10000, 365, 390, 1600, 0, 0, 0);
            LevelTable[86] = (10000, 370, 392, 1660, 0, 0, 0);
            LevelTable[87] = (10000, 375, 394, 1720, 0, 0, 0);
            LevelTable[88] = (10000, 380, 396, 1780, 0, 0, 0);
            LevelTable[89] = (10000, 385, 398, 1840, 0, 0, 0);
            LevelTable[90] = (10000, 390, 400, 1900, 3000, 0, 0);
        }
        /// <summary>
        /// This function evaluates a stat to it's respective effective used effect.
        /// Only works for level 90
        /// </summary>
        /// <param name="type">Type of stat that is input</param>
        /// <param name="totalStat">total value of stat</param>
        /// <param name="level">level of the job to evaluate for</param>
        /// <param name="job">current job</param>
        /// <param name="alternative">a way to use alternative formulas for stats that have multiple effects (0 is default furmula)</param
        /// <param name="additionalStats">pass any additional stats that are necessary to calculate given vlaue</param>
        /// <returns>Evaluated value including unit</returns>
        public static string EvaluateStatToDisplay(StatType type, int totalStat, int level, Job? job, int alternative = 0, params (StatType type, int totalStat)[] additionalStats)
        {
            string notAvail = "n.A.";
            float evaluatedValue = EvaluateStat(type, totalStat, level, job, alternative, additionalStats);
            if (float.IsNaN(evaluatedValue))
                return notAvail;
            if (type == StatType.CriticalHit && alternative == 1)
                type = StatType.CriticalHitPower;
            return (type, alternative) switch
            {
                (StatType.CriticalHit, _) => $"{evaluatedValue * 100:N1} %%",
                (StatType.CriticalHitPower, _) => $"{evaluatedValue * 100:N1} %%",
                (StatType.DirectHitRate, _) => $"{evaluatedValue * 100:N1} %%",
                (StatType.Determination, _) => $"{100 + evaluatedValue * 100:N1} %%",
                (StatType.Tenacity, _) => $"{evaluatedValue * 100:N1} %%",
                (StatType.Piety, _) => $"+{evaluatedValue:N0} MP/s",
                //GCD
                (StatType.SkillSpeed, 1) or (StatType.SpellSpeed, 1) => $"{evaluatedValue:N2} s",
                //AA/DoT Multiplier
                (StatType.SkillSpeed, _) or (StatType.SpellSpeed, _) => $"{evaluatedValue * 100:N1} %%",
                (StatType.Defense, _) or (StatType.MagicDefense, _) => $"{evaluatedValue * 100:N1} %%",
                (StatType.Vitality, _) => $"{evaluatedValue:N0} HP",
                (StatType.MagicalDamage, _) or (StatType.PhysicalDamage, _) => $"{evaluatedValue * 100:N2} Dmg/100",
                _ => notAvail
            };
        }
        /// <summary>
        /// This function evaluates a stat to it's respective effective used effect.
        /// Only works for level 90
        /// </summary>
        /// <param name="type">Type of stat that is input</param>
        /// <param name="totalStat">total value of stat</param>
        /// <param name="level">level of the job to evaluate for</param>
        /// <param name="job">current job</param>
        /// <param name="alternative">a way to use alternative formulas for stats that have multiple effects (0 is default formula)</param>
        /// /// <param name="additionalStats">pass any additional stats that are necessary to calculate given vlaue</param>
        /// <returns>Evaluated value (percentage values are in mathematical correct value, means 100% = 1.0)</returns>
        public static float EvaluateStat(StatType type, int totalStat, int level, Job? job, int alternative = 0, params (StatType t, int sum)[] additionalStats)
        {
            if (type == StatType.CriticalHit && alternative == 1)
                type = StatType.CriticalHitPower;
            return (type, alternative) switch
            {

                (StatType.CriticalHitPower, _) or (StatType.CriticalHit, 1) => MathF.Floor(200 * (totalStat - LevelTable[level].SUB) / LevelTable[level].DIV + 1400) / 1000f,
                (StatType.CriticalHit, _) => MathF.Floor(200 * (totalStat - LevelTable[level].SUB / LevelTable[level].DIV + 50)) / 1000f,
                (StatType.DirectHitRate, _) => MathF.Floor(550 * (totalStat - LevelTable[level].SUB) / LevelTable[level].DIV) / 1000f,
                (StatType.Determination, _) => MathF.Floor(140 * (totalStat - LevelTable[level].MAIN) / LevelTable[level].DIV) / 1000f,
                (StatType.Tenacity, 1) => (1000f + MathF.Floor(100 * (totalStat - LevelTable[level].SUB) / LevelTable[level].DIV)) / 1000f,
                (StatType.Tenacity, _) => (1000f - MathF.Floor(100 * (totalStat - LevelTable[level].SUB) / LevelTable[level].DIV)) / 1000f,
                (StatType.Piety, _) => MathF.Floor(150 * (totalStat - LevelTable[level].MAIN) / LevelTable[level].DIV) + 200f,
                //AA/DoT Multiplier
                (StatType.SkillSpeed, 1) or (StatType.SpellSpeed, 1) => (1000f + MathF.Ceiling(130f * (totalStat - LevelTable[level].SUB) / LevelTable[level].DIV)) / 1000f,
                //GCD
                (StatType.SkillSpeed, _) or (StatType.SpellSpeed, _) => MathF.Floor(2500f * (1000 + MathF.Ceiling(130 * (LevelTable[level].SUB - totalStat) / LevelTable[level].DIV)) / 10000f) / 100f,
                (StatType.Defense, _) or (StatType.MagicDefense, _) => MathF.Floor(15 * totalStat / LevelTable[level].DIV) / 100f,
                //ToDO: Still rounding issues
                (StatType.Vitality, _) => MathF.Floor(LevelTable[level].HP * GetJobModifier(StatType.HP, job.GetClassJob()))
                    + MathF.Floor((totalStat - LevelTable[level].MAIN) * GetHPMultiplier(level, job)),
                (StatType.MagicalDamage, _) or (StatType.PhysicalDamage, _) => CalcBaseDamageMultiplier(type, totalStat, level, job, additionalStats),
                _ => float.NaN
            };
        }
        private static float CalcBaseDamageMultiplier(StatType type, int totalStat, int level, Job? job, (StatType t, int sum)[] additionalStats)
        {
            int mainStat = type switch
            {
                StatType.MagicalDamage => additionalStats.Where(x => x.t == StatType.AttackMagicPotency).SingleOrDefault((StatType.None, -1)).Item2,
                StatType.PhysicalDamage => additionalStats.Where(x => x.t == StatType.AttackPower).SingleOrDefault((StatType.None, -1)).Item2,
                _ => -1
            };
            int m = (job.GetRole(), level) switch
            {
                (Role.Tank, 90) => 156,
                (Role.Tank, 80) => 115,
                (Role.Tank, 70) => 105,
                (_, 90) => 195,
                (_, 80) => 165,
                (_, 70) => 125,
                _ => 0
            };
            float trait = job.GetRole() switch
            {
                Role.Caster or Role.Healer => 1.3f,
                Role.Ranged => 1.2f,
                _ => 1f
            };
            if (m == 0 || mainStat < 0)
                return float.NaN;
            float baseDmg = (totalStat + MathF.Floor(LevelTable[level].MAIN * GetJobModifier(job.MainStat(), job.GetClassJob()) / 1000f)) * (100 + MathF.Floor((mainStat - LevelTable[level].MAIN) * m / LevelTable[level].MAIN)) / 100f;
            float determinationMultiplier = additionalStats.Any(x => x.t == StatType.Determination) ?
                EvaluateStat(StatType.Determination, additionalStats.Where(x => x.t == StatType.Determination).Single().sum, level, job) : 1f;
            float tenacityMultiplier = additionalStats.Any(x => x.t == StatType.Tenacity) ?
                EvaluateStat(StatType.Determination, additionalStats.Where(x => x.t == StatType.Tenacity).Single().sum, level, job, 1) : 1f;
            return baseDmg * determinationMultiplier * tenacityMultiplier * trait / 100f;
        }
        private static float GetHPMultiplier(int level, Job? job) => (job.GetRole(), level) switch
        {
            (Role.Tank, 90) => 34.6f,
            (Role.Tank, 80) => 26.6f,
            (Role.Tank, 70) => 18.8f,
            (_, 90) => 34.6f,
            (_, 80) => 34.6f,
            (_, 70) => 34.6f,
            _ => float.NaN
        };
        public static int GetStatWithModifiers(StatType type, int fromGear, int level, Job? job, Tribe? tribe)
        {
            return fromGear + (int)MathF.Round(GetBaseStat(type, level) * GetJobModifier(type, job.GetClassJob())) + GetRacialModifier(type, tribe);
        }
        public static int GetBaseStat(StatType type, int level)
        {
            if (level < 1 || level > 90)
                return 0;
            return type switch
            {
                StatType.HP => LevelTable[level].HP,
                StatType.MP => LevelTable[level].MP,
                StatType.Strength or StatType.Dexterity or StatType.Vitality or StatType.Intelligence or StatType.Mind or StatType.Determination or StatType.Piety => LevelTable[level].MAIN,
                StatType.Tenacity or StatType.DirectHitRate or StatType.CriticalHit or StatType.CriticalHitPower or StatType.SkillSpeed or StatType.SpellSpeed => LevelTable[level].SUB,
                _ => 0
            };
        }

        /// <summary>
        /// Calculates a multiplicative modifier based on the Job/class
        /// </summary>
        /// <param name="statType">Which stat is being queried</param>
        /// <param name="job">Current job to evaluate for</param>
        /// <returns>Multiplicative modifier to apply to stat</returns>
        public static float GetJobModifier(StatType statType, ClassJob? job)
        {
            if (job is null)
                return 1f;
            return statType switch
            {
                StatType.Strength => job.ModifierStrength,
                StatType.Dexterity => job.ModifierDexterity,
                StatType.Intelligence => job.ModifierIntelligence,
                StatType.Mind => job.ModifierMind,
                StatType.Vitality => job.ModifierVitality,
                StatType.HP => job.ModifierHitPoints,
                StatType.MP => job.ModifierManaPoints,
                StatType.Piety => job.ModifierPiety,
                _ => 100
            } / 100f;
        }
        /// <summary>
        /// Calculates a additive modifier based on the race and Tribe (clan)
        /// </summary>
        /// <param name="type">Which stat is being queried</param>
        /// <param name="t">The tribe (clan) to query</param>
        /// <returns>Additive modifier to apply to sta</returns>
        public static int GetRacialModifier(StatType type, Tribe? t)
        {
            if (t is null)
                return 0;
            return type switch
            {
                StatType.Strength => t.STR,
                StatType.Dexterity => t.DEX,
                StatType.Vitality => t.VIT,
                StatType.Intelligence => t.INT,
                StatType.Mind => t.MND,
                _ => 0
            };
        }
    }
}
