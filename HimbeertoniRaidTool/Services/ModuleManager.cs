using System.Diagnostics.CodeAnalysis;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Planner;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster;
using HimbeertoniRaidTool.Plugin.UI;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

internal interface IModuleScopedModuleManager
{
    void DrawGlobalButtons();
    (bool Sucess, TReturn? ReturnValue) ExecuteIntegration<TCallee, TReturn>(Func<TCallee, TReturn> integrationFunc)
        where TCallee : class, IHrtModule;
    void ExecuteIntegration<TCallee>(Action<TCallee> integrationFunc) where TCallee : class, IHrtModule;
}

internal interface IModuleManager
{
    IEnumerable<IModuleManifest> GetAvailableModules();
    internal void LoadModules();
    void Dispose();
}

internal class ModuleManager : IModuleManager
{
    private readonly ModuleManifest<LootMasterModule, LootMasterConfiguration> _lootMasterModule;
    private readonly ModuleManifest<PlannerModule, PlannerModuleConfig> _plannerModule;

    private IEnumerable<IInternalModuleManifest> _availableModules
    {
        get
        {
            yield return _lootMasterModule;
            yield return _plannerModule;
        }
    }
    private readonly ILogger _logger;
    private readonly ConfigurationManager _configurationManager;
    private readonly CommandManager _commandManager;
    private readonly LocalizationManager _localizationManager;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ServiceContainerFactory _serviceContainerFactory;

    public ModuleManager(ILogger logger, ConfigurationManager configurationManager,
                         CommandManager commandManager, LocalizationManager localizationManager,
                         IDalamudPluginInterface pluginInterface, ServiceContainerFactory serviceContainerFactory)
    {
        _configurationManager = configurationManager;
        _commandManager = commandManager;
        _logger = logger;
        _localizationManager = localizationManager;
        _pluginInterface = pluginInterface;
        _serviceContainerFactory = serviceContainerFactory;
        _lootMasterModule = new ModuleManifest<LootMasterModule, LootMasterConfiguration>(
            new ModuleScopedModuleManager<LootMasterModule>(this),
            configurationManager.CoreConfig.Data
                                .IsModuleEnabled<LootMasterModule>());
        _plannerModule = new ModuleManifest<PlannerModule, PlannerModuleConfig>(
            new ModuleScopedModuleManager<PlannerModule>(this),
            configurationManager.CoreConfig.Data
                                .IsModuleEnabled<PlannerModule>());
        _configurationManager.CoreConfig.OnConfigChange += UpdateConfiguration;
    }

    public void UpdateConfiguration()
    {
        foreach ((string internalName, bool enabledNew) in _configurationManager.CoreConfig.Data.ModulesEnabled)
        {
            var manifest = _availableModules.FirstOrDefault(m => m?.InternalName == internalName, null);
            if (manifest == null) continue;
            if (manifest.Enabled == enabledNew) continue;
            var internalManifest =
                _availableModules.FirstOrDefault(m => m?.InternalName == manifest.InternalName, null);
            if (internalManifest == null) continue;
            _configurationManager.CoreConfig.Data.ModulesEnabled[internalManifest.InternalName] = enabledNew;
            if (_configurationManager.CoreConfig.Data.ModulesEnabled[internalManifest.InternalName])
            {
                internalManifest.Enable();
            }
            else
            {
                internalManifest.Disable();
            }
        }
    }

    internal IModuleManifest<TModule>? GetModule<TModule>()
        where TModule : class, IHrtModule =>
        _availableModules.FirstOrDefault(m => m?.InternalName == TModule.InternalName,
                                         null) as IModuleManifest<TModule>;

    public IEnumerable<IModuleManifest> GetAvailableModules() => _availableModules;

    public void LoadModules()
    {
        foreach (var moduleManifest in _availableModules)
        {
            if (!_configurationManager.CoreConfig.Data.IsModuleEnabled(moduleManifest.InternalName))
                moduleManifest.Disable();
            moduleManifest.Load();
        }
        if (_lootMasterModule.Loaded)
            _pluginInterface.UiBuilder.OpenMainUi += _lootMasterModule.Module.ShowUi;
    }



    internal void DrawGlobalButtons<TModule>() where TModule : IHrtModule
    {
        if (ImGuiHelper.Button(FontAwesomeIcon.Cog, "##showConfig", LootmasterLoc.ui_btn_tt_showConfig))
            _configurationManager.Show();
        foreach (var manifest in _availableModules)
        {
            if (manifest.InternalName == TModule.InternalName) continue;
            foreach (var descriptor in manifest.GlobalButtons)
            {
                ImGui.SameLine();
                if (ImGuiHelper.Button(descriptor.Icon, descriptor.Id, descriptor.ToolTip))
                    descriptor.OnClick();
            }
        }
    }



    public void Dispose()
    {
        _configurationManager.CoreConfig.OnConfigChange -= UpdateConfiguration;
        foreach (var module in _availableModules)
        {
            module.Unload();
            module.Dispose();
        }
    }

    internal class ModuleScopedModuleManager<TModule>(ModuleManager parent)
        : IModuleScopedModuleManager
        where TModule : IHrtModule
    {


        public void DrawGlobalButtons() => parent.DrawGlobalButtons<TModule>();
        public (bool Sucess, TReturn? ReturnValue) ExecuteIntegration<TCallee, TReturn>(
            Func<TCallee, TReturn> integrationFunc)
            where TCallee : class, IHrtModule
        {
            var callee = parent.GetModule<TCallee>();
            if (callee is { Loaded: true })
                return (true, integrationFunc(callee.Module));
            return (false, default);

        }
        public void ExecuteIntegration<TCallee>(Action<TCallee> integrationFunc)
            where TCallee : class, IHrtModule
        {
            var callee = parent.GetModule<TCallee>();
            if (callee is { Loaded: true })
                integrationFunc(callee.Module);
        }

        internal IModuleServiceContainer CreateModuleServiceContainer() =>
            parent._serviceContainerFactory.CreateModuleServiceContainer<TModule>(this);

        internal void RemoveCommands(IEnumerable<HrtCommand> commands) =>
            parent._commandManager.RemoveCommands(commands);

        internal void AddCommands(IEnumerable<HrtCommand> commands) => parent._commandManager.AddCommands(commands);
        public ILogger Logger => parent._logger;
        public ConfigurationManager ConfigurationManager => parent._configurationManager;
        public LocalizationManager LocalizationManager => parent._localizationManager;

    }

    private interface IInternalModuleManifest : IModuleManifest, IDisposable
    {
        void Enable();

        void Disable();

        internal void Load();

        internal void Unload();
        IList<ButtenDescriptor> GlobalButtons { get; }
    }

    private class ModuleManifest<TModule, TConfig> : IInternalModuleManifest, IModuleManifest<TModule>
        where TModule : class, IHrtModule<TModule, TConfig> where TConfig : IHrtModuleConfiguration
    {
        public string InternalName => TModule.InternalName;

        public string Name => TModule.Name;

        public string Description => TModule.Description;

        public TModule? Module { get; private set; }

        [MemberNotNullWhen(true, nameof(Module))]
        public bool Loaded => Module != null;

        public bool CanBeDisabled => TModule.CanBeDisabled;

        public bool Enabled { get; private set; }
        public event Action<IModuleManifest<TModule>>? StateChanged;

        public IList<ButtenDescriptor> GlobalButtons => Module?.GlobalButtons ?? Array.Empty<ButtenDescriptor>();

        private readonly IModuleServiceContainer _serviceContainer;
        private readonly TConfig _configuration;
        private readonly ModuleScopedModuleManager<TModule> _parent;
        public ModuleManifest(ModuleScopedModuleManager<TModule> parent,
                              bool enabled = true)
        {
            _parent = parent;
            Enabled = enabled | !TModule.CanBeDisabled;
            _serviceContainer = parent.CreateModuleServiceContainer();
            _configuration = TModule.CreateConfiguration(_serviceContainer);
            if (_parent.ConfigurationManager.RegisterConfig(_configuration))
                _configuration.AfterLoad();
            else
                _parent.Logger.Error("Configuration load error:{S}", TModule.Name);
        }

        public void Enable()
        {
            Enabled = true;
            Load();
        }

        public void Disable()
        {
            if (!CanBeDisabled) return;
            Enabled = false;
            Unload();
        }
        public void Load()
        {
            if (Loaded || !Enabled) return;
            var moduleType = typeof(TModule);
            try
            {
                _parent.Logger.Debug("Creating instance of: {ModuleTypeName}", moduleType.Name);
                var module = TModule.Create(_serviceContainer, _configuration);
                _parent.Logger.Debug("Calling {S}.AfterFullyLoaded()", TModule.InternalName);
                module.AfterFullyLoaded();
                _parent.LocalizationManager.OnLanguageChanged += module.OnLanguageChange;
                _parent.AddCommands(module.Commands);
                _parent.Logger.Information("Successfully loaded module: {S}", TModule.Name);
                Module = module;
                StateChanged?.Invoke(this);
            }
            catch (Exception e)
            {
                _parent.Logger.Error(e, "Failed to load module: {ModuleTypeName}", moduleType.Name);
            }
        }

        public void Unload()
        {
            if (Module == null) return;
            _parent.RemoveCommands(Module.Commands);
            try
            {
                _parent.LocalizationManager.OnLanguageChanged -= Module.OnLanguageChange;
                Module.Dispose();
            }
            catch (Exception e)
            {
                _parent.Logger.Fatal(e, "Unable to Dispose module \"{Type}\"", typeof(TModule));
            }
            finally
            {
                Module = null;
                StateChanged?.Invoke(this);
            }
        }
        public void Dispose() => _serviceContainer.Dispose();
    }

}

internal static class ConfigDataExtension
{
    public static bool IsModuleEnabled<TModule>(this CoreConfig.ConfigData data) where TModule : IHrtModule =>
        data.IsModuleEnabled(TModule.InternalName);

    public static bool IsModuleEnabled(this CoreConfig.ConfigData data, string internalName) =>
        data.ModulesEnabled.TryAdd(internalName, true) || data.ModulesEnabled[internalName];
}

public interface IModuleManifest<out TModule> : IModuleManifest where TModule : class, IHrtModule
{
    TModule? Module { get; }

    [MemberNotNullWhen(true, nameof(Module))]
    bool Loaded { get; }

    event Action<IModuleManifest<TModule>>? StateChanged;
}

public interface IModuleManifest
{
    string InternalName { get; }

    string Name { get; }

    string Description { get; }

    bool CanBeDisabled { get; }

    bool Enabled { get; }
}