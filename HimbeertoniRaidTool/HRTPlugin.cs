using Dalamud.Plugin;

namespace HimbeertoniRaidTool.Plugin;

// ReSharper disable once UnusedMember.Global
public sealed class HrtPlugin : IDalamudPlugin
{

    private readonly GlobalServiceContainer _serviceContainer;

    public HrtPlugin(IDalamudPluginInterface pluginInterface)
    {
        //Init all services
        _serviceContainer = new GlobalServiceContainer(pluginInterface);
        _serviceContainer.ModuleManager.LoadModules();
    }

    public void Dispose() => _serviceContainer.Dispose();
}