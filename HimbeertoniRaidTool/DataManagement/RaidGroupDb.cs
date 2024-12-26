using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class RaidGroupDb(IIdProvider idProvider, IEnumerable<JsonConverter> converters, ILogger logger)
    : DataBaseTable<RaidGroup>(idProvider, converters, logger)
{

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> referencedIds = new();
        foreach (var p in from g in Data.Values from player in g select player)
        {
            referencedIds.Add(p.LocalId);
        }
        return referencedIds;
    }
    public override HrtWindow OpenSearchWindow(Action<RaidGroup> onSelect, Action? onCancel = null) =>
        new GroupSearchWindow(this, onSelect, onCancel);

    internal class GroupSearchWindow(RaidGroupDb dataBase, Action<RaidGroup> onSelect, Action? onCancel)
        : SearchWindow<RaidGroup, RaidGroupDb>(dataBase,
                                               onSelect, onCancel)
    {

        protected override void DrawContent() => throw new NotImplementedException();
    }
}