using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Data
{
    public class InstanceWithLoot
    {
        private static readonly ExcelSheet<InstanceContent> InstanceSheet = Services.DataManager.GetExcelSheet<InstanceContent>()!;
        public InstanceType InstanceType => (InstanceType)(InstanceSheet.GetRow(InstanceID)?.InstanceContentType ?? 0);
        public EncounterDifficulty Difficulty { get; }
        public string Name { get; }
        public IEnumerable<HrtItem> PossibleItems { get; }
        public IEnumerable<HrtItem> GuaranteedItems { get; }
        public uint InstanceID { get; }
        public IEnumerable<HrtItem> AllLoot
        {
            get
            {
                foreach (var item in PossibleItems)
                    yield return item;
                foreach (var item in GuaranteedItems)
                    yield return item;
            }
        }
        public InstanceWithLoot(uint id, string name, EncounterDifficulty difficulty = EncounterDifficulty.Normal, ItemIDCollection? possibleLoot = null, ItemIDCollection? guaranteedLoot = null)
        {
            InstanceID = id;
            Difficulty = difficulty;
            Name = name;
            GuaranteedItems = (guaranteedLoot ?? ItemIDCollection.Empty).Select((id, i) => new HrtItem(id));
            PossibleItems = (possibleLoot ?? ItemIDCollection.Empty).Select((id, i) => new HrtItem(id));
        }
    }
}
