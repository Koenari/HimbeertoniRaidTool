using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Planner;
using HimbeertoniRaidTool.Plugin.Modules.Core;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class ModuleManager
{
    private const string CONFIG_FILE_NAME = "ModuleManager";
    private readonly ConfigData _config = new();
    private readonly ModuleManifest<CoreModule> _coreModule;
    private readonly Dictionary<string, IInternalModuleManifest> _availableModules = [];
    private readonly HashSet<string> _dalamudRegisteredCommands = [];
    private readonly ILogger _logger;
    private readonly ConfigurationManager _configurationManager;
    private readonly HrtDataManager _dataManager;
    private readonly ICommandManager _commandManager;
    private readonly LocalizationManager _localizationManager;
    private readonly IDalamudPluginInterface _pluginInterface;
    public event Action<IEnumerable<IModuleManifest>>? ModuleStateChanged;
    public ModuleManager(ILogger logger, ConfigurationManager configurationManager, HrtDataManager dataManager,
                         ICommandManager commandManager, LocalizationManager localizationManager,
                         IDalamudPluginInterface pluginInterface)
    {
        _configurationManager = configurationManager;
        _dataManager = dataManager;
        _commandManager = commandManager;
        _logger = logger;
        _localizationManager = localizationManager;
        _pluginInterface = pluginInterface;
        _dataManager.ModuleConfigurationManager.LoadConfiguration(CONFIG_FILE_NAME, ref _config);
        _coreModule = new ModuleManifest<CoreModule>(this, CoreModule.INTERNAL_NAME);
        AddAvailableModule<LootMasterModule>(LootMasterModule.INTERNAL_NAME, false);
        AddAvailableModule<PlannerModule>(PlannerModule.INTERNAL_NAME, true);
    }

    private bool AddAvailableModule<TMod>(string internalName, bool canBeDisabled)
        where TMod : class, IHrtModule, new()
    {
        var manifest =
            new ModuleManifest<TMod>(this, internalName, _config.IsModuleEnabled(internalName), canBeDisabled);
        return _availableModules.TryAdd(internalName, manifest);
    }

    internal void UpdateConfiguration(Dictionary<IModuleManifest, bool> moduleConfigurationUpdate)
    {
        bool changed = false;
        foreach ((var manifest, bool enabledNew) in moduleConfigurationUpdate)
        {
            if (manifest.Enabled == enabledNew) continue;
            if (!TryGetModuleManifestInternal(manifest.InternalName, out var internalManifest)) continue;
            changed = true;
            _config.ModuleEnabled[internalManifest.InternalName] = enabledNew;
            if (_config.ModuleEnabled[internalManifest.InternalName])
            {
                internalManifest.Enable();
            }
            else
            {
                internalManifest.Disable();
            }
        }
        if (changed) ModuleStateChanged?.Invoke(GetAvailableModules());
    }

    public IEnumerable<IModuleManifest> GetAvailableModules()
    {
        yield return _coreModule;
        foreach (var moduleManifest in _availableModules.Values)
        {
            yield return moduleManifest;
        }
    }

    public bool TryGetModule<TModule>(string internalName, [NotNullWhen(true)] out TModule? module)
        where TModule : class
    {
        module = null;
        if (!TryGetModuleManifestInternal(internalName, out var internalManifest)) return false;
        if (!internalManifest.Enabled) return false;
        module = internalManifest.GetModule<TModule>();
        return module != null;
    }

    private bool TryGetModuleManifestInternal(string internalName,
                                              [NotNullWhen(true)] out IInternalModuleManifest? module) =>
        _availableModules.TryGetValue(internalName, out module);

    internal void LoadModules()
    {
        //Ensure core module is loaded first
        _coreModule.Enable();
        foreach ((string internalName, var moduleManifest) in _availableModules)
        {
            _config.ModuleEnabled.TryAdd(internalName, true);
            if (!_config.ModuleEnabled[internalName])
                moduleManifest.Disable();
            moduleManifest.Load();
        }
        if (TryGetModule(LootMasterModule.INTERNAL_NAME, out LootMasterModule? lootMasterModule))
            _pluginInterface.UiBuilder.OpenMainUi += lootMasterModule.ShowUi;
        ModuleStateChanged?.Invoke(GetAvailableModules());
    }

    private void RemoveCommand(HrtCommand command)
    {
        if (_dalamudRegisteredCommands.Remove(command.Command))
            _commandManager.RemoveHandler(command.Command);
        foreach (string altCommand in command.AltCommands)
        {
            if (_dalamudRegisteredCommands.Remove(altCommand))
                _commandManager.RemoveHandler(altCommand);
        }
        _coreModule.Module?.RemoveCommand(command);
    }

    private void AddCommand(HrtCommand command)
    {
        if (command.ShouldExposeToDalamud)
        {
            if (!_dalamudRegisteredCommands.Contains(command.Command) && _commandManager.AddHandler(command.Command,
                    new CommandInfo(command.OnCommand)
                    {
                        HelpMessage = command.Description,
                        ShowInHelp = command.ShowInHelp,
                    }))
            {
                _dalamudRegisteredCommands.Add(command.Command);
            }


            if (command.ShouldExposeAltsToDalamud)
            {
                foreach (string alt in command.AltCommands)
                {
                    if (!_dalamudRegisteredCommands.Contains(alt) && _commandManager.AddHandler(alt,
                            new CommandInfo(command.OnCommand)
                            {
                                HelpMessage = command.Description,
                                ShowInHelp = false,
                            }))
                        _dalamudRegisteredCommands.Add(alt);
                }
            }
        }

        _coreModule.Module?.AddCommand(command);
    }
    public void Dispose()
    {
        _dataManager.ModuleConfigurationManager.SaveConfiguration(CONFIG_FILE_NAME, _config);
        foreach (var module in _availableModules.Values)
        {
            module.Unload();
        }
        _coreModule.Unload();
        foreach (string command in _dalamudRegisteredCommands)
        {
            _logger.Error($"Command \"{command}\" was not removed by module unload");
            _commandManager.RemoveHandler(command);
        }
    }

    private interface IInternalModuleManifest : IModuleManifest
    {
        public TModule? GetModule<TModule>() where TModule : class;

        public void Enable();

        public void Disable();

        internal void Load();

        internal void Unload();
    }

    private class ModuleManifest<TModule>(
        ModuleManager parent,
        string internalName,
        bool enabled = true,
        bool canBeDisabled = false)
        : IInternalModuleManifest where TModule : class, IHrtModule, new()
    {
        public string InternalName => internalName;
        internal TModule? Module { get; private set; }

        public bool CanBeDisabled { get; } = canBeDisabled;

        public bool Enabled { get; private set; } = enabled;

        public bool Loaded => Module != null;

        public T? GetModule<T>() where T : class => Module as T;

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
                parent._logger.Debug($"Creating instance of: {moduleType.Name}");
                var module = new TModule();
                if (parent._configurationManager.RegisterConfig(module.Configuration))
                    module.Configuration.AfterLoad();
                else
                    parent._logger.Error($"Configuration load error:{module.Name}");
                parent._logger.Debug($"Calling {module.InternalName}.AfterFullyLoaded()");
                module.AfterFullyLoaded();
                parent._localizationManager.OnLanguageChanged += module.OnLanguageChange;
                foreach (var command in module.Commands)
                {
                    parent.AddCommand(command);
                }
                parent._logger.Information($"Successfully loaded module: {module.Name}");
                Module = module;
            }
            catch (Exception e)
            {
                parent._logger.Error(e, $"Failed to load module: {moduleType.Name}");
            }
        }

        public void Unload()
        {
            if (Module == null) return;
            foreach (var command in Module.Commands)
            {
                parent.RemoveCommand(command);
            }
            try
            {
                parent._localizationManager.OnLanguageChanged -= Module.OnLanguageChange;
                Module.Dispose();
                Module.Services.Dispose();
            }
            catch (Exception e)
            {
                parent._logger.Fatal(e, $"Unable to Dispose module \"{typeof(TModule)}\"");
            }
            finally
            {
                Module = null;
            }
        }
    }

    private class ConfigData : IHrtConfigData
    {
        [JsonProperty] public Dictionary<string, bool> ModuleEnabled { get; set; } = [];

        public bool IsModuleEnabled(string internalName) =>
            ModuleEnabled.TryAdd(internalName, true) || ModuleEnabled[internalName];

        public void AfterLoad(HrtDataManager dataManager) { }

        public void BeforeSave() { }
    }


}

public interface IModuleManifest
{
    public string InternalName { get; }

    public bool CanBeDisabled { get; }

    public bool Enabled { get; }

    public bool Loaded { get; }

}