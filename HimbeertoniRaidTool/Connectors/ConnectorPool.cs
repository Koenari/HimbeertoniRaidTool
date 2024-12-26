using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool : IDisposable
{
    internal readonly EtroConnector EtroConnector;
    internal readonly LodestoneConnector LodestoneConnector;
    internal readonly XivGearAppConnector XivGearAppConnector;

    internal ConnectorPool(HrtDataManager hrtDataManager, TaskManager tm, IDataManager dataManager, ILogger log)
    {
        EtroConnector = new EtroConnector(hrtDataManager, tm, log);
        LodestoneConnector = new LodestoneConnector(hrtDataManager, dataManager, log);
        XivGearAppConnector = new XivGearAppConnector(hrtDataManager, tm, log);
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