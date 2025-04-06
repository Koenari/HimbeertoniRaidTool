using Dalamud.Plugin;

namespace HimbeertoniRaidTool.Plugin;

// ReSharper disable once UnusedMember.Global
public sealed class HrtPlugin : IDalamudPlugin
{

    private readonly IGlobalServiceContainer _services;

    public HrtPlugin(IDalamudPluginInterface pluginInterface)
    {
        //Init all services
        _services = ServiceManager.Get(pluginInterface);
        _services.ModuleManager.LoadModules();
    }

    public void Dispose() => _services.Dispose();
}