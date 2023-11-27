using Dalamud.Game.Command;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Core;

namespace HimbeertoniRaidTool.Plugin;

// ReSharper disable once UnusedMember.Global
public sealed class HrtPlugin : IDalamudPlugin
{
    private readonly Configuration _configuration;
    private readonly ICommandManager _commandManager;
    private const string NAME = "Himbeertoni Raid Tool";

    private readonly bool _loadError;

    private readonly List<string> _dalamudRegisteredCommands = new();
    private readonly Dictionary<Type, IHrtModule> _registeredModules = new();

    public HrtPlugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface, ICommandManager commandManager)
    {
        _commandManager = commandManager;
        //Init all services
        _loadError = !ServiceManager.Init(pluginInterface);
        //Init Localization
        Localization.Init(pluginInterface);
        ServiceManager.Config = _configuration =
            ServiceManager.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (!_loadError)
        {
            //Load and update/correct configuration + ConfigUi
            _configuration.AfterLoad();
            LoadAllModules();
        }
        else
        {
            pluginInterface.UiBuilder.AddNotification(
                NAME + " did not load correctly. Please disable/enable to try again", "Error in HRT",
                NotificationType.Error, 10000);
            ServiceManager.ChatGui.PrintError(NAME + " did not load correctly. Please disable/enable to try again");
        }
    }

    private void LoadAllModules()
    {
        //Ensure core module is loaded first
        RegisterModule(new CoreModule());
        string moduleNamespace = $"{GetType().Namespace}.Modules";
        //Look for all classes in Modules namespace that implement the IHrtModule interface
        foreach (Type moduleType in GetType().Assembly.GetTypes().Where(
                     t => (t.Namespace?.StartsWith(moduleNamespace) ?? false)
                          && t is { IsInterface: false, IsAbstract: false }
                          && t.GetInterfaces().Any(i => i == typeof(IHrtModule))))
        {
            if (moduleType == typeof(CoreModule)) continue;
            try
            {
                var module = (IHrtModule?)Activator.CreateInstance(moduleType);
                if (module is null)
                {
                    ServiceManager.PluginLog.Error($"Could not create module: {moduleType.Name}");
                    continue;
                }

                RegisterModule(module);
            }
            catch (Exception e)
            {
                ServiceManager.PluginLog.Error(e, $"Failed to load module: {moduleType.Name}");
            }
        }
    }

    private void RegisterModule(IHrtModule module)
    {
        if (_registeredModules.ContainsKey(module.GetType()))
        {
            ServiceManager.PluginLog.Error($"Tried to register module \"{module.Name}\" twice");
            return;
        }

        try
        {

            _registeredModules.Add(module.GetType(), module);
            foreach (HrtCommand command in module.Commands)
                AddCommand(command);
            if (_configuration.RegisterConfig(module.Configuration))
                module.Configuration.AfterLoad();
            else
                ServiceManager.PluginLog.Error($"Configuration load error:{module.Name}");
            ServiceManager.PluginInterface.UiBuilder.Draw += module.WindowSystem.Draw;
            module.AfterFullyLoaded();
            ServiceManager.PluginLog.Information($"Successfully loaded module: {module.Name}");
        }
        catch (Exception e)
        {
            _registeredModules.Remove(module.GetType());
            HandleError(e);
        }

        void HandleError(Exception? e = null)
        {
            ServiceManager.PluginLog.Error(e, $"Error loading module: {module.GetType()}");
        }
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
                    if (_commandManager.AddHandler(alt,
                            new CommandInfo(command.OnCommand)
                            {
                                HelpMessage = command.Description,
                                ShowInHelp = false,
                            }))
                        _dalamudRegisteredCommands.Add(alt);
        }

        ServiceManager.CoreModule.AddCommand(command);
    }

    public void Dispose()
    {
        foreach (string command in _dalamudRegisteredCommands)
            _commandManager.RemoveHandler(command);
        if (!_loadError)
        {
            _configuration.Save(false);
            ServiceManager.HrtDataManager.Save();
        }

        foreach ((Type type, IHrtModule module) in _registeredModules)
            try
            {
                ServiceManager.PluginInterface.UiBuilder.Draw -= module.WindowSystem.Draw;
                module.WindowSystem.RemoveAllWindows();
                module.Dispose();
            }
            catch (Exception e)
            {
                ServiceManager.PluginLog.Fatal($"Unable to Dispose module \"{type}\"\n{e}");
            }

        Localization.Dispose();
        _configuration.Dispose();
        ServiceManager.Dispose();
    }
}