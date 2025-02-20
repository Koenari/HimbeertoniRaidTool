using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;

#pragma warning disable CS8618
namespace HimbeertoniRaidTool.Plugin.Services;

public interface IModuleServiceContainer : IGlobalServiceContainer
{

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
    internal IUiSystem UiSystem { get; }

}

internal sealed class ModuleScopedServiceContainer : IModuleServiceContainer
{
    private readonly IGlobalServiceContainer _globalServices;
    public ModuleScopedServiceContainer(IHrtModule module, IGlobalServiceContainer globalServices)
    {
        _globalServices = globalServices;
        UiSystem = UiSystemFactory.CreateUiSystem(module, this);
        PluginInterface.UiBuilder.Draw += UiSystem.Draw;
    }

    public IChatProvider Chat => _globalServices.Chat;
    public IDataManager DataManager => _globalServices.DataManager;
    public ITargetManager TargetManager => _globalServices.TargetManager;
    public IDalamudPluginInterface PluginInterface => _globalServices.PluginInterface;
    public IClientState ClientState => _globalServices.ClientState;
    public IObjectTable ObjectTable => _globalServices.ObjectTable;
    public IPartyList PartyList => _globalServices.PartyList;
    public ICondition Condition => _globalServices.Condition;
    public ILogger Logger => _globalServices.Logger;
    public IconCache IconCache => _globalServices.IconCache;
    public HrtDataManager HrtDataManager => _globalServices.HrtDataManager;
    public TaskManager TaskManager => _globalServices.TaskManager;
    public ConnectorPool ConnectorPool => _globalServices.ConnectorPool;
    public ConfigurationManager ConfigManager => _globalServices.ConfigManager;
    public CharacterInfoService CharacterInfoService => _globalServices.CharacterInfoService;
    public ExamineGearDataProvider ExamineGearDataProvider => _globalServices.ExamineGearDataProvider;
    public OwnCharacterDataProvider OwnCharacterDataProvider => _globalServices.OwnCharacterDataProvider;
    public INotificationManager NotificationManager => _globalServices.NotificationManager;
    public IUiSystem UiSystem { get; }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= UiSystem.Draw;
        UiSystem.RemoveAllWindows();
    }
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
            CommonLibrary.Init(DataManager.Excel, pluginInterface.UiLanguage);
            Logger = new LoggingProxy(DalamudServices.PluginLog);
            Chat = new DalamudChatProxy(DalamudServices.ChatGui);
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
            UiSystem = UiSystemFactory.CreateUiSystem(this);
            PluginInterface.UiBuilder.Draw += UiSystem.Draw;
            ConfigManager =
                new ConfigurationManager(pluginInterface, this);
        }

        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= UiSystem.Draw;
            UiSystem.RemoveAllWindows();
            ConnectorPool.Dispose();
            ConfigManager.Dispose();
            ExamineGearDataProvider.Dispose();
            OwnCharacterDataProvider.Dispose();
            TaskManager.Dispose();
        }
        public IUiSystem UiSystem { get; }
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