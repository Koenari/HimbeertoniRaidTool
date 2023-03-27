using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Plugin.HrtServices;
using HimbeertoniRaidTool.Plugin.Modules;

namespace HimbeertoniRaidTool.Plugin;

public sealed class HRTPlugin : IDalamudPlugin
{
    private readonly Configuration _Configuration;
    public string Name => "Himbeertoni Raid Tool";

    private readonly bool LoadError = false;

    private readonly List<string> RegisteredCommands = new();
    private readonly Dictionary<Type, IHrtModule> RegisteredModules = new();

    public HRTPlugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
    {
        //Init all services
        LoadError = !Services.Init(pluginInterface);
        //Init Localization
        Localization.Init(pluginInterface);
        Services.Config = _Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (!LoadError)
        {
            //Load and update/correct configuration + ConfigUi
            _Configuration.AfterLoad();
            LoadAllModules();
        }
        else
        {
            pluginInterface.UiBuilder.AddNotification(Name + " did not load correctly. Please disbale/enable to try again", "Error in HRT", NotificationType.Error, 10000);
            Services.ChatGui.PrintError(Name + " did not load correctly. Please disbale/enable to try again");
        }
    }
    private void LoadAllModules()
    {
        string moduleNamespace = $"{GetType().Namespace}.Modules";
        //Look for all classes in Modules namespace that imlement the IHrtModule interface
        foreach (var moduleType in GetType().Assembly.GetTypes().Where(
            t => (t.Namespace?.StartsWith(moduleNamespace) ?? false)
            && !t.IsInterface && !t.IsAbstract
            && t.GetInterfaces().Any(i => i == typeof(IHrtModule))))
        {
            bool hasConfig = moduleType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHrtModule<,>));
            try
            {
                dynamic? module = Activator.CreateInstance(moduleType);
                if (module is null)
                {
                    PluginLog.Error($"Could not create module: {moduleType.Name}");
                    continue;
                }
                RegisterModule(module, hasConfig);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"Failed to load module: {moduleType.Name}");
            }
        }
    }
    private bool TryGetModule<T>([NotNullWhen(true)] out T? module) where T : class, IHrtModule
    {
        module = null;
        if (RegisteredModules.TryGetValue(typeof(T), out IHrtModule? value))
        {
            module = (T)value;
            return true;
        }
        return false;
    }

    private void RegisterModule(dynamic instance, bool hasConfig)
    {
        if (RegisteredModules.ContainsKey(instance.GetType()))
        {
            PluginLog.Error($"Tried to register module \"{instance.GetType()}\" twice");
            return;
        }
        try
        {
            IHrtModule? module = instance as IHrtModule;
            RegisteredModules.Add(module.GetType(), module);
            foreach (HrtCommand command in instance.Commands)
                AddCommand(command);
            if (hasConfig)
                if (!_Configuration.RegisterConfig(instance.Configuration))
                    PluginLog.Error($"Configuration load error:{module.Name}");
            Services.PluginInterface.UiBuilder.Draw += module.WindowSystem.Draw;
            instance.AfterFullyLoaded();
            PluginLog.Debug($"Succesfully loaded module: {module.Name}");
        }
        catch (Exception e)
        {
            if (RegisteredModules.ContainsKey(instance.GetType()))
                RegisteredModules.Remove(instance.GetType());
            PluginLog.Error(e, $"Error loading module: {instance.GetType()}\n {1}");
        }
    }

    private void AddCommand(HrtCommand command)
    {
        if (Services.CommandManager.AddHandler(command.Command,
            new CommandInfo((x, y) => command.OnCommand(y))
            {
                HelpMessage = command.Description,
                ShowInHelp = command.ShowInHelp
            }))
        { RegisteredCommands.Add(command.Command); }
    }
    public void Dispose()
    {
        RegisteredCommands.ForEach(command => Services.CommandManager.RemoveHandler(command));
        if (!LoadError)
        {
            _Configuration.Save(false);
            Services.HrtDataManager.Save();
        }
        foreach ((Type type, IHrtModule module) in RegisteredModules)
        {
            try
            {
                Services.PluginInterface.UiBuilder.Draw -= module.WindowSystem.Draw;
                module.WindowSystem.RemoveAllWindows();
                module.Dispose();
            }
            catch (Exception e)
            {
                PluginLog.Fatal($"Unable to Dispose module \"{type}\"\n{e}");
            }
        }
        Localization.Dispose();
        _Configuration.Dispose();
        Services.Dispose();
    }
}
