using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    public static class AllaganLibrary
    {
        public static void Init()
        {
            AllaganLibraryData.Init();
        }
        /// <summary>
        /// This function evaluates a stat to it's respective effective used effect.
        /// Only works for level 90
        /// </summary>
        /// <param name="type">Type of stat that is input</param>
        /// <param name="totalStat">total value of stat</param>
        /// <param name="level">level of the job to evaluate for</param>
        /// <param name="job">current job</param>
        /// <param name="alternative">a way to use alternative formulas for stats that have multiple effects (0 is default furmula)</param>
        /// <returns>Evaluated value including unit</returns>
        public static string EvaluateStatToDisplay(StatType type, int totalStat, int level, Job job, int alternative = 0)
        {
            string notAvail = "n.A.";
            float evaluatedValue = EvaluateStat(type, totalStat, level, job, alternative);
            if (float.IsNaN(evaluatedValue))
                return notAvail;
            if (type == StatType.CriticalHit && alternative == 1)
                type = StatType.CriticalHitPower;
            return type switch
            {
                StatType.CriticalHit => $"{evaluatedValue * 100:N1} %%",
                StatType.CriticalHitPower => $"{evaluatedValue * 100:N1} %%",
                StatType.DirectHitRate => $"{evaluatedValue * 100:N1} %%",
                StatType.Determination => $"{100 + evaluatedValue * 100:N1} %%",
                StatType.Tenacity => $"{evaluatedValue * 100:N1} %%",
                StatType.Piety => $"+{evaluatedValue:N0} MP/s",
                StatType.SkillSpeed or StatType.SpellSpeed =>
                     alternative switch
                     {
                         //GCD
                         0 => $"{evaluatedValue:N2} s",
                         //AA/DoT Multiplier
                         1 => $"{evaluatedValue * 100:N1} %%",
                         _ => notAvail
                     },
                StatType.Defense or StatType.MagicDefense => $"{evaluatedValue * 100:N1} %%",
                StatType.Vitality => $"{evaluatedValue:N0} HP",
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
        /// <param name="alternative">a way to use alternative formulas for stats that have multiple effects (0 is default furmula)</param>
        /// <returns>Evaluated value (percentage values are in mathematical correct value, means 100% = 1.0)</returns>
        public static float EvaluateStat(StatType type, int totalStat, int level, Job job, int alternative = 0)
        {
            if (level < 90)
                return float.NaN;
            if (type == StatType.CriticalHit && alternative == 1)
                type = StatType.CriticalHitPower;
            return type switch
            {
                StatType.CriticalHit => MathF.Floor(200 * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "SUB")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV") + 50) / 1000f,
                StatType.CriticalHitPower => MathF.Floor(200 * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "SUB")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV") + 1400) / 1000f,
                StatType.DirectHitRate => MathF.Floor(550 * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "SUB")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV")) / 1000f,
                StatType.Determination => MathF.Floor(140 * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "MAIN")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV")) / 1000f,
                StatType.Tenacity =>
                    alternative switch
                    {
                        //Inc Dmg Mitigation
                        0 => (1000f - MathF.Floor(100 * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "SUB")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV"))) / 1000f,
                        //Out DMG%Heal
                        1 => (1000f + MathF.Floor(100 * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "SUB")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV"))) / 1000f,
                        _ => float.NaN,
                    },
                StatType.Piety => MathF.Floor(150 * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "MAIN")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV")) + 200f,
                StatType.SkillSpeed or StatType.SpellSpeed =>
                     alternative switch
                     {
                         //GCD
                         0 => MathF.Floor(2500f * (1000 + MathF.Ceiling(130 * (GetTableData<int>(AllaganTables.Level, $"LV = {level}", "SUB") - totalStat) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV"))) / 10000f) / 100f,
                         //AA/DoT Multiplier
                         1 => (1000f + MathF.Ceiling(130f * (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "SUB")) / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV"))) / 1000f,
                         _ => float.NaN
                     },
                StatType.Defense or StatType.MagicDefense => MathF.Floor(15 * totalStat / GetTableData<int>(AllaganTables.Level, $"LV = {level}", "DIV")) / 100f,
                StatType.Vitality => MathF.Floor(GetTableData<int>(AllaganTables.Level, $"LV = {level}", "HP") * GetTableData<int>(AllaganTables.Job, $"JOB = '{job}'", "HP") / 100f)
                    + (totalStat - GetTableData<int>(AllaganTables.Level, $"LV = {level}", "Main")) *
                    job.GetRole() switch
                    {
                        Role.Tank => 34.6f,
                        _ => 24.3f,
                    },
                _ => float.NaN
            };
        }
        public static int GetStatWithModifiers(StatType type, int fromGear, int level, Job job, string race, string clan)
        {
            return fromGear + (int)(GetBaseStat(type, level) * GetJobModifier(type, job)) + GetRacialModifier(type, race, clan);
        }
        public static int GetBaseStat(StatType type, int level)
        {
            string levelCol = type switch
            {
                StatType.HP => "HP",
                StatType.MP => "MP",
                StatType.Strength or StatType.Dexterity or StatType.Vitality or StatType.Intelligence or StatType.Mind or StatType.Determination or StatType.Piety => "MAIN",
                StatType.Tenacity or StatType.DirectHitRate or StatType.CriticalHit or StatType.CriticalHitPower or StatType.SkillSpeed or StatType.SpellSpeed => "SUB",
                _ => ""
            };

            return levelCol.Equals("") || level < 1 ? 0 : (int)MathF.Floor(GetTableData<int>(AllaganTables.Level, $"LV = {level}", levelCol));
        }


        public static float GetJobModifier(StatType statType, Job job)
        {
            string jobCol = statType switch
            {
                StatType.Strength => "STR",
                StatType.Dexterity => "DEX",
                StatType.Intelligence => "INT",
                StatType.Mind => "MND",
                StatType.Vitality => "VIT",
                StatType.HP => "HP",
                StatType.MP => "MP",
                _ => ""
            };
            return jobCol.Equals("") ? 1 :
                GetTableData<int>(AllaganTables.Job, $"JOB = '{job}'", jobCol) / 100f;
        }
        public static int GetRacialModifier(StatType type, string race, string clan)
        {
            string rowFilter = $"Clan = '{clan}' AND Race = '{race}'";
            try
            {
                return type switch
                {
                    StatType.Strength => GetTableData<int>(AllaganTables.Racial, rowFilter, "STR"),
                    StatType.Dexterity => GetTableData<int>(AllaganTables.Racial, rowFilter, "DEX"),
                    StatType.Vitality => GetTableData<int>(AllaganTables.Racial, rowFilter, "VIT"),
                    StatType.Intelligence => GetTableData<int>(AllaganTables.Racial, rowFilter, "INT"),
                    StatType.Mind => GetTableData<int>(AllaganTables.Racial, rowFilter, "MND"),
                    _ => 0
                };
            }
            catch { return 0; }
        }
        private static bool IsMainStat(StatType statType)
        {
            StatType[] mainStats = new StatType[] { StatType.Strength, StatType.Dexterity, StatType.Mind, StatType.Intelligence };
            return mainStats.Contains(statType);
        }
        private static T GetTableData<T>(AllaganTables table, string whereClause, string col)
        {
            DataTable dataTable = table switch
            {
                AllaganTables.Level => AllaganLibraryData.Level,
                AllaganTables.Job => AllaganLibraryData.Job,
                AllaganTables.Racial => AllaganLibraryData.Racial,
                _ => throw new NotImplementedException(),
            };
            return (T)dataTable.Select($"{whereClause}")[0][col];
        }
        public static void Dispose()
        {
            AllaganLibraryData.Dispose();
        }
        private static class AllaganLibraryData
        {
            private static bool _initialized = false;
            internal static DataTable Level = new DataTable("Level", "AllaganLibrary");
            internal static DataTable Job = new DataTable("Job", "AllaganLibrary");
            internal static DataTable Racial = new DataTable("Racial", "AllaganLibrary");

            internal static void Init()
            {
                if (_initialized) return;
                {
                    Level.Columns.Add("LV", typeof(int));
                    Level.Columns.Add("MP", typeof(int));
                    Level.Columns.Add("MAIN", typeof(int));
                    Level.Columns.Add("SUB", typeof(int));
                    Level.Columns.Add("DIV", typeof(int));
                    Level.Columns.Add("HP", typeof(int));
                    Level.Columns.Add("ELMT", typeof(int));
                    Level.Columns.Add("THREAT", typeof(int));
                    Level.Rows.Add(1, 10000, 20, 56, 56, 86, 52, 2);
                    Level.Rows.Add(2, 10000, 21, 57, 57, 101, 54, 2);
                    Level.Rows.Add(3, 10000, 22, 60, 60, 109, 56, 3);
                    Level.Rows.Add(4, 10000, 24, 62, 62, 116, 58, 3);
                    Level.Rows.Add(5, 10000, 26, 65, 65, 123, 60, 3);
                    Level.Rows.Add(6, 10000, 27, 68, 68, 131, 62, 3);
                    Level.Rows.Add(7, 10000, 29, 70, 70, 138, 64, 4);
                    Level.Rows.Add(8, 10000, 31, 73, 73, 145, 66, 4);
                    Level.Rows.Add(9, 10000, 33, 76, 76, 153, 68, 4);
                    Level.Rows.Add(10, 10000, 35, 78, 78, 160, 70, 5);
                    Level.Rows.Add(11, 10000, 36, 82, 82, 174, 73, 5);
                    Level.Rows.Add(12, 10000, 38, 85, 85, 188, 75, 5);
                    Level.Rows.Add(13, 10000, 41, 89, 89, 202, 78, 6);
                    Level.Rows.Add(14, 10000, 44, 93, 93, 216, 81, 6);
                    Level.Rows.Add(15, 10000, 46, 96, 96, 230, 84, 7);
                    Level.Rows.Add(16, 10000, 49, 100, 100, 244, 86, 7);
                    Level.Rows.Add(17, 10000, 52, 104, 104, 258, 89, 8);
                    Level.Rows.Add(18, 10000, 54, 109, 109, 272, 93, 9);
                    Level.Rows.Add(19, 10000, 57, 113, 113, 286, 95, 9);
                    Level.Rows.Add(20, 10000, 60, 116, 116, 300, 98, 10);
                    Level.Rows.Add(21, 10000, 63, 122, 122, 333, 102, 10);
                    Level.Rows.Add(22, 10000, 67, 127, 127, 366, 105, 11);
                    Level.Rows.Add(23, 10000, 71, 133, 133, 399, 109, 12);
                    Level.Rows.Add(24, 10000, 74, 138, 138, 432, 113, 13);
                    Level.Rows.Add(25, 10000, 78, 144, 144, 465, 117, 14);
                    Level.Rows.Add(26, 10000, 81, 150, 150, 498, 121, 15);
                    Level.Rows.Add(27, 10000, 85, 155, 155, 531, 125, 16);
                    Level.Rows.Add(28, 10000, 89, 162, 162, 564, 129, 17);
                    Level.Rows.Add(29, 10000, 92, 168, 168, 597, 133, 18);
                    Level.Rows.Add(30, 10000, 97, 173, 173, 630, 137, 19);
                    Level.Rows.Add(31, 10000, 101, 181, 181, 669, 143, 20);
                    Level.Rows.Add(32, 10000, 106, 188, 188, 708, 148, 22);
                    Level.Rows.Add(33, 10000, 110, 194, 194, 747, 153, 23);
                    Level.Rows.Add(34, 10000, 115, 202, 202, 786, 159, 25);
                    Level.Rows.Add(35, 10000, 119, 209, 209, 825, 165, 27);
                    Level.Rows.Add(36, 10000, 124, 215, 215, 864, 170, 29);
                    Level.Rows.Add(37, 10000, 128, 223, 223, 903, 176, 31);
                    Level.Rows.Add(38, 10000, 134, 229, 229, 942, 181, 33);
                    Level.Rows.Add(39, 10000, 139, 236, 236, 981, 186, 35);
                    Level.Rows.Add(40, 10000, 144, 244, 244, 1020, 192, 38);
                    Level.Rows.Add(41, 10000, 150, 253, 253, 1088, 200, 40);
                    Level.Rows.Add(42, 10000, 155, 263, 263, 1156, 207, 43);
                    Level.Rows.Add(43, 10000, 161, 272, 272, 1224, 215, 46);
                    Level.Rows.Add(44, 10000, 166, 283, 283, 1292, 223, 49);
                    Level.Rows.Add(45, 10000, 171, 292, 292, 1360, 231, 52);
                    Level.Rows.Add(46, 10000, 177, 302, 302, 1428, 238, 55);
                    Level.Rows.Add(47, 10000, 183, 311, 311, 1496, 246, 58);
                    Level.Rows.Add(48, 10000, 189, 322, 322, 1564, 254, 62);
                    Level.Rows.Add(49, 10000, 196, 331, 331, 1632, 261, 66);
                    Level.Rows.Add(50, 10000, 202, 341, 341, 1700, 269, 70);
                    Level.Rows.Add(51, 10000, 204, 342, 366, 0, 0, 0);
                    Level.Rows.Add(52, 10000, 205, 344, 392, 0, 0, 0);
                    Level.Rows.Add(53, 10000, 207, 345, 418, 0, 0, 0);
                    Level.Rows.Add(54, 10000, 209, 346, 444, 0, 0, 0);
                    Level.Rows.Add(55, 10000, 210, 347, 470, 0, 0, 0);
                    Level.Rows.Add(56, 10000, 212, 349, 496, 0, 0, 0);
                    Level.Rows.Add(57, 10000, 214, 350, 522, 0, 0, 0);
                    Level.Rows.Add(58, 10000, 215, 351, 548, 0, 0, 0);
                    Level.Rows.Add(59, 10000, 217, 352, 574, 0, 0, 0);
                    Level.Rows.Add(60, 10000, 218, 354, 600, 0, 0, 0);
                    Level.Rows.Add(61, 10000, 224, 355, 630, 0, 0, 0);
                    Level.Rows.Add(62, 10000, 228, 356, 660, 0, 0, 0);
                    Level.Rows.Add(63, 10000, 236, 357, 690, 0, 0, 0);
                    Level.Rows.Add(64, 10000, 244, 358, 720, 0, 0, 0);
                    Level.Rows.Add(65, 10000, 252, 359, 750, 0, 0, 0);
                    Level.Rows.Add(66, 10000, 260, 360, 780, 0, 0, 0);
                    Level.Rows.Add(67, 10000, 268, 361, 810, 0, 0, 0);
                    Level.Rows.Add(68, 10000, 276, 362, 840, 0, 0, 0);
                    Level.Rows.Add(69, 10000, 284, 363, 870, 0, 0, 0);
                    Level.Rows.Add(70, 10000, 292, 364, 900, 0, 0, 0);
                    Level.Rows.Add(71, 10000, 296, 365, 940, 0, 0, 0);
                    Level.Rows.Add(72, 10000, 300, 366, 980, 0, 0, 0);
                    Level.Rows.Add(73, 10000, 305, 367, 1020, 0, 0, 0);
                    Level.Rows.Add(74, 10000, 310, 368, 1060, 0, 0, 0);
                    Level.Rows.Add(75, 10000, 315, 370, 1100, 0, 0, 0);
                    Level.Rows.Add(76, 10000, 320, 372, 1140, 0, 0, 0);
                    Level.Rows.Add(77, 10000, 325, 374, 1180, 0, 0, 0);
                    Level.Rows.Add(78, 10000, 330, 376, 1220, 0, 0, 0);
                    Level.Rows.Add(79, 10000, 335, 378, 1260, 0, 0, 0);
                    Level.Rows.Add(80, 10000, 340, 380, 1300, 0, 0, 0);
                    Level.Rows.Add(81, 10000, 345, 382, 1360, 0, 0, 0);
                    Level.Rows.Add(82, 10000, 350, 384, 1420, 0, 0, 0);
                    Level.Rows.Add(83, 10000, 355, 386, 1480, 0, 0, 0);
                    Level.Rows.Add(84, 10000, 360, 388, 1540, 0, 0, 0);
                    Level.Rows.Add(85, 10000, 365, 390, 1600, 0, 0, 0);
                    Level.Rows.Add(86, 10000, 370, 392, 1660, 0, 0, 0);
                    Level.Rows.Add(87, 10000, 375, 394, 1720, 0, 0, 0);
                    Level.Rows.Add(88, 10000, 380, 396, 1780, 0, 0, 0);
                    Level.Rows.Add(89, 10000, 385, 398, 1840, 0, 0, 0);
                    Level.Rows.Add(90, 10000, 390, 400, 1900, 3000, 0, 0);
                    Level.AcceptChanges();
                }
                {
                    Job.Columns.Add("JOBID", typeof(int));
                    Job.Columns.Add("JOB", typeof(string));
                    Job.Columns.Add("HP", typeof(int));
                    Job.Columns.Add("MP", typeof(int));
                    Job.Columns.Add("STR", typeof(int));
                    Job.Columns.Add("VIT", typeof(int));
                    Job.Columns.Add("DEX", typeof(int));
                    Job.Columns.Add("INT", typeof(int));
                    Job.Columns.Add("MND", typeof(int));
                    Job.Rows.Add(1, "GLA", 130, 100, 95, 100, 90, 50, 95);
                    Job.Rows.Add(2, "PGL", 105, 100, 100, 95, 100, 45, 85);
                    Job.Rows.Add(3, "MRD", 135, 100, 100, 100, 90, 30, 50);
                    Job.Rows.Add(4, "LNC", 110, 100, 105, 100, 95, 40, 60);
                    Job.Rows.Add(5, "ARC", 100, 100, 85, 95, 105, 80, 75);
                    Job.Rows.Add(6, "CNJ", 100, 100, 50, 95, 100, 100, 105);
                    Job.Rows.Add(7, "THM", 100, 100, 40, 95, 95, 105, 70);
                    Job.Rows.Add(19, "PLD", 140, 100, 100, 110, 95, 60, 100);
                    Job.Rows.Add(20, "MNK", 110, 100, 110, 100, 105, 50, 90);
                    Job.Rows.Add(21, "WAR", 145, 100, 105, 110, 95, 40, 55);
                    Job.Rows.Add(22, "DRG", 115, 100, 115, 105, 100, 45, 65);
                    Job.Rows.Add(23, "BRD", 105, 100, 90, 100, 115, 85, 80);
                    Job.Rows.Add(24, "WHM", 105, 100, 55, 100, 105, 105, 115);
                    Job.Rows.Add(25, "BLM", 105, 100, 45, 100, 100, 115, 75);
                    Job.Rows.Add(26, "ACN", 100, 100, 85, 95, 95, 105, 75);
                    Job.Rows.Add(27, "SMN", 105, 100, 90, 100, 100, 115, 80);
                    Job.Rows.Add(28, "SCH", 105, 100, 90, 100, 100, 105, 115);
                    Job.Rows.Add(29, "ROG", 103, 100, 80, 95, 100, 60, 70);
                    Job.Rows.Add(30, "NIN", 108, 100, 85, 100, 110, 65, 75);
                    Job.Rows.Add(31, "MCH", 105, 100, 85, 100, 115, 80, 85);
                    Job.Rows.Add(32, "DRK", 140, 100, 105, 110, 95, 60, 40);
                    Job.Rows.Add(33, "AST", 105, 100, 50, 100, 100, 105, 115);
                    Job.Rows.Add(34, "SAM", 109, 100, 112, 100, 108, 60, 50);
                    Job.Rows.Add(35, "RDM", 105, 100, 55, 100, 105, 115, 110);
                    Job.Rows.Add(36, "BLU", 105, 100, 70, 100, 110, 115, 105);
                    Job.Rows.Add(37, "GNB", 120, 100, 100, 110, 95, 60, 100);
                    Job.Rows.Add(38, "DNC", 105, 100, 90, 100, 115, 85, 80);
                    Job.Rows.Add(39, "RPR", 115, 100, 115, 105, 100, 80, 40);
                    Job.Rows.Add(40, "SGE", 105, 100, 60, 100, 100, 115, 115);
                    Job.AcceptChanges();

                }
                {
                    Racial.Columns.Add("Clan", typeof(string));
                    Racial.Columns.Add("Race", typeof(string));
                    Racial.Columns.Add("STR", typeof(int));
                    Racial.Columns.Add("DEX", typeof(int));
                    Racial.Columns.Add("VIT", typeof(int));
                    Racial.Columns.Add("INT", typeof(int));
                    Racial.Columns.Add("MND", typeof(int));
                    Racial.Rows.Add("Dunesfolk", "Lalafell", -1, 1, -2, 2, 3);
                    Racial.Rows.Add("Duskwight", "Elezen", 0, 0, -1, 3, 1);
                    Racial.Rows.Add("Helion", "Hrothgar", 3, -3, 3, -3, 3);
                    Racial.Rows.Add("Hellsguard", "Roegadyn", 0, -2, 2, 0, 2);
                    Racial.Rows.Add("Highlander", "Hyur", 3, 0, 2, -2, 0);
                    Racial.Rows.Add("Midlander", "Hyur", 2, -1, 0, 3, -1);
                    Racial.Rows.Add("Moon", "Miqote", -1, 2, -2, 1, 3);
                    Racial.Rows.Add("Plainsfolk", "Lalafell", -1, 3, -1, 2, 0);
                    Racial.Rows.Add("Raen", "Au Ra", -1, 2, -1, 0, 3);
                    Racial.Rows.Add("Rava", "Viera", 0, 3, -2, 1, 1);
                    Racial.Rows.Add("Sea Wolves", "Roegadyn", 2, -1, 3, -2, 1);
                    Racial.Rows.Add("Sun", "Miqote", 2, 3, 0, -1, -1);
                    Racial.Rows.Add("The Lost", "Hrothgar", 3, -3, 3, -3, 3);
                    Racial.Rows.Add("Veena", "Viera", -1, 0, -1, 3, 2);
                    Racial.Rows.Add("Wildwood", "Elezen", 0, 3, -1, 2, -1);
                    Racial.Rows.Add("Xaela", "Au Ra", 3, 0, 2, 0, -2);
                    Racial.AcceptChanges();


                }
                _initialized = true;
            }
            internal static void Dispose()
            {
                if (!_initialized) return;
                Level.Clear();
                Level.Dispose();
                Job.Clear();
                Job.Dispose();
                Racial.Clear();
                Racial.Dispose();
                Level = new DataTable("Level", "AllaganLibrary");
                Job = new DataTable("Job", "AllaganLibrary");
                Racial = new DataTable("Racial", "AllaganLibrary");
                _initialized = false;
            }

        }
    }
    public enum AllaganTables
    {
        Level,
        Job,
        Racial
    }

}
