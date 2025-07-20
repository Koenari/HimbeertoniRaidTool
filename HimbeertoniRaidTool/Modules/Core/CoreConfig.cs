using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;
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
            Module.Services.Logger.Fatal(msg);
            Module.Services.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
        }
        Upgrade();
        _saveTask.Repeat = TimeSpan.FromMinutes(Data.SaveIntervalMinutes);
        _saveTask.ShouldRun = Data.SavePeriodically;
        _saveTask.LastRun = DateTime.Now;
        Module.Services.TaskManager.RegisterTask(_saveTask);
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
            Module.Services.Logger.Fatal(msg);
            Module.Services.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new InvalidOperationException(msg);


        }
    }

    private void DoUpgradeStep() { }

    private HrtUiMessage PeriodicSave()
    {
        if (Module.Services.HrtDataManager.Save())
            return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveSuccessful,
                                    HrtUiMessageType.Success);
        return new HrtUiMessage(CoreLoc.UiMessage_PeriodicSaveFailed,
                                HrtUiMessageType.Failure);
    }

    internal sealed class ConfigData : IHrtConfigData<ConfigData>
    {

        #region ChangLog

        [JsonProperty] public ChangelogShowOptions ChangelogNotificationOptions = ChangelogShowOptions.ShowAll;

        #endregion

        #region Ui

        [JsonProperty] public bool HideInCombat = true;

        #endregion

        public void AfterLoad(HrtDataManager dataManager) { }

        public void BeforeSave() { }

        #region Calculations

        [JsonProperty("DefaultPartyBonus")] public PartyBonus PartyBonus = PartyBonus.None;

        #endregion

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

        public ConfigData Clone() => CloneService.Clone(this);
    }

    internal class ConfigUi(CoreConfig parent) : IHrtConfigUi
    {
        private ConfigData _dataCopy = parent.Data.Clone();

        public void Cancel()
        {
        }

        public void Draw()
        {
            using var tabBar = ImRaii.TabBar("##coreTabs");
            if (!tabBar)
                return;

            DrawGeneralTab();
            DrawGearUpdatesTab();
        }

        private void DrawGeneralTab()
        {
            using var tabItem = ImRaii.TabItem(CoreLoc.ConfigUi_tab_general);
            if (!tabItem)
                return;

            //Ui
            ImGui.Text(CoreLoc.ConfigUi_hdg_ui);
            using (ImRaii.PushIndent())
            {
                ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_hideInCombat, ref _dataCopy.HideInCombat,
                    CoreLoc.ConfigUi_cb_tt_hideInCombat);
            }
            ImGui.Separator();
            //Calc
            ImGui.Text("Calculation defaults");
            using (ImRaii.PushIndent())
            {
                ImGui.Text("Party Bonus");
                ImGui.SameLine();
                ImGuiHelper.Combo("##PartyBonus", ref _dataCopy.PartyBonus, b => b.FriendlyName());
            }
            ImGui.Separator();
            //AutoSave
            ImGui.Text(CoreLoc.ConfigUi_hdg_AutoSave);
            using (ImRaii.PushIndent())
            {
                ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_periodicSave, ref _dataCopy.SavePeriodically,
                    CoreLoc.ConfigUi_cb_tt_periodicSave);
                using var disabled = ImRaii.Disabled(!_dataCopy.SavePeriodically);
                ImGui.TextWrapped($"{CoreLoc.ConfigUi_in_autoSaveInterval}:");
                ImGui.SetNextItemWidth(150 * HrtWindow.ScaleFactor);
                if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
                    if (_dataCopy.SaveIntervalMinutes < 1)
                        _dataCopy.SaveIntervalMinutes = 1;
            }
            ImGui.Separator();
            //Changelog
            ImGui.Text(CoreLoc.ConfigUi_hdg_changelog);
            using (ImRaii.PushIndent())
            {
                ImGuiHelper.Combo("##showChangelog", ref _dataCopy.ChangelogNotificationOptions,
                    t => t.LocalizedDescription());
            }
            ImGui.Separator();
            DrawConnectorSection(GearSetManager.Etro, ref _dataCopy.UpdateEtroBisOnStartup,
                ref _dataCopy.EtroUpdateIntervalDays);
            DrawConnectorSection(GearSetManager.XivGear, ref _dataCopy.UpdateXivGearBisOnStartup,
                ref _dataCopy.XivGearUpdateIntervalDays);
        }

        private void DrawGearUpdatesTab()
        {
            using var tabItem = ImRaii.TabItem(CoreLoc.ConfigUi_tab_GearUpdates);
            if (!tabItem)
                return;

            //Automatic gear
            ImGui.Text(CoreLoc.ConfigUi_hdg_dataUpdate);
            using var indent = ImRaii.PushIndent();

            ImGui.Checkbox(CoreLoc.ConfigUi_cb_ownData, ref _dataCopy.UpdateOwnData);
            ImGuiHelper.AddTooltip(CoreLoc.ConfigUi_cb_tt_ownData);
            ImGui.Checkbox(CoreLoc.ConfigUi_cb_examine, ref _dataCopy.UpdateGearOnExamine);
            using var disabled = ImRaii.Disabled(_dataCopy is {UpdateOwnData: false, UpdateGearOnExamine: false});
            ImGui.Text(CoreLoc.ConfigUi_text_dataUpdateJobs);
            using (ImRaii.PushIndent())
            {
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateCombatJobs, ref _dataCopy.UpdateCombatJobs);
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateDohJobs, ref _dataCopy.UpdateDoHJobs);
                ImGui.Checkbox(CoreLoc.ConfigUi_cb_updateDolJobs, ref _dataCopy.UpdateDoLJobs);
            }
            ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_ignorePrevTierGear,
                ref _dataCopy.GearUpdateRestrictToCurrentTier,
                CoreLoc.ConfigUi_cb_tt_ignorePrevTierGear);
            ImGui.SameLine();
            ImGui.Text(
                $"({GeneralLoc.CommonTerms_itemLvl_abbrev} < {(GameInfo.PreviousSavageTier?.ArmorItemLevel ?? 0) + 10})");
            ImGuiHelper.Checkbox(CoreLoc.ConfigUi_cb_ignoreCustomILvlGear,
                ref _dataCopy.GearUpdateRestrictToCustomILvL,
                CoreLoc.ConfigUi_cb_tt_ignoreCustomILvlGear);
            {
                using var disabled2 = ImRaii.Disabled(!_dataCopy.GearUpdateRestrictToCustomILvL);
                using var indent2 = ImRaii.PushIndent();
                ImGui.InputInt(GeneralLoc.CommonTerms_itemLevel, ref _dataCopy.GearUpdateCustomILvlCutoff);
            }
        }

        void DrawConnectorSection(GearSetManager type, ref bool doUpdates, ref int maxAgeInDays)
        {
            string serviceName = type.FriendlyName();
            using (ImRaii.PushId(serviceName))
            {
                ImGui.Text(string.Format(CoreLoc.ConfigUi_hdg_externalUpdates, serviceName));
                if (parent.Module.Services.ConnectorPool.TryGetConnector(
                        type, out var connector))
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.Button("Update now",
                                           $"Triggers auto updates for {serviceName} according to below rules now"))
                    {
                        int maxAge = maxAgeInDays;
                        parent.Module.Services.TaskManager.RegisterTask(
                            new HrtTask<HrtUiMessage>(
                                () => connector.UpdateAllSets(true, maxAge),
                                parent.Module.HandleMessage, serviceName));
                    }
                    ImGui.SameLine();
                    if (ImGuiHelper.GuardedButton("Force-update",
                                                  $"Triggers auto updates for EVERY set from {serviceName}. This might take a while"))
                    {
                        parent.Module.Services.TaskManager.RegisterTask(
                            new HrtTask<HrtUiMessage>(
                                () => connector.UpdateAllSets(true, 0),
                                parent.Module.HandleMessage,
                                serviceName));
                    }
                }
                using (ImRaii.PushIndent()) {
                    ImGui.Checkbox(string.Format(CoreLoc.ConfigUi_cb_extAutoUpdate, serviceName), ref doUpdates);
                    using var disabled = ImRaii.Disabled(!doUpdates);
                    ImGui.SetNextItemWidth(150f * HrtWindow.ScaleFactor);
                    if (ImGui.InputInt(CoreLoc.ConfigUi_in_externalUpdateInterval,
                            ref maxAgeInDays))
                        if (maxAgeInDays < 1)
                            maxAgeInDays = 1;
                }
            }
            ImGui.Separator();
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