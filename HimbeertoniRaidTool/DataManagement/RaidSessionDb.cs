using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class RaidSessionDb(IIdProvider idProvider, IEnumerable<JsonConverter> converters, ILogger logger)
    : DataBaseTable<RaidSession>(idProvider, converters, logger)
{

    public override HrtWindow GetSearchWindow(IUiSystem uiSystem, Action<RaidSession> onSelect, Action? onCancel = null)
        => new RaidSessionSearchWindow(uiSystem, this, onSelect, onCancel);

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> result = new();
        foreach (var raidSession in Data.Values)
        {
            if (raidSession.Group is not null) result.Add(raidSession.Group.LocalId);
            foreach (var participant in raidSession.Participants)
            {
                result.Add(participant.Player.LocalId);
            }
        }
        return result;
    }

    private class RaidSessionSearchWindow(
        IUiSystem uiSystem,
        RaidSessionDb db,
        Action<RaidSession> onSelect,
        Action? onCancel)
        : SearchWindow<RaidSession, RaidSessionDb>(uiSystem, db, onSelect, onCancel)
    {

        protected override void DrawContent() => throw new NotImplementedException();
    }
}