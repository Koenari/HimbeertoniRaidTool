using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using ICloneable = HimbeertoniRaidTool.Common.Data.ICloneable;

namespace HimbeertoniRaidTool.Plugin;

public class Configuration : IPluginConfiguration, IDisposable
{
    private readonly Dictionary<Type, IHrtConfiguration> _configurations = new();
    private readonly int _targetVersion = 5;
    private readonly ConfigUi _ui;
    private bool _fullyLoaded;

    public Configuration()
    {
        _ui = new ConfigUi(this);
    }

    public void Dispose() => _ui.Dispose();
    public int Version { get; set; } = 5;

    internal void AfterLoad()
    {
        if (_fullyLoaded)
            return;
        if (Version < 5)
            Version = 5;
        _fullyLoaded = true;
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

    internal void Save(bool saveAll = true)
    {
        if (Version == _targetVersion)
        {
            ServiceManager.PluginInterface.SavePluginConfig(this);
            if (!saveAll)
                return;
            foreach (IHrtConfiguration config in _configurations.Values)
            {
                ServiceManager.PluginLog.Debug($"Saved {config.ParentInternalName} config");
                config.Save(ServiceManager.HrtDataManager.ModuleConfigurationManager);
            }
        }
        else
        {
            ServiceManager.PluginLog.Error("Configuration Version mismatch. Did not Save!");
        }
    }

    public class ConfigUi : HrtWindow, IDisposable
    {
        private readonly Configuration _configuration;
        private readonly WindowSystem _windowSystem;

        public ConfigUi(Configuration configuration) : base("HimbeerToniRaidToolConfiguration")
        {
            _windowSystem = new WindowSystem("HRTConfig");
            _windowSystem.AddWindow(this);
            _configuration = configuration;
            ServiceManager.PluginInterface.UiBuilder.OpenConfigUi += Show;
            ServiceManager.PluginInterface.UiBuilder.Draw += _windowSystem.Draw;

            (Size, SizeCondition) = (new Vector2(450, 500), ImGuiCond.Appearing);
            Flags = ImGuiWindowFlags.NoCollapse;
            Title = GeneralLoc.ConfigUi_Title;
            IsOpen = false;
        }

        public void Dispose()
        {
            ServiceManager.PluginInterface.UiBuilder.OpenConfigUi -= Show;
            ServiceManager.PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        }

        public override void OnOpen()
        {
            foreach (IHrtConfiguration config in _configuration._configurations.Values)
            {
                config.Ui?.OnShow();
            }
        }

        public override void OnClose()
        {
            foreach (IHrtConfiguration config in _configuration._configurations.Values)
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
            foreach (IHrtConfiguration c in _configuration._configurations.Values)
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
            foreach (IHrtConfiguration c in _configuration._configurations.Values)
            {
                c.Ui?.Save();
            }
            _configuration.Save();
            Hide();
        }

        private void Cancel()
        {
            foreach (IHrtConfiguration c in _configuration._configurations.Values)
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