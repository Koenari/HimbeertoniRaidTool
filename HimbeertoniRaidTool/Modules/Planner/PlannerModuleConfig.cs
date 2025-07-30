using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Modules.Planner;

internal class PlannerModuleConfig : ModuleConfiguration<PlannerModuleConfig.ConfigData, PlannerModule>
{
    public PlannerModuleConfig(PlannerModule module) : base(module)
    {
        _ui = new ConfigUi(this);
    }

    private readonly ConfigUi _ui;
    public override IHrtConfigUi Ui => _ui;

    public override void AfterLoad()
    {
    }

    internal class ConfigUi(PlannerModuleConfig parent) : IHrtConfigUi
    {
        private ConfigData _dataCopy = parent.Data.Clone();

        public void Cancel() { }

        public void Draw()
        {
            /*
             * Ui
             */
            ImGui.Text("Ui");
            ImGui.Text("Weeks start on");
            ImGui.SameLine();
            ImGuiHelper.Combo("##FirstDayOfWeek", ref _dataCopy.FirstDayOfWeek);
        }

        public void OnHide() { }

        public void OnShow() => _dataCopy = parent.Data.Clone();

        public void Save() => parent.Data = _dataCopy;
    }

    internal class ConfigData : IHrtConfigData<ConfigData>
    {
        [JsonProperty("BeginOfWeek")] public DayOfWeek FirstDayOfWeek = DayOfWeek.Monday;

        public void AfterLoad(HrtDataManager dataManager) { }
        public void BeforeSave() { }

        public ConfigData Clone() => (ConfigData)MemberwiseClone();
    }
}