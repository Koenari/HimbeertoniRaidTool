using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal sealed class CoreConfig : HrtConfiguration<CoreConfig.ConfigData, CoreConfig.ConfigUi>
{
    private readonly PeriodicTask _saveTask;
    public override ConfigUi Ui { get; }
    private const int TARGET_VERSION = 1;
    public CoreConfig(CoreModule module) : base(module.InternalName, Localize("General", "General"))
    {
        Ui = new ConfigUi(this);
        _saveTask = new PeriodicTask(PeriodicSave, module.HandleMessage, "Automatic Save",
            TimeSpan.FromMinutes(Data.SaveIntervalMinutes))
        {
            ShouldRun = false,
        };
    }

    public override void AfterLoad()
    {
        if (Data.Version > TARGET_VERSION)
        {
            const string msg = "Tried loading a configuration from a newer version of the plugin." +
                               "\nTo prevent data loss operation has been stopped.\nYou need to update to use this plugin!";
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
        }
        Upgrade();
        _saveTask.Repeat = TimeSpan.FromMinutes(Data.SaveIntervalMinutes);
        _saveTask.ShouldRun = Data.SavePeriodically;
        ServiceManager.TaskManager.RegisterTask(_saveTask);
    }

    private void Upgrade()
    {
        while (Data.Version < TARGET_VERSION)
        {
            int oldVersion = Data.Version;
            DoUpgradeStep();
            if (Data.Version > oldVersion)
                continue;
            string msg = $"Error upgrading Lootmaster configuration from version {oldVersion}";
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new InvalidOperationException(msg);


        }
    }

    private void DoUpgradeStep() { }

    private static HrtUiMessage PeriodicSave()
    {
        if (ServiceManager.HrtDataManager.Save())
            return new HrtUiMessage(Localize("Core:PeriodicSaveSuccessful", "Data Saved successfully"),
                HrtUiMessageType.Success);
        else
            return new HrtUiMessage(Localize("Core:PeriodicSaveFailed", "Data failed to save"),
                HrtUiMessageType.Failure);
    }

    internal sealed class ConfigData : IHrtConfigData
    {
        [JsonProperty] public int Version = 1;
        [JsonProperty] public bool ShowWelcomeWindow = true;
        [JsonProperty] public bool SavePeriodically = true;
        [JsonProperty] public int SaveIntervalMinutes = 30;
        [JsonProperty] public bool HideInCombat = true;
        /**
         * BiS
         */
        [JsonProperty] public bool UpdateEtroBisOnStartup = true;
        [JsonProperty] public int EtroUpdateIntervalDays = 7;

        public void AfterLoad() { }

        public void BeforeSave() { }
    }

    internal class ConfigUi : IHrtConfigUi
    {
        private ConfigData _dataCopy;
        private readonly CoreConfig _parent;

        public ConfigUi(CoreConfig parent)
        {
            _parent = parent;
            _dataCopy = parent.Data.Clone();
        }

        public void Cancel()
        {
        }

        public void Draw()
        {
            ImGui.Text(Localize("Ui", "User Interface"));
            ImGui.Checkbox(Localize("HideInCombat", "Hide in combat"), ref _dataCopy.HideInCombat);
            ImGuiHelper.AddTooltip(Localize("HideInCombatTooltip", "Hides all windows while character is in combat"));
            ImGui.Separator();
            ImGui.Text(Localize("Auto Save", "Auto Save"));
            ImGui.Checkbox(Localize("Save periodically", "Save periodically"), ref _dataCopy.SavePeriodically);
            ImGuiHelper.AddTooltip(Localize("SavePeriodicallyTooltip",
                "Saves all data of this plugin periodically. (Helps prevent losing data if your game crashes)"));
            ImGui.BeginDisabled(!_dataCopy.SavePeriodically);
            ImGui.TextWrapped($"{Localize("AutoSave_interval_min", "AutoSave interval (min)")}:");
            ImGui.SetNextItemWidth(150 * HrtWindow.ScaleFactor);
            if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
                if (_dataCopy.SaveIntervalMinutes < 1)
                    _dataCopy.SaveIntervalMinutes = 1;
            ImGui.EndDisabled();
            ImGui.Separator();
            ImGui.Text(Localize("Etro Gear Updates"));
            ImGui.Checkbox(Localize("UpdateBisONStartUp", "Update sets from etro.gg periodically"), ref _dataCopy.UpdateEtroBisOnStartup);
            ImGui.BeginDisabled(!_dataCopy.UpdateEtroBisOnStartup);
            ImGui.SetNextItemWidth(150f * HrtWindow.ScaleFactor);
            if (ImGui.InputInt(Localize("BisUpdateInterval", "Update interval (days)"), ref _dataCopy.EtroUpdateIntervalDays))
                if (_dataCopy.EtroUpdateIntervalDays < 1)
                    _dataCopy.EtroUpdateIntervalDays = 1;
            ImGui.EndDisabled();
        }

        public void OnHide()
        {
        }

        public void OnShow()
        {
            _dataCopy = _parent.Data.Clone();
        }

        public void Save()
        {
            if (_dataCopy.SaveIntervalMinutes != _parent.Data.SaveIntervalMinutes)
                _parent._saveTask.Repeat = TimeSpan.FromMinutes(_dataCopy.SaveIntervalMinutes);
            if (_dataCopy.SavePeriodically != _parent.Data.SavePeriodically)
                _parent._saveTask.ShouldRun = _dataCopy.SavePeriodically;
            _parent.Data = _dataCopy;
        }
    }
}