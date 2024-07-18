using Dalamud.Plugin.Services;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool : IDisposable
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;
    internal readonly IReadOnlyGearConnector XivGearAppConnector;

    internal ConnectorPool(TaskManager tm, ILogger log)
    {
        EtroConnector = new EtroConnector(tm, log);
        LodestoneConnector = new LodestoneConnector();
        XivGearAppConnector = new XivGearAppConnector();
    }

    public IReadOnlyGearConnector GetConnector(GearSetManager type) => type switch
    {
        GearSetManager.Etro       => EtroConnector,
        GearSetManager.XivGearApp => XivGearAppConnector,
        _                         => throw new NotImplementedException(),
    };

    public void Dispose() => LodestoneConnector.Dispose();
}