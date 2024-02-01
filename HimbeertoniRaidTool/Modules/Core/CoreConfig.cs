using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal sealed class CoreConfig : HrtConfiguration<CoreConfig.ConfigData>
{
    private const int TARGET_VERSION = 1;
    private readonly PeriodicTask _saveTask;
    public CoreConfig(CoreModule module) : base(module.InternalName, CoreLocalization.CoreConfig_Title)
    {
        Ui = new ConfigUi(this);
        _saveTask = new PeriodicTask(PeriodicSave, module.HandleMessage, "Automatic Save",
                                     TimeSpan.FromMinutes(Data.SaveIntervalMinutes))
        {
            ShouldRun = false,
        };
    }
    public override ConfigUi Ui { get; }

    public override void AfterLoad()
    {
        if (Data.Version > TARGET_VERSION)
        {
            string msg = GeneralLoc.Config_Error_Downgrade;
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
        }
        Upgrade();
        _saveTask.Repeat = TimeSpan.FromMinutes(Data.SaveIntervalMinutes);
        _saveTask.ShouldRun = Data.SavePeriodically;
        _saveTask.LastRun = DateTime.Now;
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
            string msg = string.Format(CoreLocalization.Config_UpgradeError, oldVersion);
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new InvalidOperationException(msg);


        }
    }

    private void DoUpgradeStep() { }

    private static HrtUiMessage PeriodicSave()
    {
        if (ServiceManager.HrtDataManager.Save())
            return new HrtUiMessage(CoreLocalization.PeriodicSaveSuccessful,
                                    HrtUiMessageType.Success);
        return new HrtUiMessage(CoreLocalization.PeriodicSaveFailed,
                                HrtUiMessageType.Failure);
    }

    internal sealed class ConfigData : IHrtConfigData
    {
        [JsonProperty] public ChangelogShowOptions ChangelogNotificationOptions = ChangelogShowOptions.ShowAll;
        [JsonProperty] public int EtroUpdateIntervalDays = 7;
        [JsonProperty] public bool HideInCombat = true;
        /*
         * ChangeLog
         */
        [JsonProperty] public Version LastSeenChangelog = new(0, 0, 0, 0);
        [JsonProperty] public int SaveIntervalMinutes = 30;
        [JsonProperty] public bool SavePeriodically = true;
        [JsonProperty] public bool ShowWelcomeWindow = true;
        [JsonProperty] public bool UpdateCombatJobs = true;
        [JsonProperty] public bool UpdateDoHJobs;
        [JsonProperty] public bool UpdateDoLJobs;
        /**
         * BiS
         */
        [JsonProperty] public bool UpdateEtroBisOnStartup = true;
        [JsonProperty] public bool UpdateGearOnExamine = true;
        /*
         * Data providers
         */
        [JsonProperty] public bool UpdateOwnData = true;
        [JsonProperty] public int Version = 1;


        public void AfterLoad() { }

        public void BeforeSave() { }
    }

    internal class ConfigUi : IHrtConfigUi
    {
        private readonly CoreConfig _parent;
        private ConfigData _dataCopy;

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
            ImGui.Text(CoreLocalization.ConfigUi_Heading_dataUpdate);
            ImGui.Checkbox(CoreLocalization.ConfigUi_checkbox_ownData, ref _dataCopy.UpdateOwnData);
            ImGuiHelper.AddTooltip(CoreLocalization.ConfigUi_checkbox_ownData_tooltip);
            ImGui.Checkbox(CoreLocalization.ConfigUi_checkbox_examine, ref _dataCopy.UpdateGearOnExamine);
            ImGui.BeginDisabled(_dataCopy is { UpdateOwnData: false, UpdateGearOnExamine: false });
            ImGui.Text(CoreLocalization.ConfigUi_text_dataUpdateJobs);
            ImGui.Indent(25);
            ImGui.Checkbox(CoreLocalization.ConfigUi_checkbox_updateCombatJobs, ref _dataCopy.UpdateCombatJobs);
            ImGui.Checkbox(CoreLocalization.ConfigUi_checkbox_updateDohJobs, ref _dataCopy.UpdateDoHJobs);
            ImGui.Checkbox(CoreLocalization.ConfigUi_checkbox_updateDolJobs, ref _dataCopy.UpdateDoLJobs);
            ImGui.Indent(-25);
            ImGui.EndDisabled();
            ImGui.Separator();
            ImGui.Text(CoreLocalization.ConfigUi_heading_ui);
            ImGuiHelper.Checkbox(CoreLocalization.ConfigUi_checkbox_hideInCombat, ref _dataCopy.HideInCombat,
                                 CoreLocalization.ConfigUi_checkbox_hideInCombat_tooltip);
            ImGui.Separator();
            ImGui.Text(CoreLocalization.ConfigUi_heading_AutoSave);
            ImGuiHelper.Checkbox(CoreLocalization.ConfigUi_checkbox_savePeriodically, ref _dataCopy.SavePeriodically,
                                 CoreLocalization.ConfigUi_checkbox_savePeriodically_tooltip);
            ImGui.BeginDisabled(!_dataCopy.SavePeriodically);
            ImGui.TextWrapped($"{CoreLocalization.ConfigUi_input_AutoSaveInterval}:");
            ImGui.SetNextItemWidth(150 * HrtWindow.ScaleFactor);
            if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
                if (_dataCopy.SaveIntervalMinutes < 1)
                    _dataCopy.SaveIntervalMinutes = 1;
            ImGui.EndDisabled();
            ImGui.Separator();
            ImGui.Text(CoreLocalization.ConfigUi_Changelog_Title);
            ImGuiHelper.Combo("##showChangelog", ref _dataCopy.ChangelogNotificationOptions,
                              t => t.LocalizedDescription());
            ImGui.Separator();
            ImGui.Text(CoreLocalization.ConfigUi_EtroGearUpdates);
            ImGui.Checkbox(CoreLocalization.ConfigUi_UpdateBisOnStartUp, ref _dataCopy.UpdateEtroBisOnStartup);
            ImGui.BeginDisabled(!_dataCopy.UpdateEtroBisOnStartup);
            ImGui.SetNextItemWidth(150f * HrtWindow.ScaleFactor);
            if (ImGui.InputInt(CoreLocalization.ConfigUi_BisUpdateInterval, ref _dataCopy.EtroUpdateIntervalDays))
                if (_dataCopy.EtroUpdateIntervalDays < 1)
                    _dataCopy.EtroUpdateIntervalDays = 1;
            ImGui.EndDisabled();
        }

        public void OnHide()
        {
        }

        public void OnShow() => _dataCopy = _parent.Data.Clone();

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