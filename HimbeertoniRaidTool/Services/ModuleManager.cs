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
    public IModuleManifest<CoreModule> CoreModule => _coreModule;
    public IModuleManifest<LootMasterModule> LootMasterModule => _lootMasterModule;
    public IModuleManifest<PlannerModule> PlannerModule => _plannerModule;

    private const string CONFIG_FILE_NAME = "ModuleManager";
    private readonly ConfigData _config = new();

    private readonly ModuleManifest<CoreModule> _coreModule;
    private readonly ModuleManifest<LootMasterModule> _lootMasterModule;
    private readonly ModuleManifest<PlannerModule> _plannerModule;


    private IEnumerable<IInternalModuleManifest> AvailableModules
    {
        get
        {
            yield return _lootMasterModule;
            yield return _plannerModule;
        }
    }
    private readonly HashSet<string> _dalamudRegisteredCommands = [];
    private readonly ILogger _logger;
    private readonly ConfigurationManager _configurationManager;
    private readonly HrtDataManager _dataManager;
    private readonly ICommandManager _commandManager;
    private readonly LocalizationManager _localizationManager;
    private readonly IDalamudPluginInterface _pluginInterface;

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
        _coreModule = new ModuleManifest<CoreModule>(this, Modules.Core.CoreModule.INTERNAL_NAME);
        _lootMasterModule = CreateModule<LootMasterModule>(Modules.LootMaster.LootMasterModule.INTERNAL_NAME, false);
        _plannerModule = CreateModule<PlannerModule>(Modules.Planner.PlannerModule.INTERNAL_NAME, true);
    }

    private ModuleManifest<TMod> CreateModule<TMod>(string internalName, bool canBeDisabled)
        where TMod : class, IHrtModule, new() =>
        new(this, internalName, _config.IsModuleEnabled(internalName), canBeDisabled);

    internal void UpdateConfiguration(Dictionary<IModuleManifest, bool> moduleConfigurationUpdate)
    {
        foreach ((var manifest, bool enabledNew) in moduleConfigurationUpdate)
        {
            if (manifest.Enabled == enabledNew) continue;
            var internalManifest = AvailableModules.FirstOrDefault(m => m?.InternalName == manifest.InternalName, null);
            if (internalManifest == null) continue;
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
    }

    public IEnumerable<IModuleManifest> GetAvailableModules()
    {
        yield return _coreModule;
        foreach (var moduleManifest in AvailableModules)
        {
            yield return moduleManifest;
        }
    }

    internal void LoadModules()
    {
        //Ensure core module is loaded first
        _coreModule.Enable();
        foreach (var moduleManifest in AvailableModules)
        {
            _config.ModuleEnabled.TryAdd(moduleManifest.InternalName, true);
            if (!_config.ModuleEnabled[moduleManifest.InternalName])
                moduleManifest.Disable();
            moduleManifest.Load();
        }
        if (_lootMasterModule.Loaded)
            _pluginInterface.UiBuilder.OpenMainUi += _lootMasterModule.Module.ShowUi;
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
        foreach (var module in AvailableModules)
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
        : IInternalModuleManifest, IModuleManifest<TModule> where TModule : class, IHrtModule, new()
    {
        public string InternalName => internalName;
        public TModule? Module { get; private set; }

        public bool CanBeDisabled { get; } = canBeDisabled;

        public bool Enabled { get; private set; } = enabled;
        public event Action<IModuleManifest<TModule>>? StateChanged;

        [MemberNotNullWhen(true, nameof(Module))]
        public bool Loaded => Module != null;

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
                StateChanged?.Invoke(this);
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
                StateChanged?.Invoke(this);
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

public interface IModuleManifest<out TModule> : IModuleManifest where TModule : class, IHrtModule
{
    public TModule? Module { get; }

    public event Action<IModuleManifest<TModule>>? StateChanged;
}

public interface IModuleManifest
{
    public string InternalName { get; }

    public bool CanBeDisabled { get; }

    public bool Enabled { get; }

    public bool Loaded { get; }

}