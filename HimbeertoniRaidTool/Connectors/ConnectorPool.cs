using Dalamud.Plugin.Services;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;

    internal ConnectorPool(TaskManager tm, IPluginLog log)
    {
        EtroConnector = new EtroConnector(tm,log);
        LodestoneConnector = new LodestoneConnector();
    }
}
