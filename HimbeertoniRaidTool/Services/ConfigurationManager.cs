using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;

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
        _services.Logger.Debug("Registered {ConfigParentInternalName} config", config.ParentInternalName);
        return config.Load(_services.HrtDataManager.ModuleConfigurationManager);
    }

    internal bool TryGetConfig<T>(Type type, [NotNullWhen(true)] out T? config) where T : class, IHrtConfiguration
    {
        config = null;
        if (_configurations.TryGetValue(type, out var configInner))
            config = configInner as T;
        return config != null;
    }

    internal void Save()
    {
        foreach (var config in _configurations.Values)
        {
            _services.Logger.Debug("Saved {ConfigParentInternalName} config", config.ParentInternalName);
            config.Save(_services.HrtDataManager.ModuleConfigurationManager);
        }
    }

    private class ConfigUi : HrtWindow
    {
        private readonly ConfigurationManager _configManager;
        private readonly Dictionary<IModuleManifest, bool> _availableModules = new();

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
            _availableModules.Clear();
            foreach (var manifest in _configManager._services.ModuleManager.GetAvailableModules())
            {
                _availableModules.Add(manifest, manifest.Enabled);
            }
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
            using var tabBar = ImRaii.TabBar("Modules");
            foreach (var moduleManifest in _availableModules.Keys)
            {
                using var tabItem = ImRaii.TabItem($"{moduleManifest.Name}##{moduleManifest.InternalName}");
                if (!tabItem)
                    continue;
                ImGui.Text(moduleManifest.Description);
                using (ImRaii.Disabled(!moduleManifest.CanBeDisabled))
                {
                    bool enabled = _availableModules[moduleManifest];
                    if (ImGui.Checkbox($"Enabled##{moduleManifest.InternalName}", ref enabled))
                        _availableModules[moduleManifest] = enabled;
                }
                var c = _configManager._configurations.Values.FirstOrDefault(
                    config => config?.ParentInternalName == moduleManifest.InternalName, null);
                c?.Ui?.Draw();
            }
        }

        private void Save()
        {
            foreach (var c in _configManager._configurations.Values)
            {
                c.Ui?.Save();
            }
            _configManager._services.ModuleManager.UpdateConfiguration(_availableModules);
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

internal abstract class ModuleConfiguration<TData, TModule>(TModule module) : IHrtConfiguration
    where TData : IHrtConfigData, new() where TModule : IHrtModule
{
    private TData _data = new();
    protected readonly TModule Module = module;

    public TData Data
    {
        get => _data;
        protected set
        {
            _data = value;
            OnConfigChange?.Invoke();
        }
    }

    public string ParentInternalName => TModule.InternalName;
    public string ParentName => TModule.Name;
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

public interface IHrtConfigData<out T> : IHrtConfigData, ICloneable<T>;

public interface IHrtConfigData
{
    public void AfterLoad(HrtDataManager dataManager);
    public void BeforeSave();
}