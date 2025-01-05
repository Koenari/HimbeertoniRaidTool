using System.Numerics;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using ICloneable = HimbeertoniRaidTool.Common.Data.ICloneable;

namespace HimbeertoniRaidTool.Plugin.Services;

public class ConfigurationManager : IDisposable
{
    private readonly Dictionary<Type, IHrtConfiguration> _configurations = new();
    private readonly ConfigUi _ui;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IGlobalServiceContainer _services;

    internal ConfigurationManager(IDalamudPluginInterface pluginInterface, IGlobalServiceContainer services)
    {
        _pluginInterface = pluginInterface;
        _services = services;
        _ui = new ConfigUi(this);
        _services.UiSystem.AddWindow(_ui);
        _pluginInterface.UiBuilder.OpenConfigUi += Show;
    }

    public void Dispose() => _pluginInterface.UiBuilder.OpenConfigUi -= Show;

    internal void Show() => _ui.Show();

    internal bool RegisterConfig(IHrtConfiguration config)
    {
        if (_configurations.ContainsKey(config.GetType()))
            return false;
        _configurations.Add(config.GetType(), config);
        _services.Logger.Debug($"Registered {config.ParentInternalName} config");
        return config.Load(_services.HrtDataManager.ModuleConfigurationManager);
    }

    internal void Save()
    {
        foreach (var config in _configurations.Values)
        {
            _services.Logger.Debug($"Saved {config.ParentInternalName} config");
            config.Save(_services.HrtDataManager.ModuleConfigurationManager);
        }
    }

    private class ConfigUi : HrtWindow
    {
        private readonly ConfigurationManager _configManager;

        public ConfigUi(ConfigurationManager configManager) : base(configManager._services.UiSystem,
                                                                   "HimbeerToniRaidToolConfiguration")
        {
            _configManager = configManager;
            (Size, SizeCondition) = (new Vector2(450, 500), ImGuiCond.Appearing);
            Flags = ImGuiWindowFlags.NoCollapse;
            Title = GeneralLoc.ConfigUi_Title;
            IsOpen = false;
            Persistent = true;
        }

        public override void OnOpen()
        {
            foreach (var config in _configManager._configurations.Values)
            {
                config.Ui?.OnShow();
            }
        }

        public override void OnClose()
        {
            foreach (var config in _configManager._configurations.Values)
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
            foreach (var c in _configManager._configurations.Values)
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
            foreach (var c in _configManager._configurations.Values)
            {
                c.Ui?.Save();
            }
            _configManager.Save();
            Hide();
        }

        private void Cancel()
        {
            foreach (var c in _configManager._configurations.Values)
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
    protected readonly IHrtModule Module = module;

    public T Data
    {
        get => _data;
        set
        {
            _data = value;
            OnConfigChange?.Invoke();
        }
    }

    public string ParentInternalName => Module.InternalName;
    public string ParentName => Module.Name;
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
    public void AfterLoad(HrtDataManager dataManager);
    public void BeforeSave();
}