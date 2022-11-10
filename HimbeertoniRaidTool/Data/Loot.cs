using System.Collections.ObjectModel;
using System.Linq;

namespace HimbeertoniRaidTool.Data
{
    public class InstanceWithLoot
    {
        public ContentType InstanceType;
        public string Name { get; set; }
        public ReadOnlyCollection<HrtItem> PossibleItems { get; }
        public ReadOnlyCollection<HrtItem> GuaranteedItems { get; }
        public readonly int InstanceID;

        public InstanceWithLoot(int id, ContentType type, string name, ItemIDCollection guaranteedLoot, ItemIDCollection possibleLoot)
        {
            InstanceID = id;
            InstanceType = type;
            Name = name;
            GuaranteedItems = new(guaranteedLoot.Select((id, i) => new HrtItem(id)).ToList());
            PossibleItems = new(possibleLoot.Select((id, i) => new HrtItem(id)).ToList());
        }
    }
}
