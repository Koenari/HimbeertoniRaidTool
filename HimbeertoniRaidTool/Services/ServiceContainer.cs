using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;

namespace HimbeertoniRaidTool.Plugin.Services;

public interface IModuleServiceContainer : IGlobalServiceContainer
{

}

public interface IGlobalServiceContainer : IDisposable
{
    public IChatProvider Chat { get; }
    public IDataManager DataManager { get; }
    public ITargetManager TargetManager { get; }
    public IClientState ClientState { get; }
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
    internal IUiSystem UiSystem { get; }
    internal ModuleManager ModuleManager { get; }
    internal LocalizationManager LocalizationManager { get; }
    internal IFramework Framework { get; }
}

internal class ServiceContainerFactory(GlobalServiceContainer globalServices)
{
    public IModuleServiceContainer CreateModuleServiceContainer<TModule>()
        where TModule : IHrtModule =>
        new ModuleScopedServiceContainer<TModule>(globalServices);
}

internal sealed class ModuleScopedServiceContainer<TModule> : IModuleServiceContainer where TModule : IHrtModule
{
    private readonly GlobalServiceContainer _globalServices;
    public ModuleScopedServiceContainer(GlobalServiceContainer globalServices)
    {
        _globalServices = globalServices;
        Logger = new LoggingProxy(_globalServices.Logger, $"[{TModule.Name}]");
        UiSystem = UiSystemFactory.CreateUiSystem<TModule>(this);
        _globalServices.DalamudServices.PluginInterface.UiBuilder.Draw += UiSystem.Draw;
    }

    public IChatProvider Chat => _globalServices.Chat;
    public IDataManager DataManager => _globalServices.DataManager;
    public ITargetManager TargetManager => _globalServices.TargetManager;
    public IClientState ClientState => _globalServices.ClientState;
    public IObjectTable ObjectTable => _globalServices.DalamudServices.ObjectTable;
    public IPartyList PartyList => _globalServices.PartyList;
    public ICondition Condition => _globalServices.Condition;
    public ILogger Logger { get; }
    public IconCache IconCache => _globalServices.IconCache;
    public HrtDataManager HrtDataManager => _globalServices.HrtDataManager;
    public TaskManager TaskManager => _globalServices.TaskManager;
    public ConnectorPool ConnectorPool => _globalServices.ConnectorPool;
    public ConfigurationManager ConfigManager => _globalServices.ConfigManager;
    public CharacterInfoService CharacterInfoService => _globalServices.CharacterInfoService;
    public ExamineGearDataProvider ExamineGearDataProvider => _globalServices.ExamineGearDataProvider;
    public OwnCharacterDataProvider OwnCharacterDataProvider => _globalServices.OwnCharacterDataProvider;
    public IUiSystem UiSystem { get; }
    public ModuleManager ModuleManager => _globalServices.ModuleManager;
    public LocalizationManager LocalizationManager => _globalServices.LocalizationManager;
    public IFramework Framework => _globalServices.Framework;

    public void Dispose()
    {
        _globalServices.DalamudServices.PluginInterface.UiBuilder.Draw -= UiSystem.Draw;
        UiSystem.RemoveAllWindows();
    }
}

internal class GlobalServiceContainer : IGlobalServiceContainer
{
    internal GlobalServiceContainer(IDalamudPluginInterface pluginInterface)
    {
        DalamudServices = pluginInterface.Create<DalamudServiceWrapper>()
                       ?? throw new FailedToLoadException("Could not initialize dalamud services");
        CommonLibrary.Init(DataManager.Excel, pluginInterface.UiLanguage);
        Logger = new LoggingProxy(DalamudServices.PluginLog, "[HRT]");
        Chat = new DalamudChatProxy(DalamudServices.ChatGui);
        IconCache = new IconCache(DalamudServices.TextureProvider);
        HrtDataManager = new HrtDataManager(DalamudServices.PluginInterface, Logger, DataManager);
        if (!HrtDataManager.Initialized)
            throw new FailedToLoadException("Could not initialize data manager");
        TaskManager = new TaskManager(DalamudServices.Framework, Logger);
        ConnectorPool = new ConnectorPool(HrtDataManager, TaskManager, DataManager, Logger);
        CharacterInfoService = new CharacterInfoService(DalamudServices.ObjectTable, PartyList, ClientState);
        ExamineGearDataProvider = new ExamineGearDataProvider(DalamudServices.GameInteropProvider, Logger,
                                                              DalamudServices.ObjectTable, HrtDataManager,
                                                              CharacterInfoService,
                                                              ConnectorPool);
        OwnCharacterDataProvider = new OwnCharacterDataProvider(DalamudServices.ClientState,
                                                                DalamudServices.Framework, Logger, HrtDataManager);
        UiSystem = UiSystemFactory.CreateGlobalUiSystem(this);
        ConfigManager =
            new ConfigurationManager(pluginInterface, this);
        LocalizationManager = new LocalizationManager(DalamudServices.PluginInterface, Logger);
        ModuleManager = new ModuleManager(Logger, ConfigManager, HrtDataManager, DalamudServices.CommandManager,
                                          LocalizationManager, DalamudServices.PluginInterface,
                                          new ServiceContainerFactory(this));
        DalamudServices.PluginInterface.UiBuilder.Draw += UiSystem.Draw;
    }

    public void Dispose()
    {
        DalamudServices.PluginInterface.UiBuilder.Draw -= UiSystem.Draw;
        ConfigManager.Save();
        HrtDataManager.Save();
        ModuleManager.Dispose();
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
    public IClientState ClientState => DalamudServices.ClientState;
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
    internal DalamudServiceWrapper DalamudServices { get; }
    public ModuleManager ModuleManager { get; }
    public LocalizationManager LocalizationManager { get; }
    public IFramework Framework => DalamudServices.Framework;

#pragma warning disable CS8618
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.GLobal")]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    internal sealed class DalamudServiceWrapper
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

        [PluginService] public ICommandManager CommandManager { get; set; }
    }
#pragma warning restore CS8618
}

public class FailedToLoadException(string? message) : Exception(message);