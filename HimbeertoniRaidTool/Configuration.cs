using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Game;
using Dalamud.Logging;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool
{
    [Serializable]
    public class Configuration : IPluginConfiguration, IDisposable
    {
        [JsonIgnore]
        private bool FullyLoaded = false;
        [JsonIgnore]
        private readonly int TargetVersion = 5;
        [JsonProperty]
        public int Version { get; set; } = 5;
        [JsonProperty]
        private ConfigData Data = new();
        [JsonIgnore]
        private TimeSpan _saveInterval;
        [JsonIgnore]
        private TimeSpan _timeSinceLastSave;
        [JsonIgnore]
        public bool HideOnZoneChange => Data.HideOnZoneChange;
        [JsonIgnore]
        public bool HideInBattle => Data.HideInCombat;
        [JsonIgnore]
        private readonly Dictionary<Type, dynamic> Configurations = new();
        [JsonIgnore]
        public readonly ConfigUI Ui;
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
            _saveInterval = TimeSpan.FromMinutes(Data.SaveIntervalMinutes);
            _timeSinceLastSave = TimeSpan.Zero;
            Services.Framework.Update += Update;
            FullyLoaded = true;
        }
        private void Update(Framework fw)
        {
            _timeSinceLastSave += fw.UpdateDelta;
            if (Data.SavePeriodically && _timeSinceLastSave > _saveInterval)
            {
                Services.HrtDataManager.Save();
                _timeSinceLastSave = TimeSpan.Zero;
            }

        }
        internal bool RegisterConfig<T, S>(HRTConfiguration<T, S> config) where T : new() where S : IHrtConfigUi
        {
            if (Configurations.ContainsKey(config.GetType()))
                return false;
            Configurations.Add(config.GetType(), config);
            return Services.HrtDataManager.ModuleConfigurationManager.LoadConfiguration(config.ParentInternalName, ref config.Data);
        }
        internal void Save(bool saveAll = true)
        {
            if (Version == TargetVersion)
            {
                Services.PluginInterface.SavePluginConfig(this);
                if (saveAll)
                    foreach (var config in Configurations.Values)
                        config.Save();
            }
            else
                PluginLog.LogError("Configuration Version mismatch. Did not Save!");
        }

        public void Dispose()
        {
            Ui.Dispose();
        }
        private class ConfigData
        {
            [JsonProperty]
            public bool SavePeriodically = true;
            [JsonProperty]
            public int SaveIntervalMinutes = 30;
            [JsonProperty]
            public bool HideOnZoneChange = true;
            [JsonProperty]
            public bool HideInCombat = true;
        }
        public class ConfigUI : Window
        {
            private readonly Configuration _configuration;
            private ConfigData _dataCopy;
            public ConfigUI(Configuration configuration) : base(false, "HimbeerToni Raid Tool Configuration")
            {
                _configuration = configuration;
                _dataCopy = _configuration.Data.Clone();
                Services.PluginInterface.UiBuilder.OpenConfigUi += Show;

                (Size, SizingCondition) = (new Vector2(450, 500), ImGuiCond.Always);
                WindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
                Title = Localize("ConfigWindowTitle", "HimbeerToni Raid Tool Configuration");
            }
            protected override void BeforeDispose()
            {
                Services.PluginInterface.UiBuilder.OpenConfigUi -= Show;
            }
            protected override void OnShow()
            {
                _dataCopy = _configuration.Data.Clone();
                foreach (dynamic config in _configuration.Configurations.Values)
                    try
                    {
                        if (config.Ui != null)
                            config.Ui.OnShow();
                    }
                    catch (Exception) { }
            }
            protected override void OnHide()
            {
                foreach (dynamic config in _configuration.Configurations.Values)
                    try
                    {
                        if (config.Ui != null)
                            config.Ui.OnHide();
                    }
                    catch (Exception) { }
            }
            protected override void Draw()
            {
                if (ImGuiHelper.SaveButton())
                    Save();
                ImGui.SameLine();
                if (ImGuiHelper.CancelButton())
                    Cancel();
                ImGui.BeginTabBar("Modules");
                if (ImGui.BeginTabItem(Localize("General", "General")))
                {
                    ImGui.Text(Localize("Ui", "User Interface"));
                    ImGui.Checkbox(Localize("HideInCombat", "Hide in combat"), ref _dataCopy.HideInCombat);
                    ImGui.SetTooltip(Localize("HideInCombatTooltip", "Hides all windows while character is in combat"));
                    ImGui.Checkbox(Localize("HideOnZoneChange", "Hide in loading screenst"), ref _dataCopy.HideOnZoneChange);
                    ImGui.SetTooltip(Localize("HideOnZoneChangeTooltip", "Hides all windows while in a loading screen"));
                    ImGui.Separator();
                    ImGui.Text(Localize("Auto Save", "Auto Save"));
                    ImGui.Checkbox(Localize("Save periodically", "Save periodically"), ref _dataCopy.SavePeriodically);
                    ImGui.SetTooltip(Localize("SavePeriodicallyTooltip", "Saves all data of this plugin periodically. (Helps prevent losing data if your game crashes)"));
                    ImGui.TextWrapped($"{Localize("AutoSave_interval_min", "AutoSave interval (min)")}:");
                    ImGui.SetNextItemWidth(150 * ScaleFactor);
                    if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
                    {
                        if (_dataCopy.SaveIntervalMinutes < 1)
                            _dataCopy.SaveIntervalMinutes = 1;
                    }
                    ImGui.EndTabItem();
                }
                foreach (dynamic c in _configuration.Configurations.Values)
                {
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
                    catch (Exception) { }

                }
                ImGui.EndTabBar();
            }
            private void Save()
            {
                _configuration.Data = _dataCopy;
                _configuration._saveInterval = TimeSpan.FromMinutes(_configuration.Data.SaveIntervalMinutes);
                foreach (dynamic c in _configuration.Configurations.Values)
                    if (c.Ui != null)
                        c.Ui.Save();
                _configuration.Save();
                Hide();
            }
            private void Cancel()
            {
                foreach (dynamic c in _configuration.Configurations.Values)
                    if (c.Ui != null)
                        c.Ui.Cancel();
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
            Services.HrtDataManager.ModuleConfigurationManager.SaveConfiguration(ParentInternalName, Data);
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
}
