using System.Globalization;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Core;

namespace HimbeertoniRaidTool.Plugin;

// ReSharper disable once UnusedMember.Global
public sealed class HrtPlugin : IDalamudPlugin
{
    private const string NAME = "Himbeertoni Raid Tool";
    private readonly ICommandManager _commandManager;

    private readonly List<string> _dalamudRegisteredCommands = [];
    private readonly bool _loadedSuccessfully;
    private readonly Dictionary<Type, IHrtModule> _registeredModules = new();

    public HrtPlugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager)
    {
        _commandManager = commandManager;
        GeneralLoc.Culture = new CultureInfo(pluginInterface.UiLanguage);
        //Init all services
        _loadedSuccessfully = ServiceManager.Init(pluginInterface);
        if (_loadedSuccessfully)
        {
            LoadAllModules();
        }
        else
        {
            ServiceManager.NotificationManager.AddNotification(new Notification
            {
                Content = NAME + " did not load correctly. Please disable/enable to try again",
                Title = "Error in HRT",
                InitialDuration = TimeSpan.FromSeconds(10),
                Type = NotificationType.Error,
            });
            ServiceManager.Chat.PrintError(NAME + " did not load correctly. Please disable/enable to try again");
        }
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
        if (_loadedSuccessfully)
        {
            ServiceManager.ConfigManager.Save();
            ServiceManager.HrtDataManager.Save();
        }

        foreach (var (type, module) in _registeredModules)
        {
            try
            {
                ServiceManager.PluginInterface.UiBuilder.Draw -= module.WindowSystem.Draw;
                module.WindowSystem.RemoveAllWindows();
                module.Dispose();
            }
            catch (Exception e)
            {
                ServiceManager.Logger.Fatal($"Unable to Dispose module \"{type}\"\n{e}");
            }
        }
        ServiceManager.Dispose();
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
                ServiceManager.Logger.Debug($"Creating instance of: {moduleType.Name}");
                if (Activator.CreateInstance(moduleType) is not IHrtModule module)
                    throw new FailedToLoadException($"Failed to load module: {moduleType.Name}");
                RegisterModule(module, coreModule.AddCommand);
            }
            catch (Exception e)
            {
                ServiceManager.Logger.Error(e, $"Failed to load module: {moduleType.Name}");
            }
        }
    }

    private void RegisterModule(IHrtModule module, Action<HrtCommand> registerCommand)
    {
        if (_registeredModules.ContainsKey(module.GetType()))
        {
            ServiceManager.Logger.Error($"Tried to register module \"{module.Name}\" twice");
            return;
        }

        try
        {
            ServiceManager.Logger.Debug($"Registering module \"{module.Name}\"");
            _registeredModules.Add(module.GetType(), module);
            foreach (var command in module.Commands)
            {
                AddCommand(command, registerCommand);
            }
            if (ServiceManager.ConfigManager.RegisterConfig(module.Configuration))
                module.Configuration.AfterLoad();
            else
                ServiceManager.Logger.Error($"Configuration load error:{module.Name}");
            ServiceManager.PluginInterface.UiBuilder.Draw += module.WindowSystem.Draw;
            ServiceManager.Logger.Debug($"Calling {module.InternalName}.AfterFullyLoaded()");
            module.AfterFullyLoaded();
            ServiceManager.Logger.Information($"Successfully loaded module: {module.Name}");
        }
        catch (Exception e)
        {
            _registeredModules.Remove(module.GetType());
            ServiceManager.Logger.Error(e, $"Error loading module: {module.GetType()}");
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
        ServiceManager.Logger.Information($"Loading Localization for {languageCode}");
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
            ServiceManager.Logger.Error(ex, "Unable to Load Localization");
        }
    }
}