using HimbeertoniRaidTool.Plugin.Connectors.Utils;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class XivGearAppConnector() : WebConnector(new RateLimit(5, TimeSpan.FromSeconds(10))), IReadOnlyGearConnector
{
    private const string API_BASE_URL = "https://api.xivgear.app/";

    public void RequestGearSetUpdate(GearSet set, Action<HrtUiMessage>? messageCallback = null,
                                     string taskName = "Gearset Update") => throw new NotImplementedException();
    public HrtUiMessage UpdateGearSet(GearSet set) => throw new NotImplementedException();
}