using HimbeertoniRaidTool.Data;
using System;
namespace HimbeertoniRaidTool.LootMaster
{
    /// <summary>
    /// This is a really dirty way to have stats displayed until a final solution for using AllaganStudies Formulas and tables is decided and implemted
    /// This is used temporarily. Else this branch would need to be abandoned for an unknown amount of time
    /// </summary>
    internal static class AllaganLibraryMock
    {
        internal enum Table
        {
            Job,
            Level,
            Enmity,
            Racial,
            Deity
        }
        private enum Col
        {
            MP,
            Main,
            Sub,
            Div,
            HP
        }

        internal static int GetBaseStatAt90(StatType type)
        {
            return type switch
            {
                StatType.MP => GetTableDataInt(Table.Level, 90, Col.MP),
                StatType.HP => GetTableDataInt(Table.Level, 90, Col.HP),
                StatType.Vitality => GetTableDataInt(Table.Level, 90, Col.Main),
                StatType.Dexterity => GetTableDataInt(Table.Level, 90, Col.Main),
                StatType.Strength => GetTableDataInt(Table.Level, 90, Col.Main),
                StatType.Mind => GetTableDataInt(Table.Level, 90, Col.Main),
                StatType.Intelligence => GetTableDataInt(Table.Level, 90, Col.Main),
                StatType.CriticalHit => GetTableDataInt(Table.Level, 90, Col.Sub),
                StatType.Determination => GetTableDataInt(Table.Level, 90, Col.Sub),
                StatType.DirectHitRate => GetTableDataInt(Table.Level, 90, Col.Sub),
                StatType.SpellSpeed => GetTableDataInt(Table.Level, 90, Col.Sub),
                StatType.Piety => GetTableDataInt(Table.Level, 90, Col.Sub),
                StatType.SkillSpeed => GetTableDataInt(Table.Level, 90, Col.Sub),
                StatType.Tenacity => GetTableDataInt(Table.Level, 90, Col.Sub),
                _ => 0,
            };
        }
        private static int GetTableDataInt(Table t, int row, Col col)
        {
            switch (t)
            {
                case Table.Level:
                    return col switch
                    {
                        Col.MP => 10000,
                        Col.Main => 390,
                        Col.Sub => 400,
                        Col.Div => 1900,
                        Col.HP => 3000,
                        _ => 0
                    };

                default:
                    return 0;
            }
        }
        internal static float EvaluateStat(StatType outType, int value)
        {
            return outType switch
            {
                StatType.CriticalHit => MathF.Floor(200 * (value - GetTableDataInt(Table.Level, 90, Col.Sub)) / GetTableDataInt(Table.Level, 90, Col.Div) + 50) / 10f,
                _ => float.NaN
            };
        }
    }
}
