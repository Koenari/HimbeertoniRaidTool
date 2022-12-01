using Dalamud.Game;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;

    internal ConnectorPool(Framework fw)
    {
        EtroConnector = new(fw);
        LodestoneConnector = new(fw);
    }
}
