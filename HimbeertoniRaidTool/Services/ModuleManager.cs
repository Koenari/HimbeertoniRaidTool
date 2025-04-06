
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Calendar;
using HimbeertoniRaidTool.Plugin.Modules.Core;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class ModuleManager
{
    private const string CONFIG_FILE_NAME = "ModuleManager";
    private readonly ConfigData _config = new();
    private readonly ModuleManifest<CoreModule> _coreModule;
    private readonly List<IInternalModuleManifest> _availableModules;
    private readonly List<string> _dalamudRegisteredCommands = [];
    private readonly ILogger _logger;
    private readonly ConfigurationManager _configurationManager;
    private readonly HrtDataManager _dataManager;
    private readonly ICommandManager _commandManager;
    private readonly LocalizationManager _localizationManager;
    public ModuleManager(ILogger logger, ConfigurationManager configurationManager, HrtDataManager dataManager,
                         ICommandManager commandManager, LocalizationManager localizationManager)
    {
        _configurationManager = configurationManager;
        _dataManager = dataManager;
        _commandManager = commandManager;
        _logger = logger;
        _localizationManager = localizationManager;
        _dataManager.ModuleConfigurationManager.LoadConfiguration(CONFIG_FILE_NAME, ref _config);
        _coreModule = new ModuleManifest<CoreModule>(this, CoreModule.INTERNAL_NAME);
        _availableModules =
        [
            new ModuleManifest<LootMasterModule>(this, LootMasterModule.INTERNAL_NAME),
            new ModuleManifest<CalendarModule>(this, CalendarModule.INTERNAL_NAME)
            {
                CanBeDisabled = true,
            },
        ];
    }

    internal void UpdateConfiguration(Dictionary<IModuleManifest, bool> moduleConfigurationUpdate)
    {
        foreach ((var manifest, bool enabledNew) in moduleConfigurationUpdate)
        {
            if (manifest.Enabled == enabledNew) continue;
            if (!TryGetModuleManifestInternal(manifest.InternalName, out var internalManifest)) continue;
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
        foreach (var moduleManifest in _availableModules)
        {
            yield return moduleManifest;
        }
    }

    public bool TryGetModuleManifest(string internalName, [NotNullWhen(true)] out IModuleManifest? module)
    {
        module = null;
        if (!TryGetModuleManifestInternal(internalName, out var moduleInternal)) return false;
        module = moduleInternal;
        return true;
    }

    private bool TryGetModuleManifestInternal(string internalName,
                                              [NotNullWhen(true)] out IInternalModuleManifest? module)
    {
        module = _availableModules.FirstOrDefault(m => m?.InternalName == internalName, null);
        return module != null;
    }

    internal void LoadModules()
    {
        //Ensure core module is loaded first
        _coreModule.Enable();
        foreach (var moduleManifest in _availableModules)
        {
            _config.ModuleEnabled.TryAdd(moduleManifest.InternalName, true);
            if (_config.ModuleEnabled[moduleManifest.InternalName])
                moduleManifest.Enable();
        }
    }

    private void RemoveCommand(HrtCommand command)
    {
        if (_dalamudRegisteredCommands.Contains(command.Command))
            _commandManager.RemoveHandler(command.Command);
        foreach (string altCommand in command.AltCommands)
        {
            if (_dalamudRegisteredCommands.Contains(altCommand))
                _commandManager.RemoveHandler(altCommand);
        }
        _coreModule.Module?.RemoveCommand(command);
    }

    private void AddCommand(HrtCommand command)
    {
        if (command.ShouldExposeToDalamud)
        {
            if (_commandManager.AddHandler(command.Command,
                                           new CommandInfo(command.OnCommand)
                                           {
                                               HelpMessage = command.Description,
                                               ShowInHelp = command.ShowInHelp,
                                           }))
                _dalamudRegisteredCommands.Add(command.Command);

            if (command.ShouldExposeAltsToDalamud)
                foreach (string alt in command.AltCommands)
                {
                    if (_commandManager.AddHandler(alt,
                                                   new CommandInfo(command.OnCommand)
                                                   {
                                                       HelpMessage = command.Description,
                                                       ShowInHelp = false,
                                                   }))
                        _dalamudRegisteredCommands.Add(alt);
                }
        }

        _coreModule.Module?.AddCommand(command);
    }
    public void Dispose()
    {
        foreach (string command in _dalamudRegisteredCommands)
        {
            _commandManager.RemoveHandler(command);
        }
        _dataManager.ModuleConfigurationManager.SaveConfiguration(CONFIG_FILE_NAME, _config);
        foreach (var module in _availableModules)
        {
            module.Unload();
        }
        _coreModule.Unload();
    }

    private interface IInternalModuleManifest : IModuleManifest
    {
        public void Enable();

        public void Disable();

        internal void Load();

        internal void Unload();
    }

    private class ModuleManifest<TModule>(ModuleManager parent, string internalName)
        : IInternalModuleManifest where TModule : class, IHrtModule
    {
        public string InternalName => internalName;
        internal TModule? Module { get; private set; } = null;

        public Type Type => typeof(TModule);

        public bool CanBeDisabled { get; init; } = false;

        public bool Enabled { get; private set; } = false;

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
                if (Activator.CreateInstance(moduleType) is not TModule module)
                    throw new FailedToLoadException($"Failed to load module: {moduleType.Name}");
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

        public void AfterLoad(HrtDataManager dataManager) { }

        public void BeforeSave() { }
    }


}

public interface IModuleManifest
{
    public Type Type { get; }

    public string InternalName { get; }

    public bool CanBeDisabled { get; }

    public bool Enabled { get; }

    public bool Loaded { get; }

}