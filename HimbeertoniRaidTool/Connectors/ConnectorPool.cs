namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;

    internal ConnectorPool(TaskManager tm)
    {
        EtroConnector = new EtroConnector(tm);
        LodestoneConnector = new LodestoneConnector();
    }
}