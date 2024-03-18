using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using ICloneable = HimbeertoniRaidTool.Common.Data.ICloneable;

namespace HimbeertoniRaidTool.Plugin;

public class ConfigurationManager : IDisposable
{
    private readonly Dictionary<Type, IHrtConfiguration> _configurations = new();
    private readonly ConfigUi _ui;
    private readonly WindowSystem _windowSystem;
    private readonly DalamudPluginInterface _pluginInterface;

    public ConfigurationManager(DalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        _windowSystem = new WindowSystem("HRTConfig");
        _ui = new ConfigUi(this);
        _windowSystem.AddWindow(_ui);
        _pluginInterface.UiBuilder.OpenConfigUi += Show;
        _pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
    }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.OpenConfigUi -= Show;
        _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
    }

    internal void Show() => _ui.Show();

    internal bool RegisterConfig(IHrtConfiguration config)
    {
        if (_configurations.ContainsKey(config.GetType()))
            return false;
        _configurations.Add(config.GetType(), config);
        ServiceManager.PluginLog.Debug($"Registered {config.ParentInternalName} config");
        return config.Load(ServiceManager.HrtDataManager.ModuleConfigurationManager);
    }

    internal void Save()
    {
        foreach (IHrtConfiguration config in _configurations.Values)
        {
            ServiceManager.PluginLog.Debug($"Saved {config.ParentInternalName} config");
            config.Save(ServiceManager.HrtDataManager.ModuleConfigurationManager);
        }
    }

    private class ConfigUi : HrtWindow
    {
        private readonly ConfigurationManager _configManager;

        public ConfigUi(ConfigurationManager configManager) : base("HimbeerToniRaidToolConfiguration")
        {

            _configManager = configManager;

            (Size, SizeCondition) = (new Vector2(450, 500), ImGuiCond.Appearing);
            Flags = ImGuiWindowFlags.NoCollapse;
            Title = GeneralLoc.ConfigUi_Title;
            IsOpen = false;
        }

        public override void OnOpen()
        {
            foreach (IHrtConfiguration config in _configManager._configurations.Values)
            {
                config.Ui?.OnShow();
            }
        }

        public override void OnClose()
        {
            foreach (IHrtConfiguration config in _configManager._configurations.Values)
            {
                config.Ui?.OnHide();
            }
        }

        public override void Draw()
        {
            if (ImGuiHelper.SaveButton())
                Save();
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
                Cancel();
            ImGui.BeginTabBar("Modules");
            foreach (IHrtConfiguration c in _configManager._configurations.Values)
            {
                if (c.Ui == null)
                    continue;
                if (ImGui.BeginTabItem(c.ParentName))
                {
                    c.Ui.Draw();
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        private void Save()
        {
            foreach (IHrtConfiguration c in _configManager._configurations.Values)
            {
                c.Ui?.Save();
            }
            _configManager.Save();
            Hide();
        }

        private void Cancel()
        {
            foreach (IHrtConfiguration c in _configManager._configurations.Values)
            {
                c.Ui?.Cancel();
            }
            Hide();
        }
    }
}

public interface IHrtConfiguration
{
    public string ParentInternalName { get; }
    public string ParentName { get; }
    public IHrtConfigUi? Ui { get; }

    public event Action? OnConfigChange;
    internal bool Load(IModuleConfigurationManager configManager);
    internal bool Save(IModuleConfigurationManager configManager);
    public void AfterLoad();
}

internal abstract class ModuleConfiguration<T>(IHrtModule module) : IHrtConfiguration
    where T : IHrtConfigData, new()
{
    private T _data = new();

    public T Data
    {
        get => _data;
        set
        {
            _data = value;
            OnConfigChange?.Invoke();
        }
    }

    public string ParentInternalName => module.InternalName;
    public string ParentName => module.Name;
    public abstract IHrtConfigUi? Ui { get; }

    public event Action? OnConfigChange;
    public bool Load(IModuleConfigurationManager configManager) =>
        configManager.LoadConfiguration(ParentInternalName, ref _data);

    public bool Save(IModuleConfigurationManager configManager) =>
        configManager.SaveConfiguration(ParentInternalName, _data);

    public abstract void AfterLoad();
}

public interface IHrtConfigUi
{
    public void OnShow();
    public void Draw();
    public void OnHide();
    public void Save();
    public void Cancel();
}

public interface IHrtConfigData : ICloneable
{
    public void AfterLoad();
    public void BeforeSave();
}