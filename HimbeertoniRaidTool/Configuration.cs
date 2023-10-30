using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin;

public class Configuration : IPluginConfiguration, IDisposable
{
    private bool _fullyLoaded = false;
    private readonly int _targetVersion = 5;
    public int Version { get; set; } = 5;
    private readonly Dictionary<Type, dynamic> _configurations = new();
    private readonly ConfigUi _ui;

    public Configuration()
    {
        _ui = new ConfigUi(this);
    }

    internal void AfterLoad()
    {
        if (_fullyLoaded)
            return;
        if (Version < 5)
            Version = 5;
        _fullyLoaded = true;
    }

    internal void Show()
    {
        _ui.Show();
    }

    internal bool RegisterConfig<T, S>(HrtConfiguration<T, S> config) where T : new() where S : IHrtConfigUi
    {
        if (_configurations.ContainsKey(config.GetType()))
            return false;
        _configurations.Add(config.GetType(), config);
        return ServiceManager.HrtDataManager.ModuleConfigurationManager.LoadConfiguration(config.ParentInternalName,
            ref config.Data);
    }

    internal void Save(bool saveAll = true)
    {
        if (Version == _targetVersion)
        {
            ServiceManager.PluginInterface.SavePluginConfig(this);
            if (saveAll)
                foreach (dynamic? config in _configurations.Values)
                    config.Save();
        }
        else
        {
            ServiceManager.PluginLog.Error("Configuration Version mismatch. Did not Save!");
        }
    }

    public void Dispose()
    {
        _ui.Dispose();
    }

    public class ConfigUi : HrtWindow, IDisposable
    {
        private readonly WindowSystem _windowSystem;
        private readonly Configuration _configuration;

        public ConfigUi(Configuration configuration) : base("HimbeerToniRaidToolConfiguration")
        {
            _windowSystem = new WindowSystem("HRTConfig");
            _windowSystem.AddWindow(this);
            _configuration = configuration;
            ServiceManager.PluginInterface.UiBuilder.OpenConfigUi += Show;
            ServiceManager.PluginInterface.UiBuilder.Draw += _windowSystem.Draw;

            (Size, SizeCondition) = (new Vector2(450, 500), ImGuiCond.Appearing);
            Flags = ImGuiWindowFlags.NoCollapse;
            Title = Localize("ConfigWindowTitle", "HimbeerToni Raid Tool Configuration");
            IsOpen = false;
        }

        public void Dispose()
        {
            ServiceManager.PluginInterface.UiBuilder.OpenConfigUi -= Show;
            ServiceManager.PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        }

        public override void OnOpen()
        {
            foreach (dynamic config in _configuration._configurations.Values)
                try
                {
                    config.Ui?.OnShow();
                }
                catch (Exception)
                {
                }
        }

        public override void OnClose()
        {
            foreach (dynamic config in _configuration._configurations.Values)
                try
                {
                    config.Ui?.OnHide();
                }
                catch (Exception)
                {
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
            foreach (dynamic c in _configuration._configurations.Values)
                try
                {
                    if (c.Ui == null)
                        continue;
                    if (ImGui.BeginTabItem(c.ParentName))
                    {
                        c.Ui.Draw();
                        ImGui.EndTabItem();
                    }
                }
                catch (Exception)
                {
                }

            ImGui.EndTabBar();
        }

        private void Save()
        {
            foreach (dynamic c in _configuration._configurations.Values)
                c.Ui?.Save();
            _configuration.Save();
            Hide();
        }

        private void Cancel()
        {
            foreach (dynamic c in _configuration._configurations.Values)
                c.Ui?.Cancel();
            Hide();
        }
    }
}

public abstract class HrtConfiguration<T, S> where T : new() where S : IHrtConfigUi
{
    public readonly string ParentInternalName;
    public readonly string ParentName;
    public T Data = new();
    public abstract S? Ui { get; }

    public HrtConfiguration(string parentInternalName, string parentName)
    {
        ParentInternalName = parentInternalName;
        ParentName = parentName;
    }

    internal void Save()
    {
        ServiceManager.HrtDataManager.ModuleConfigurationManager.SaveConfiguration(ParentInternalName, Data);
    }

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