using System.Globalization;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Core;

namespace HimbeertoniRaidTool.Plugin;

// ReSharper disable once UnusedMember.Global
public sealed class HrtPlugin : IDalamudPlugin
{
    private readonly ICommandManager _commandManager;

    private readonly List<string> _dalamudRegisteredCommands = [];
    private readonly Dictionary<Type, IHrtModule> _registeredModules = new();
    private readonly IGlobalServiceContainer _services;

    public HrtPlugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
    {
        _commandManager = commandManager;
        GeneralLoc.Culture = new CultureInfo(pluginInterface.UiLanguage);
        //Init all services
        _services = ServiceManager.Init(pluginInterface);
        LoadAllModules();
        //Init Localization
        pluginInterface.LanguageChanged += OnLanguageChange;
        OnLanguageChange(pluginInterface.UiLanguage);
    }

    public void Dispose()
    {
        foreach (string command in _dalamudRegisteredCommands)
        {
            _commandManager.RemoveHandler(command);
        }
        _services.ConfigManager.Save();
        _services.HrtDataManager.Save();

        foreach (var (type, module) in _registeredModules)
        {
            try
            {
                _services.PluginInterface.UiBuilder.Draw -= module.WindowSystem.Draw;
                module.WindowSystem.RemoveAllWindows();
                module.Dispose();
            }
            catch (Exception e)
            {
                _services.Logger.Fatal($"Unable to Dispose module \"{type}\"\n{e}");
            }
        }
        _services.Dispose();
    }

    private void LoadAllModules()
    {
        var coreModule = new CoreModule();
        //Ensure core module is loaded first
        RegisterModule(coreModule, coreModule.AddCommand);
        //Look for all classes in Modules namespace that implement the IHrtModule interface
        foreach (var moduleType in GetType().Assembly.GetTypes().Where(
                     t => (t.Namespace?.StartsWith($"{GetType().Namespace}.Modules") ?? false)
                       && t is { IsInterface: false, IsAbstract: false }
                       && t.GetInterfaces().Any(i => i == typeof(IHrtModule))))
        {
            if (moduleType == typeof(CoreModule)) continue;
            try
            {
                _services.Logger.Debug($"Creating instance of: {moduleType.Name}");
                if (Activator.CreateInstance(moduleType) is not IHrtModule module)
                    throw new FailedToLoadException($"Failed to load module: {moduleType.Name}");
                RegisterModule(module, coreModule.AddCommand);
            }
            catch (Exception e)
            {
                _services.Logger.Error(e, $"Failed to load module: {moduleType.Name}");
            }
        }
    }

    private void RegisterModule(IHrtModule module, Action<HrtCommand> registerCommand)
    {
        if (_registeredModules.ContainsKey(module.GetType()))
        {
            _services.Logger.Error($"Tried to register module \"{module.Name}\" twice");
            return;
        }

        try
        {
            _services.Logger.Debug($"Registering module \"{module.Name}\"");
            _registeredModules.Add(module.GetType(), module);
            foreach (var command in module.Commands)
            {
                AddCommand(command, registerCommand);
            }
            if (_services.ConfigManager.RegisterConfig(module.Configuration))
                module.Configuration.AfterLoad();
            else
                _services.Logger.Error($"Configuration load error:{module.Name}");
            _services.PluginInterface.UiBuilder.Draw += module.WindowSystem.Draw;
            _services.Logger.Debug($"Calling {module.InternalName}.AfterFullyLoaded()");
            module.AfterFullyLoaded();
            _services.Logger.Information($"Successfully loaded module: {module.Name}");
        }
        catch (Exception e)
        {
            _registeredModules.Remove(module.GetType());
            _services.Logger.Error(e, $"Error loading module: {module.GetType()}");
        }
    }

    private void AddCommand(HrtCommand command, Action<HrtCommand> registerCommand)
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

        registerCommand(command);
    }

    private void OnLanguageChange(string languageCode)
    {
        _services.Logger.Information($"Loading Localization for {languageCode}");
        Common.Services.ServiceManager.SetLanguage(languageCode);
        try
        {
            var newLanguage = new CultureInfo(languageCode);
            GeneralLoc.Culture = newLanguage;
            foreach (var module in _registeredModules.Values)
            {
                module.OnLanguageChange(languageCode);
            }
        }
        catch (Exception ex)
        {
            _services.Logger.Error(ex, "Unable to Load Localization");
        }
    }
}