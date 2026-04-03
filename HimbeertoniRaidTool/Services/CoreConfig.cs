using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

internal sealed class CoreConfig : Configuration<CoreConfig.ConfigData, CoreConfig.ConfigUi>
{
    private readonly TaskManager _taskManager;
    private readonly ILogger _logger;

    public CoreConfig(ILogger logger, TaskManager taskManager, IUiSystem uiSystem) :
        base("Core")
    {
        Ui = new ConfigUi(this, uiSystem);
        _taskManager = taskManager;
        _logger = logger;

    }


    internal sealed class ConfigData : IHrtConfigData<ConfigData>
    {

        #region Modules

        [JsonProperty] public Dictionary<string, bool> ModulesEnabled { get; set; } = [];

        #endregion

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

        #region Helpers

        public int MinILvlDowngrade => (RestrictToCurrentTier: GearUpdateRestrictToCurrentTier,
                RestrictToCustomILvL: GearUpdateRestrictToCustomILvL) switch
            {
                (true, true) => Math.Min((GameInfo.PreviousSavageTier?.ArmorItemLevel ?? -10) + 10,
                                         GearUpdateCustomILvlCutoff),
                (true, false) => (GameInfo.PreviousSavageTier?.ArmorItemLevel ?? -10) + 10,
                (false, true) => GearUpdateCustomILvlCutoff,
                _             => 0,
            };

        #endregion
    }

    internal class ConfigUi(CoreConfig parent, IUiSystem uiSystem) : IHrtConfigUi
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
                InputHelper.Combo("##PartyBonus", ref _dataCopy.PartyBonus, b => b.FriendlyName());
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
                InputHelper.Combo("##showChangelog", ref _dataCopy.ChangelogNotificationOptions,
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
            using var disabled = ImRaii.Disabled(_dataCopy is { UpdateOwnData: false, UpdateGearOnExamine: false });
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

        private void DrawConnectorSection(GearSetManager type, ref bool doUpdates, ref int maxAgeInDays)
        {
            string serviceName = type.FriendlyName();
            using (ImRaii.PushId(serviceName))
            {
                ImGui.Text(string.Format(CoreLoc.ConfigUi_hdg_externalUpdates, serviceName));
                if (uiSystem.GetConnectorPool().TryGetConnector(type, out var connector))
                {
                    ImGui.SameLine();
                    if (ImGuiHelper.Button("Update now",
                                           $"Triggers auto updates for {serviceName} according to below rules now"))
                    {
                        int maxAge = maxAgeInDays;
                        parent._taskManager.RegisterTask(
                            new HrtTask<HrtUiMessage>(
                                () => connector.UpdateAllSets(true, maxAge),
                                parent._logger.Write, serviceName));
                    }
                    ImGui.SameLine();
                    if (ImGuiHelper.GuardedButton("Force-update",
                                                  $"Triggers auto updates for EVERY set from {serviceName}. This might take a while"))
                    {
                        parent._taskManager.RegisterTask(
                            new HrtTask<HrtUiMessage>(
                                () => connector.UpdateAllSets(true, 0), parent._logger.Write,
                                serviceName));
                    }
                }
                using (ImRaii.PushIndent())
                {
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
            _dataCopy.ModulesEnabled = parent.Data.ModulesEnabled;
            parent.Data = _dataCopy;
        }
    }
}