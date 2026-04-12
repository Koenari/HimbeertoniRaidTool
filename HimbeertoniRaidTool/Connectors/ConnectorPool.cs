using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Connectors;

public class ConnectorPool : IDisposable
{
    private readonly EtroConnector _etroConnector;
    private readonly LodestoneConnector _lodestoneConnector;
    private readonly XivGearAppConnector _xivGearAppConnector;

    internal ConnectorPool(HrtDataManager hrtDataManager, TaskManager tm, IDataManager dataManager, ILogger log,
                           ConfigurationManager configurationManager)
    {
        _etroConnector = new EtroConnector(hrtDataManager, tm, log, dataManager, configurationManager);
        _lodestoneConnector = new LodestoneConnector(hrtDataManager, dataManager, log);
        _xivGearAppConnector = new XivGearAppConnector(hrtDataManager, tm, log, configurationManager);
    }
    public bool TryGetConnector<TConnector>([NotNullWhen(true)] out TConnector? connector)
        where TConnector : class
    {
        switch (typeof(TConnector))
        {
            case var t when t == typeof(EtroConnector):
                connector = _etroConnector as TConnector;
                return connector != null;
            case var t when t == typeof(LodestoneConnector):
                connector = _lodestoneConnector as TConnector;
                return connector != null;
            case var t when t == typeof(XivGearAppConnector):
                connector = _xivGearAppConnector as TConnector;
                return connector != null;
            default:
                connector = null;
                return false;
        }
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
        _                      => null,
    };

    public ExternalBiSDefinition GetDefaultBiS(Job job) => _etroConnector.GetDefaultBiS(job);

    public void Dispose() => _lodestoneConnector.Dispose();
}