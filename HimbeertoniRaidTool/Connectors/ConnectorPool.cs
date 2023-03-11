namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;

    internal ConnectorPool()
    {
        EtroConnector = new();
        LodestoneConnector = new();
    }
}
