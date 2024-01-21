using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules.Core;

#pragma warning disable CS8618
namespace HimbeertoniRaidTool.Plugin.Services;

internal class ServiceManager
{
    private static bool _initialized = false;
    [PluginService] public static IChatGui ChatGui { get; private set; }
    [PluginService] public static IDataManager DataManager { get; private set; }
    [PluginService] public static ITargetManager TargetManager { get; private set; }
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; }
    [PluginService] public static IClientState ClientState { get; private set; }
    [PluginService] public static IObjectTable ObjectTable { get; private set; }
    [PluginService] public static IPartyList PartyList { get; private set; }
    [PluginService] public static ICondition Condition { get; private set; }
    [PluginService] public static IPluginLog PluginLog { get; private set; }
    [PluginService] private static IGameInteropProvider GameInteropProvider { get; set; }
    [PluginService] private static ITextureProvider TextureProvider { get; set; }
    [PluginService] private static IFramework Framework { get; set; }
    public static IconCache IconCache { get; private set; }
    public static HrtDataManager HrtDataManager { get; private set; }
    internal static TaskManager TaskManager { get; private set; }
    internal static ConnectorPool ConnectorPool { get; private set; }
    internal static Configuration Config { get; set; }
    internal static CoreModule CoreModule { get; set; }
    internal static CharacterInfoService CharacterInfoService { get; private set; }
    internal static ItemInfo ItemInfo => Common.Services.ServiceManager.ItemInfo;

    internal static bool Init(DalamudPluginInterface pluginInterface)
    {
        if (_initialized) return false;
        _initialized = true;
        pluginInterface.Create<ServiceManager>();
        Common.Services.ServiceManager.Init(DataManager.Excel);
        IconCache = new IconCache(PluginInterface, DataManager, TextureProvider);
        HrtDataManager = new HrtDataManager(PluginInterface);
        TaskManager = new TaskManager();
        ConnectorPool = new ConnectorPool(TaskManager, PluginLog);
        CharacterInfoService = new CharacterInfoService(ObjectTable, PartyList);
        GearRefresher.Instance.Enable(GameInteropProvider);
        OwnCharacterDataProvider.Initialize(ClientState, Framework);
        return HrtDataManager.Initialized;
    }

    internal static void Dispose()
    {
        GearRefresher.Instance.Dispose();
        OwnCharacterDataProvider.Destroy(ClientState, Framework);
        TaskManager.Dispose();
        IconCache.Dispose();
    }
}

public class FailedToLoadException : Exception
{
    public FailedToLoadException(string? message) : base(message)
    {
    }
}
#pragma warning restore CS8618