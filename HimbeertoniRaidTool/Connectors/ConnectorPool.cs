using Dalamud.Plugin.Services;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool : IDisposable
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;

    internal ConnectorPool(TaskManager tm, ILogger log)
    {
        EtroConnector = new EtroConnector(tm, log);
        LodestoneConnector = new LodestoneConnector();
    }

    public void Dispose() => LodestoneConnector.Dispose();
}