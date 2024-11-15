using Dalamud.Plugin.Services;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool : IDisposable
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;
    internal readonly XivGearAppConnector XivGearAppConnector;

    internal ConnectorPool(TaskManager tm, ILogger log)
    {
        EtroConnector = new EtroConnector(tm, log);
        LodestoneConnector = new LodestoneConnector();
        XivGearAppConnector = new XivGearAppConnector(tm);
    }

    public IReadOnlyGearConnector GetConnector(GearSetManager type) => type switch
    {
        GearSetManager.Etro    => EtroConnector,
        GearSetManager.XivGear => XivGearAppConnector,
        GearSetManager.Hrt     => throw new NotImplementedException(),
        _                      => throw new ArgumentOutOfRangeException($"type {type} is not supported."),
    };

    public void Dispose() => LodestoneConnector.Dispose();
}