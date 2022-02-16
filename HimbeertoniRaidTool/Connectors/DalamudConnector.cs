using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Connectors
{
    public static class DalamudConnector
    {
        private static ExcelSheet<Item>? Sheet => Services.Data.Excel.GetSheet<Item>();
        static DalamudConnector() { }

        public static bool GetGearStats(GearItem toFill)
        {
            if (toFill.ID < 1)
                return false;
            if (Sheet == null)
                return false;
            Item? newItem = Sheet.GetRow((uint)toFill.ID);
            if (newItem == null)
                return false;

            toFill.Name = newItem.Name;
            toFill.Description = newItem.Description;
            toFill.ItemLevel = (int) newItem.LevelItem.Row;
            return true;
        }
    }
}
