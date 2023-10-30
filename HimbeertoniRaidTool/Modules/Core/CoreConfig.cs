using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal sealed class CoreConfig : HrtConfiguration<CoreConfig.ConfigData, CoreConfig.ConfigUi>
{
    private readonly ConfigUi _ui;
    private readonly CoreModule _parent;
    private readonly PeriodicTask _saveTask;
    public override ConfigUi? Ui => _ui;

    public CoreConfig(CoreModule module) : base(module.InternalName, Localize("General", "General"))
    {
        _ui = new ConfigUi(this);
        _parent = module;
        _saveTask = new PeriodicTask(PeriodicSave, _parent.HandleMessage, "Automatic Save",
            TimeSpan.FromMinutes(Data.SaveIntervalMinutes))
        {
            ShouldRun = false,
        };
    }

    public override void AfterLoad()
    {
        _saveTask.Repeat = TimeSpan.FromMinutes(Data.SaveIntervalMinutes);
        _saveTask.ShouldRun = Data.SavePeriodically;
        ServiceManager.TaskManager.RegisterTask(_saveTask);
    }

    private HrtUiMessage PeriodicSave()
    {
        if (ServiceManager.HrtDataManager.Save())
            return new HrtUiMessage(Localize("Core:PeriodicSaveSuccessful", "Data Saved successfully"),
                HrtUiMessageType.Success);
        else
            return new HrtUiMessage(Localize("Core:PeriodicSaveFailed", "Data failed to save"),
                HrtUiMessageType.Failure);
    }

    internal sealed class ConfigData
    {
        [JsonProperty] internal bool ShowWelcomeWindow = true;
        [JsonProperty] public bool SavePeriodically = true;
        [JsonProperty] public int SaveIntervalMinutes = 30;
        [JsonProperty] public bool HideInCombat = true;
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
            ImGui.TextWrapped($"{Localize("AutoSave_interval_min", "AutoSave interval (min)")}:");
            ImGui.SetNextItemWidth(150 * HrtWindow.ScaleFactor);
            if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
                if (_dataCopy.SaveIntervalMinutes < 1)
                    _dataCopy.SaveIntervalMinutes = 1;
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