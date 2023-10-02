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
    private bool FullyLoaded = false;
    private readonly int TargetVersion = 5;
    public int Version { get; set; } = 5;
    private readonly Dictionary<Type, dynamic> Configurations = new();
    private readonly ConfigUI Ui;

    public Configuration()
    {
        Ui = new ConfigUI(this);
    }

    internal void AfterLoad()
    {
        if (FullyLoaded)
            return;
        if (Version < 5)
            Version = 5;
        FullyLoaded = true;
    }

    internal void Show()
    {
        Ui.Show();
    }

    internal bool RegisterConfig<T, S>(HRTConfiguration<T, S> config) where T : new() where S : IHrtConfigUi
    {
        if (Configurations.ContainsKey(config.GetType()))
            return false;
        Configurations.Add(config.GetType(), config);
        return ServiceManager.HrtDataManager.ModuleConfigurationManager.LoadConfiguration(config.ParentInternalName,
            ref config.Data);
    }

    internal void Save(bool saveAll = true)
    {
        if (Version == TargetVersion)
        {
            ServiceManager.PluginInterface.SavePluginConfig(this);
            if (saveAll)
                foreach (dynamic? config in Configurations.Values)
                    config.Save();
        }
        else
        {
            ServiceManager.PluginLog.Error("Configuration Version mismatch. Did not Save!");
        }
    }

    public void Dispose()
    {
        Ui.Dispose();
    }

    public class ConfigUI : HrtWindow, IDisposable
    {
        private readonly WindowSystem _windowSystem;
        private readonly Configuration _configuration;

        public ConfigUI(Configuration configuration) : base("HimbeerToniRaidToolConfiguration")
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
            foreach (dynamic config in _configuration.Configurations.Values)
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
            foreach (dynamic config in _configuration.Configurations.Values)
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
            foreach (dynamic c in _configuration.Configurations.Values)
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
            foreach (dynamic c in _configuration.Configurations.Values)
                c.Ui?.Save();
            _configuration.Save();
            Hide();
        }

        private void Cancel()
        {
            foreach (dynamic c in _configuration.Configurations.Values)
                c.Ui?.Cancel();
            Hide();
        }
    }
}

public abstract class HRTConfiguration<T, S> where T : new() where S : IHrtConfigUi
{
    public readonly string ParentInternalName;
    public readonly string ParentName;
    public T Data = new();
    public abstract S? Ui { get; }

    public HRTConfiguration(string parentInternalName, string parentName)
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