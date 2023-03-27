using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;
internal sealed class CoreConfig : HRTConfiguration<CoreConfig.ConfigData, CoreConfig.ConfigUi>
{
    private readonly ConfigUi _ui;
    private readonly CoreModule Parent;
    private readonly PeriodicTask SaveTask;
    public override ConfigUi? Ui => _ui;

    public CoreConfig(CoreModule module) : base(module.InternalName, Localize("General", "General"))
    {
        _ui = new(this);
        Parent = module;
        SaveTask = new PeriodicTask(PeriodicSave, Parent.HandleMessage,
                    TimeSpan.FromMinutes(Data.SaveIntervalMinutes))
        {
            ShouldRun = false,
        };
    }
    public override void AfterLoad()
    {
        SaveTask.Repeat = TimeSpan.FromMinutes(Data.SaveIntervalMinutes);
        SaveTask.ShouldRun = Data.SavePeriodically;
        ServiceManager.TaskManager.RegisterTask(SaveTask);
    }
    private HrtUiMessage PeriodicSave()
    {
        if (ServiceManager.HrtDataManager.Save())
            return new(Localize("Core:PeriodicSaveSuccessful", "Data Saved successfully"), HrtUiMessageType.Success);
        else
            return new(Localize("Core:PeriodicSaveFailed", "Data failed to save"), HrtUiMessageType.Failure);
    }
    internal sealed class ConfigData
    {
        [JsonProperty]
        internal bool ShowWelcomeWindow = true;
        [JsonProperty]
        public bool SavePeriodically = true;
        [JsonProperty]
        public int SaveIntervalMinutes = 30;
        [JsonProperty]
        public bool HideInCombat = true;
    }
    internal class ConfigUi : IHrtConfigUi
    {
        private ConfigData _dataCopy;
        private readonly CoreConfig Parent;

        public ConfigUi(CoreConfig parent)
        {
            Parent = parent;
            _dataCopy = parent.Data.Clone();
        }
        public void Cancel() { }

        public void Draw()
        {
            ImGui.Text(Localize("Ui", "User Interface"));
            ImGui.Checkbox(Localize("HideInCombat", "Hide in combat"), ref _dataCopy.HideInCombat);
            ImGuiHelper.AddTooltip(Localize("HideInCombatTooltip", "Hides all windows while character is in combat"));
            ImGui.Separator();
            ImGui.Text(Localize("Auto Save", "Auto Save"));
            ImGui.Checkbox(Localize("Save periodically", "Save periodically"), ref _dataCopy.SavePeriodically);
            ImGuiHelper.AddTooltip(Localize("SavePeriodicallyTooltip", "Saves all data of this plugin periodically. (Helps prevent losing data if your game crashes)"));
            ImGui.TextWrapped($"{Localize("AutoSave_interval_min", "AutoSave interval (min)")}:");
            ImGui.SetNextItemWidth(150 * HrtWindow.ScaleFactor);
            if (ImGui.InputInt("##AutoSave_interval_min", ref _dataCopy.SaveIntervalMinutes))
            {
                if (_dataCopy.SaveIntervalMinutes < 1)
                    _dataCopy.SaveIntervalMinutes = 1;
            }
        }

        public void OnHide() { }

        public void OnShow() => _dataCopy = Parent.Data.Clone();
        public void Save()
        {
            if (_dataCopy.SaveIntervalMinutes != Parent.Data.SaveIntervalMinutes)
                Parent.SaveTask.Repeat = TimeSpan.FromMinutes(_dataCopy.SaveIntervalMinutes);
            if (_dataCopy.SavePeriodically != Parent.Data.SavePeriodically)
                Parent.SaveTask.ShouldRun = _dataCopy.SavePeriodically;
            Parent.Data = _dataCopy;
        }
    }
}

