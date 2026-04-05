using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

public class ConfigurationManager : IDisposable
{
    private readonly Dictionary<Type, IHrtModuleConfiguration> _configurations = new();
    private ConfigUi? _ui;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ILogger _logger;
    private readonly HrtDataManager _hrtDataManager;
    private readonly PeriodicTask _saveTask;
    internal CoreConfig CoreConfig { get; }

    internal ConfigurationManager(IDalamudPluginInterface pluginInterface, ILogger logger, TaskManager taskManager,
                                  HrtDataManager hrtDataManager)
    {
        _pluginInterface = pluginInterface;
        _logger = logger;
        _hrtDataManager = hrtDataManager;

        CoreConfig = new CoreConfig();
        if (CoreConfig.Load(hrtDataManager))
            CoreConfig.AfterLoad();
        _pluginInterface.UiBuilder.OpenConfigUi += Show;


        _saveTask = new PeriodicTask(() =>
                                     {
                                         if (hrtDataManager.Save())
                                             return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveSuccessful,
                                                                     HrtUiMessageType.Success);
                                         return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveFailed,
                                                                 HrtUiMessageType.Failure);
                                     }, logger.Write, "Automatic Save",
                                     TimeSpan.FromMinutes(CoreConfig.Data.SaveIntervalMinutes))
        {
            ShouldRun = CoreConfig.Data.SavePeriodically,
            Repeat = TimeSpan.FromMinutes(CoreConfig.Data.SaveIntervalMinutes),
            LastRun = DateTime.Now,
        };
        taskManager.RegisterTask(_saveTask);
        CoreConfig.OnConfigChange += UpdateTask;
    }

    internal void InitUi(IUiSystem uiSystem)
    {
        _ui = new ConfigUi(this, uiSystem);
        uiSystem.AddWindow(_ui);
    }

    private void UpdateTask()
    {
        _saveTask.ShouldRun = CoreConfig.Data.SavePeriodically;
        _saveTask.Repeat = TimeSpan.FromMinutes(CoreConfig.Data.SaveIntervalMinutes);
        _saveTask.LastRun = DateTime.Now;
    }




    public void Dispose()
    {
        CoreConfig.OnConfigChange -= UpdateTask;
        _pluginInterface.UiBuilder.OpenConfigUi -= Show;
        Save();
    }

    internal void Show() => _ui?.Show();

    internal bool RegisterConfig(IHrtModuleConfiguration config)
    {
        if (_configurations.ContainsKey(config.GetType()))
            return false;
        _configurations.Add(config.GetType(), config);
        _logger.Debug("Registered {ConfigParentInternalName} config", config.ParentInternalName);
        return config.Load(_hrtDataManager);
    }

    internal bool TryGetConfig<TConfig>([NotNullWhen(true)] out TConfig? config)
        where TConfig : class, IHrtConfiguration
    {
        config = null;
        if (_configurations.TryGetValue(typeof(TConfig), out var configInner))
            config = configInner as TConfig;
        return config != null;
    }

    internal void Save()
    {
        _logger.Debug("Saved {ConfigParentInternalName} config", CoreConfig.ParentInternalName);
        CoreConfig.Save(_hrtDataManager);
        foreach (var config in _configurations.Values)
        {
            _logger.Debug("Saved {ConfigParentInternalName} config", config.ParentInternalName);
            config.Save(_hrtDataManager);
        }
    }

    private class ConfigUi : HrtWindow
    {
        private readonly ConfigurationManager _configManager;

        public ConfigUi(ConfigurationManager configManager, IUiSystem uiSystem) : base(uiSystem,
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
            using var tabBar = ImRaii.TabBar("Modules");

            {
                var configuration = _configManager.CoreConfig;
                using var tabItem = ImRaii.TabItem($"General##{configuration.ParentInternalName}");
                if (tabItem)
                    (configuration as IHrtConfiguration).Ui?.Draw();
            }
            foreach (var configuration in _configManager._configurations.Values)
            {
                using var tabItem = ImRaii.TabItem($"{configuration.ModuleName}##{configuration.ParentInternalName}");
                if (!tabItem)
                    continue;
                ImGui.Text(configuration.ModuleDescription);
                using (ImRaii.Disabled(!configuration.ModuleCanBeDisabled))
                {
                    bool enabled =
                        _configManager.CoreConfig.Data.ModulesEnabled.TryAdd(configuration.ParentInternalName, true)
                     || _configManager.CoreConfig.Data.ModulesEnabled[configuration.ParentInternalName];
                    if (ImGui.Checkbox($"Enabled##{configuration.ParentInternalName}", ref enabled))
                        _configManager.CoreConfig.Data.ModulesEnabled[configuration.ParentInternalName] = enabled;
                }
                configuration.Ui?.Draw();
            }
        }

        private void Save()
        {
            _configManager.CoreConfig.Ui?.Save();
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
    string ParentInternalName { get; }
    IHrtConfigUi? Ui { get; }

    event Action? OnConfigChange;
    internal bool Load(HrtDataManager configFileManager);
    internal bool Save(HrtDataManager configFileManager);
    void AfterLoad();
}

internal abstract class ModuleConfiguration<TData, TModule, TUi>(IModuleServiceContainer serviceContainer)
    : Configuration<TData, TUi>(TModule.InternalName), IHrtModuleConfiguration
    where TData : IHrtConfigData, new() where TModule : IHrtModule where TUi : class, IHrtConfigUi
{
    protected IModuleServiceContainer Services => serviceContainer;
    public string ModuleName => TModule.Name;
    public string ModuleDescription => TModule.Description;
    public bool ModuleCanBeDisabled => TModule.CanBeDisabled;
}

internal interface IHrtModuleConfiguration : IHrtConfiguration
{
    string ModuleName { get; }
    string ModuleDescription { get; }
    bool ModuleCanBeDisabled { get; }
}

internal abstract class Configuration<TData, TUi>(string internalName) : IHrtConfiguration
    where TData : IHrtConfigData, new() where TUi : class, IHrtConfigUi
{
    private TData _data = new();

    public TData Data
    {
        get => _data;
        protected set
        {
            _data = value;
            OnConfigChange?.Invoke();
        }
    }

    public string ParentInternalName => internalName;
    public TUi? Ui { get; init; }
    IHrtConfigUi? IHrtConfiguration.Ui => Ui;

    public event Action? OnConfigChange;
    public bool Load(HrtDataManager hrtDataManager) =>
        hrtDataManager.LoadConfiguration(ParentInternalName, ref _data);

    public bool Save(HrtDataManager hrtDataManager) =>
        hrtDataManager.SaveConfiguration(ParentInternalName, _data);

    public virtual void AfterLoad() { }
}

public interface IHrtConfigUi
{
    void OnShow();
    void Draw();
    void OnHide();
    void Save();
    void Cancel();
}

public interface IHrtConfigData<out T> : IHrtConfigData, ICloneable<T>;

public interface IHrtConfigData
{
    void AfterLoad();
    void BeforeSave();
}