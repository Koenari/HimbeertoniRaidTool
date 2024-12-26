using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;

#pragma warning disable CS8618
namespace HimbeertoniRaidTool.Plugin.Services;

public interface IServiceContainer
{
    public IChatProvider Chat { get; }
    public IDataManager DataManager { get; }
    public ITargetManager TargetManager { get; }
    public IDalamudPluginInterface PluginInterface { get; }
    public IClientState ClientState { get; }
    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public ICondition Condition { get; }
    public ILogger Logger { get; }
    public IconCache IconCache { get; }
    public HrtDataManager HrtDataManager { get; }
    internal TaskManager TaskManager { get; }
    internal ConnectorPool ConnectorPool { get; }
    internal ConfigurationManager ConfigManager { get; }
    internal CharacterInfoService CharacterInfoService { get; }
    internal ItemInfo ItemInfo { get; }
    internal ExamineGearDataProvider ExamineGearDataProvider { get; }
    internal OwnCharacterDataProvider OwnCharacterDataProvider { get; }
    internal GameInfo GameInfo { get; }
    internal INotificationManager NotificationManager { get; }
    internal EditWindowFactory EditWindows { get; }
}

internal sealed class ModuleScopedServiceContainer(IHrtModule module, GlobalServiceContainer globalServices)
    : IServiceContainer
{

    public IChatProvider Chat => globalServices.Chat;
    public IDataManager DataManager => globalServices.DataManager;
    public ITargetManager TargetManager => globalServices.TargetManager;
    public IDalamudPluginInterface PluginInterface => globalServices.PluginInterface;
    public IClientState ClientState => globalServices.ClientState;
    public IObjectTable ObjectTable => globalServices.ObjectTable;
    public IPartyList PartyList => globalServices.PartyList;
    public ICondition Condition => globalServices.Condition;
    public ILogger Logger => globalServices.Logger;
    public IconCache IconCache => globalServices.IconCache;
    public HrtDataManager HrtDataManager => globalServices.HrtDataManager;
    public TaskManager TaskManager => globalServices.TaskManager;
    public ConnectorPool ConnectorPool => globalServices.ConnectorPool;
    public ConfigurationManager ConfigManager => globalServices.ConfigManager;
    public CharacterInfoService CharacterInfoService => globalServices.CharacterInfoService;
    public ItemInfo ItemInfo => globalServices.ItemInfo;
    public ExamineGearDataProvider ExamineGearDataProvider => globalServices.ExamineGearDataProvider;
    public OwnCharacterDataProvider OwnCharacterDataProvider => globalServices.OwnCharacterDataProvider;
    public GameInfo GameInfo => globalServices.GameInfo;
    public INotificationManager NotificationManager => globalServices.NotificationManager;

    public EditWindowFactory EditWindows { get; } = new(module.WindowSystem);
}

internal static class ServiceManager
{
    private static bool _initialized;
    public static IChatProvider Chat => ServiceContainer.Chat;
    public static IDataManager DataManager => ServiceContainer.DataManager;
    public static ITargetManager TargetManager => ServiceContainer.TargetManager;
    public static IDalamudPluginInterface PluginInterface => ServiceContainer.PluginInterface;
    public static IClientState ClientState => ServiceContainer.ClientState;
    public static IObjectTable ObjectTable => ServiceContainer.ObjectTable;
    public static IPartyList PartyList => ServiceContainer.PartyList;
    public static ICondition Condition => ServiceContainer.Condition;
    public static ILogger Logger => ServiceContainer.Logger;
    public static IconCache IconCache => ServiceContainer.IconCache;
    public static HrtDataManager HrtDataManager => ServiceContainer.HrtDataManager;
    internal static TaskManager TaskManager => ServiceContainer.TaskManager;
    internal static ConnectorPool ConnectorPool => ServiceContainer.ConnectorPool;
    internal static ConfigurationManager ConfigManager => ServiceContainer.ConfigManager;
    internal static CharacterInfoService CharacterInfoService => ServiceContainer.CharacterInfoService;
    internal static ItemInfo ItemInfo => ServiceContainer.ItemInfo;
    internal static ExamineGearDataProvider ExamineGearDataProvider => ServiceContainer.ExamineGearDataProvider;
    internal static OwnCharacterDataProvider OwnCharacterDataProvider => ServiceContainer.OwnCharacterDataProvider;
    internal static GameInfo GameInfo => Common.Services.ServiceManager.GameInfo;
    internal static INotificationManager NotificationManager => ServiceContainer.NotificationManager;
    private static GlobalServiceContainer ServiceContainer { get; set; }

    internal static bool Init(IDalamudPluginInterface pluginInterface)
    {
        if (_initialized) return false;
        _initialized = true;
        ServiceContainer = new GlobalServiceContainer(pluginInterface);
        return HrtDataManager.Initialized;
    }

    internal static IServiceContainer GetServiceContainer(IHrtModule module)
        => new ModuleScopedServiceContainer(module, ServiceContainer);

    internal static void Dispose()
    {
        ConnectorPool.Dispose();
        ConfigManager.Dispose();
        ExamineGearDataProvider.Dispose();
        OwnCharacterDataProvider.Dispose();
        TaskManager.Dispose();
    }


}

internal class GlobalServiceContainer
{
    internal GlobalServiceContainer(IDalamudPluginInterface pluginInterface)
    {
        DalamudServices = pluginInterface.Create<DalamudServiceWrapper>()
                       ?? throw new FailedToLoadException("Could not initialize Service Manager");
        Logger = new LoggingProxy(DalamudServices.PluginLog);
        Chat = new DalamudChatProxy(DalamudServices.ChatGui);
        Common.Services.ServiceManager.Init(DataManager.Excel, pluginInterface.UiLanguage);
        IconCache = new IconCache(DalamudServices.TextureProvider);
        HrtDataManager = new HrtDataManager(PluginInterface);
        TaskManager = new TaskManager(DalamudServices.Framework);
        ConnectorPool = new ConnectorPool(TaskManager, Logger);
        CharacterInfoService = new CharacterInfoService(ObjectTable, PartyList, ClientState);
        ExamineGearDataProvider = new ExamineGearDataProvider(DalamudServices.GameInteropProvider);
        OwnCharacterDataProvider = new OwnCharacterDataProvider(DalamudServices.ClientState,
                                                                DalamudServices.Framework);
        ConfigManager = new ConfigurationManager(pluginInterface);
    }

    internal IChatProvider Chat { get; }
    internal IDataManager DataManager => DalamudServices.DataManager;
    internal ITargetManager TargetManager => DalamudServices.TargetManager;
    internal IDalamudPluginInterface PluginInterface => DalamudServices.PluginInterface;
    internal IClientState ClientState => DalamudServices.ClientState;
    internal IObjectTable ObjectTable => DalamudServices.ObjectTable;
    internal IPartyList PartyList => DalamudServices.PartyList;
    internal ICondition Condition => DalamudServices.Condition;
    internal ILogger Logger { get; }
    internal IconCache IconCache { get; }
    internal HrtDataManager HrtDataManager { get; }
    internal TaskManager TaskManager { get; }
    internal ConnectorPool ConnectorPool { get; }
    internal ConfigurationManager ConfigManager { get; }
    internal CharacterInfoService CharacterInfoService { get; }
    internal ItemInfo ItemInfo => Common.Services.ServiceManager.ItemInfo;
    internal ExamineGearDataProvider ExamineGearDataProvider { get; }
    internal OwnCharacterDataProvider OwnCharacterDataProvider { get; }
    internal GameInfo GameInfo => Common.Services.ServiceManager.GameInfo;
    internal INotificationManager NotificationManager => DalamudServices.NotificationManager;
    private DalamudServiceWrapper DalamudServices { get; }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    private class DalamudServiceWrapper
    {
        [PluginService] public IChatGui ChatGui { get; set; }
        [PluginService] public IDataManager DataManager { get; set; }
        [PluginService] public ITargetManager TargetManager { get; set; }
        [PluginService] public IDalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public IClientState ClientState { get; set; }
        [PluginService] public IObjectTable ObjectTable { get; set; }
        [PluginService] public IPartyList PartyList { get; set; }
        [PluginService] public ICondition Condition { get; set; }
        [PluginService] public IPluginLog PluginLog { get; set; }
        [PluginService] public IGameInteropProvider GameInteropProvider { get; set; }
        [PluginService] public ITextureProvider TextureProvider { get; set; }
        [PluginService] public IFramework Framework { get; set; }
        [PluginService] public INotificationManager NotificationManager { get; set; }
    }
}

public class FailedToLoadException(string? message) : Exception(message);
#pragma warning restore CS8618