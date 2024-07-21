using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal sealed class CoreConfig : ModuleConfiguration<CoreConfig.ConfigData>
{
    private const int TARGET_VERSION = 1;
    private readonly PeriodicTask _saveTask;
    public CoreConfig(CoreModule module) : base(module)
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
            ServiceManager.Logger.Fatal(msg);
            ServiceManager.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
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
            string msg = string.Format(CoreLoc.Chat_configUpgradeError, oldVersion);
            ServiceManager.Logger.Fatal(msg);
            ServiceManager.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new InvalidOperationException(msg);


        }
    }

    private void DoUpgradeStep() { }

    private static HrtUiMessage PeriodicSave()
    {
        if (ServiceManager.HrtDataManager.Save())
            return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveSuccessful,
                                    HrtUiMessageType.Success);
        return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveFailed,
                                HrtUiMessageType.Failure);
    }

    internal sealed class ConfigData : IHrtConfigData
    {

        #region ChangLog

        [JsonProperty] public ChangelogShowOptions ChangelogNotificationOptions = ChangelogShowOptions.ShowAll;

        #endregion
        #region Ui

        [JsonProperty] public bool HideInCombat = true;

        #endregion

        public void AfterLoad() { }

        public void BeforeSave() { }

        #region Internal

        [JsonProperty] public bool ShowWelcomeWindow = true;
        [JsonProperty] public int Version = 1;
        [JsonProperty] public Version LastSeenChangelog = new(0, 0, 0, 0);

        #endregion

        #region BiS

        [JsonProperty] public int EtroUpdateIntervalDays = 7;
        [JsonProperty] public bool UpdateEtroBisOnStartup = true;

        [JsonProperty] public int XivGearUpdateIntervalDays = 7;
        [JsonProperty] public bool UpdateXivGearBisOnStartup = true;

        #endregion

        #region AutoSave

        [JsonProperty] public int SaveIntervalMinutes = 30;
        [JsonProperty] public bool SavePeriodically = true;

        #endregion

        #region DataProviders

        [JsonProperty] public bool UpdateCombatJobs = true;
        [JsonProperty] public bool UpdateDoHJobs;
        [JsonProperty] public bool UpdateDoLJobs;
        [JsonProperty] public bool UpdateOwnData = true;
        [JsonProperty] public bool UpdateGearOnExamine = true;
        [JsonProperty] public bool GearUpdateRestrictToCurrentTier = true;
        [JsonProperty] public bool GearUpdateRestrictToCustomILvL;
        [JsonProperty] public int GearUpdateCustomILvlCutoff;

        #endregion
    }

    internal class ConfigUi(CoreConfig parent) : IHrtConfigUi
    {
        private ConfigData _dataCopy = parent.Data.Clone();

        public void Cancel()
        {
        }

        public void Draw()
        {
            if (!ImGui.BeginTabBar("##coreTabs"))
                return;
            if (ImGui.BeginTabItem(CoreLoc.ConfigUi_tab_general))
            {
                //Ui
                ImGui.Text(CoreLoc.ConfigUi_hdg_ui);
                ImGui.Indent(10);
                ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_hideInCombat, ref _dataCopy.HideInCombat,
                                     CoreLoc.ConfigUi_cb_tt_hideInCombat);
                ImGui.Indent(-10);
                ImGui.Separator();
                //AutoSave
                ImGui.Text(CoreLoc.ConfigUi_hdg_AutoSave);
                ImGui.Indent(10);
                ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_periodicSave, ref _dataCopy.SavePeriodically,
                                     CoreLoc.ConfigUi_cb_tt_periodicSave);
                ImGui.BeginDisabled(!_dataCopy.SavePeriodically);
                ImGui.TextWrapped($"{CoreLoc.ConfigUi_in_autoSaveInterval}:");
                ImGui.SetNextItemWidth(150 * HrtWindow.ScaleFactor);
                if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
                    if (_dataCopy.SaveIntervalMinutes < 1)
                        _dataCopy.SaveIntervalMinutes = 1;
                ImGui.EndDisabled();
                ImGui.Indent(-10);
                ImGui.Separator();
                //Changelog
                ImGui.Text(CoreLoc.ConfigUi_hdg_changelog);
                ImGui.Indent(10);
                ImGuiHelper.Combo("##showChangelog", ref _dataCopy.ChangelogNotificationOptions,
                                  t => t.LocalizedDescription());
                ImGui.Indent(-10);
                ImGui.Separator();
                //Etro.gg
                ImGui.Text(string.Format(CoreLoc.ConfigUi_hdg_externalUpdates, "etro.gg"));
                ImGui.Indent(10);
                ImGui.Checkbox(string.Format(CoreLoc.ConfigUi_cb_extAutoUpdate, "etro.gg"),
                               ref _dataCopy.UpdateEtroBisOnStartup);
                ImGui.BeginDisabled(!_dataCopy.UpdateEtroBisOnStartup);
                ImGui.SetNextItemWidth(150f * HrtWindow.ScaleFactor);
                if (ImGui.InputInt(CoreLoc.ConfigUi_in_externalUpdateInterval, ref _dataCopy.EtroUpdateIntervalDays))
                    if (_dataCopy.EtroUpdateIntervalDays < 1)
                        _dataCopy.EtroUpdateIntervalDays = 1;
                ImGui.EndDisabled();
                ImGui.Indent(-10);
                ImGui.EndTabItem();
                ImGui.Separator();
                //XIvGear.app
                ImGui.Text(string.Format(CoreLoc.ConfigUi_hdg_externalUpdates, "XivGear"));
                ImGui.Indent(10);
                ImGui.Checkbox(string.Format(CoreLoc.ConfigUi_cb_extAutoUpdate, "XivGear"),
                               ref _dataCopy.UpdateXivGearBisOnStartup);
                ImGui.BeginDisabled(!_dataCopy.UpdateEtroBisOnStartup);
                ImGui.SetNextItemWidth(150f * HrtWindow.ScaleFactor);
                if (ImGui.InputInt(CoreLoc.ConfigUi_in_externalUpdateInterval, ref _dataCopy.XivGearUpdateIntervalDays))
                    if (_dataCopy.XivGearUpdateIntervalDays < 1)
                        _dataCopy.XivGearUpdateIntervalDays = 1;
                ImGui.EndDisabled();
                ImGui.Indent(-10);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(CoreLoc.ConfigUi_tab_GearUpdates))
            {
                //Automatic gear
                ImGui.Text(CoreLoc.ConfigUi_hdg_dataUpdate);
                ImGui.Indent(10);
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_ownData, ref _dataCopy.UpdateOwnData);
                ImGuiHelper.AddTooltip(CoreLoc.ConfigUi_cb_tt_ownData);
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_examine, ref _dataCopy.UpdateGearOnExamine);
                ImGui.BeginDisabled(_dataCopy is { UpdateOwnData: false, UpdateGearOnExamine: false });
                ImGui.Text(CoreLoc.ConfigUi_text_dataUpdateJobs);
                ImGui.Indent(25);
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateCombatJobs, ref _dataCopy.UpdateCombatJobs);
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateDohJobs, ref _dataCopy.UpdateDoHJobs);
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateDolJobs, ref _dataCopy.UpdateDoLJobs);
                ImGui.Indent(-25);
                ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_ignorePrevTierGear,
                                     ref _dataCopy.GearUpdateRestrictToCurrentTier,
                                     CoreLoc.ConfigUi_cb_tt_ignorePrevTierGear);
                ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_ignoreCustomILvlGear,
                                     ref _dataCopy.GearUpdateRestrictToCustomILvL,
                                     CoreLoc.ConfigUi_cb_tt_ignoreCustomILvlGear);
                ImGui.BeginDisabled(!_dataCopy.GearUpdateRestrictToCustomILvL);
                ImGui.Indent(25);
                ImGui.InputInt(GeneralLoc.CommonTerms_itemLevel, ref _dataCopy.GearUpdateCustomILvlCutoff);
                ImGui.Indent(-25);
                ImGui.EndDisabled();
                ImGui.EndDisabled();
                ImGui.Indent(-10);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();


        }

        public void OnHide()
        {
        }

        public void OnShow() => _dataCopy = parent.Data.Clone();

        public void Save()
        {
            if (_dataCopy.SaveIntervalMinutes != parent.Data.SaveIntervalMinutes)
                parent._saveTask.Repeat = TimeSpan.FromMinutes(_dataCopy.SaveIntervalMinutes);
            if (_dataCopy.SavePeriodically != parent.Data.SavePeriodically)
                parent._saveTask.ShouldRun = _dataCopy.SavePeriodically;
            parent.Data = _dataCopy;
        }
    }
}