using System.Collections.ObjectModel;
using System.Linq;

namespace HimbeertoniRaidTool.Data
{
    public class InstanceWithLoot
    {
        public string Name { get; set; }
        public ReadOnlyCollection<HrtItem> PossibleItems { get; }
        public ReadOnlyCollection<HrtItem> GuaranteedItems { get; }
        public readonly int InstanceID;

        public InstanceWithLoot(int id, string name, ItemIDCollection guaranteedLoot, ItemIDCollection possibleLoot)
        {
            InstanceID = id;
            Name = name;
            GuaranteedItems = new(guaranteedLoot.Select((id, i) => new HrtItem(id)).ToList());
            PossibleItems = new(possibleLoot.Select((id, i) => new HrtItem(id)).ToList());
        }
    }
}
