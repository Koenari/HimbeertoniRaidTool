using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Core;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class ModuleManager(ILogger logger, ConfigurationManager configurationManager, ICommandManager commandManager)
{

    private readonly Dictionary<Type, IHrtModule> _registeredModules = [];
    private readonly List<string> _dalamudRegisteredCommands = [];

    internal void LoadAllModules()
    {
        var coreModule = new CoreModule();
        //Ensure core module is loaded first
        RegisterModule(coreModule, coreModule.AddCommand);
        //Look for all classes in Modules namespace that implement the IHrtModule interface
        foreach (var moduleType in GetType().Assembly.GetTypes().Where(
                     t => (t.Namespace?.StartsWith($"{typeof(HrtPlugin).Namespace}.Modules") ?? false)
                       && t is { IsInterface: false, IsAbstract: false }
                       && t.GetInterfaces().Any(i => i == typeof(IHrtModule))))
        {
            if (moduleType == typeof(CoreModule)) continue;
            try
            {
                logger.Debug($"Creating instance of: {moduleType.Name}");
                if (Activator.CreateInstance(moduleType) is not IHrtModule module)
                    throw new FailedToLoadException($"Failed to load module: {moduleType.Name}");
                RegisterModule(module, coreModule.AddCommand);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to load module: {moduleType.Name}");
            }
        }
    }

    private void RegisterModule(IHrtModule module, Action<HrtCommand> registerCommand)
    {
        if (_registeredModules.ContainsKey(module.GetType()))
        {
            logger.Error($"Tried to register module \"{module.Name}\" twice");
            return;
        }

        try
        {
            logger.Debug($"Registering module \"{module.Name}\"");
            _registeredModules.Add(module.GetType(), module);
            foreach (var command in module.Commands)
            {
                AddCommand(command, registerCommand);
            }
            if (configurationManager.RegisterConfig(module.Configuration))
                module.Configuration.AfterLoad();
            else
                logger.Error($"Configuration load error:{module.Name}");
            logger.Debug($"Calling {module.InternalName}.AfterFullyLoaded()");
            module.AfterFullyLoaded();
            logger.Information($"Successfully loaded module: {module.Name}");
        }
        catch (Exception e)
        {
            _registeredModules.Remove(module.GetType());
            logger.Error(e, $"Error loading module: {module.GetType()}");
        }
    }

    private void AddCommand(HrtCommand command, Action<HrtCommand> registerCommand)
    {
        if (command.ShouldExposeToDalamud)
        {
            if (commandManager.AddHandler(command.Command,
                                           new CommandInfo(command.OnCommand)
                                           {
                                               HelpMessage = command.Description,
                                               ShowInHelp = command.ShowInHelp,
                                           }))
                _dalamudRegisteredCommands.Add(command.Command);

            if (command.ShouldExposeAltsToDalamud)
                foreach (string alt in command.AltCommands)
                {
                    if (commandManager.AddHandler(alt,
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

    public void OnLanguageChange(string languageCode)
    {
        foreach (var module in _registeredModules.Values)
        {
            module.OnLanguageChange(languageCode);
        }
    }

    public void Dispose()
    {
        foreach (string command in _dalamudRegisteredCommands)
        {
            commandManager.RemoveHandler(command);
        }
        foreach (var (type, module) in _registeredModules)
        {
            try
            {
                module.Dispose();
                module.Services.Dispose();
            }
            catch (Exception e)
            {
                logger.Fatal($"Unable to Dispose module \"{type}\"\n{e}");
            }
        }
    }

}

public interface IModuleStatus
{
    public bool Enabled { get; }
    public bool Loaded { get; }
}