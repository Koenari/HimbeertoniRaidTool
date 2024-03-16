using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class RaidSessionDb(IIdProvider idProvider, IEnumerable<JsonConverter> converters)
    : DataBaseTable<RaidSession>(idProvider, converters)
{

    public override HrtWindow OpenSearchWindow(Action<RaidSession> onSelect, Action? onCancel = null)
        => new RaidSessionSearchWindow(this, onSelect, onCancel);

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> result = new();
        foreach (RaidSession raidSession in Data.Values)
        {
            if (raidSession.Group is not null) result.Add(raidSession.Group.LocalId);
            foreach (Participant participant in raidSession.Participants)
            {
                result.Add(participant.Player.LocalId);
            }
        }
        return result;
    }

    private class RaidSessionSearchWindow(RaidSessionDb db, Action<RaidSession> onSelect, Action? onCancel)
        : SearchWindow<RaidSession, RaidSessionDb>(db, onSelect, onCancel)
    {

        protected override void DrawContent() => throw new NotImplementedException();
    }
}