using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;

#pragma warning disable CS8618
namespace HimbeertoniRaidTool.Plugin.Services;

public interface IModuleServiceContainer : IGlobalServiceContainer
{
    internal EditWindowFactory EditWindows { get; }
}

public interface IGlobalServiceContainer : IDisposable
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
    internal ExamineGearDataProvider ExamineGearDataProvider { get; }
    internal OwnCharacterDataProvider OwnCharacterDataProvider { get; }
    internal INotificationManager NotificationManager { get; }

}

internal sealed class ModuleScopedServiceContainer(IHrtModule module, IGlobalServiceContainer globalServices)
    : IModuleServiceContainer
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
    public ExamineGearDataProvider ExamineGearDataProvider => globalServices.ExamineGearDataProvider;
    public OwnCharacterDataProvider OwnCharacterDataProvider => globalServices.OwnCharacterDataProvider;
    public INotificationManager NotificationManager => globalServices.NotificationManager;

    public EditWindowFactory EditWindows { get; } = new(module);

    public void Dispose() { }
}

internal static class ServiceManager
{
    private static volatile bool _initialized;
    private static GlobalServiceContainer ServiceContainer { get; set; }

    internal static IGlobalServiceContainer Init(IDalamudPluginInterface pluginInterface)
    {
        if (_initialized) return ServiceContainer;
        _initialized = true;
        return ServiceContainer = new GlobalServiceContainer(pluginInterface);
    }

    internal static IModuleServiceContainer GetServiceContainer(IHrtModule module)
        => new ModuleScopedServiceContainer(module, ServiceContainer);


    private class GlobalServiceContainer : IGlobalServiceContainer
    {
        internal GlobalServiceContainer(IDalamudPluginInterface pluginInterface)
        {
            DalamudServices = pluginInterface.Create<DalamudServiceWrapper>()
                           ?? throw new FailedToLoadException("Could not initialize dalamud services");
            Logger = new LoggingProxy(DalamudServices.PluginLog);
            Chat = new DalamudChatProxy(DalamudServices.ChatGui);
            Common.Services.ServiceManager.Init(DataManager.Excel, pluginInterface.UiLanguage);
            IconCache = new IconCache(DalamudServices.TextureProvider);
            HrtDataManager = new HrtDataManager(PluginInterface, Logger, DataManager);
            if (!HrtDataManager.Initialized)
                throw new FailedToLoadException("Could not initialize data manager");
            TaskManager = new TaskManager(DalamudServices.Framework, Logger);
            ConnectorPool = new ConnectorPool(HrtDataManager, TaskManager, DataManager, Logger);
            CharacterInfoService = new CharacterInfoService(ObjectTable, PartyList, ClientState);
            ExamineGearDataProvider = new ExamineGearDataProvider(DalamudServices.GameInteropProvider, Logger,
                                                                  ObjectTable, HrtDataManager, CharacterInfoService,
                                                                  ConnectorPool);
            OwnCharacterDataProvider = new OwnCharacterDataProvider(DalamudServices.ClientState,
                                                                    DalamudServices.Framework, Logger, HrtDataManager);
            ConfigManager =
                new ConfigurationManager(pluginInterface, Logger, HrtDataManager.ModuleConfigurationManager);
            UiSystem.Initialize(IconCache, DataManager, Condition);
        }

        public void Dispose()
        {
            ConnectorPool.Dispose();
            ConfigManager.Dispose();
            ExamineGearDataProvider.Dispose();
            OwnCharacterDataProvider.Dispose();
            TaskManager.Dispose();
        }

        public IChatProvider Chat { get; }
        public IDataManager DataManager => DalamudServices.DataManager;
        public ITargetManager TargetManager => DalamudServices.TargetManager;
        public IDalamudPluginInterface PluginInterface => DalamudServices.PluginInterface;
        public IClientState ClientState => DalamudServices.ClientState;
        public IObjectTable ObjectTable => DalamudServices.ObjectTable;
        public IPartyList PartyList => DalamudServices.PartyList;
        public ICondition Condition => DalamudServices.Condition;
        public ILogger Logger { get; }
        public IconCache IconCache { get; }
        public HrtDataManager HrtDataManager { get; }
        public TaskManager TaskManager { get; }
        public ConnectorPool ConnectorPool { get; }
        public ConfigurationManager ConfigManager { get; }
        public CharacterInfoService CharacterInfoService { get; }
        public ExamineGearDataProvider ExamineGearDataProvider { get; }
        public OwnCharacterDataProvider OwnCharacterDataProvider { get; }
        public INotificationManager NotificationManager => DalamudServices.NotificationManager;
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
}

public class FailedToLoadException(string? message) : Exception(message);
#pragma warning restore CS8618