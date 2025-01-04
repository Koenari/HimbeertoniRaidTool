using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class ConnectorPool : IDisposable
{
    private readonly EtroConnector _etroConnector;
    internal readonly LodestoneConnector LodestoneConnector;
    private readonly XivGearAppConnector _xivGearAppConnector;

    internal ConnectorPool(HrtDataManager hrtDataManager, TaskManager tm, IDataManager dataManager, ILogger log)
    {
        _etroConnector = new EtroConnector(hrtDataManager, tm, log, dataManager);
        LodestoneConnector = new LodestoneConnector(hrtDataManager, dataManager, log);
        _xivGearAppConnector = new XivGearAppConnector(hrtDataManager, tm, log);
    }

    public bool TryGetConnector(GearSetManager type, [NotNullWhen(true)] out IReadOnlyGearConnector? connector)
    {
        connector = GetConnectorInternal(type);
        return connector != null;
    }

    public bool HasConnector(GearSetManager type) => TryGetConnector(type, out _);
    private IReadOnlyGearConnector? GetConnectorInternal(GearSetManager type) => type switch
    {
        GearSetManager.Etro    => _etroConnector,
        GearSetManager.XivGear => _xivGearAppConnector,
        GearSetManager.Hrt     => null,
        GearSetManager.Unknown => null,
        _                      => null,
    };

    public ExternalBiSDefinition GetDefaultBiS(Job job) => _etroConnector.GetDefaultBiS(job);

    public void Dispose() => LodestoneConnector.Dispose();
}