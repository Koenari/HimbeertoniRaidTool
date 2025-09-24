using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Modules.Planner;

internal class PlannerModuleConfig : ModuleConfiguration<PlannerModuleConfig.ConfigData, PlannerModule,
    PlannerModuleConfig.ConfigUi>
{
    public PlannerModuleConfig(PlannerModule module) : base(module)
    {
        Ui = new ConfigUi(this);
    }

    internal class ConfigUi(PlannerModuleConfig parent) : IHrtConfigUi
    {
        private ConfigData _dataCopy = parent.Data.Clone();

        public void Cancel() { }

        public void Draw()
        {
            using (ImRaii.TabBar("##planner"))
            {
                using (var tabItem = ImRaii.TabItem("Appearance"))
                {
                    if (tabItem)
                    {
                        ImGui.Text("Weeks start on");
                        ImGui.SameLine();
                        ImGuiHelper.Combo("##FirstDayOfWeek", ref _dataCopy.FirstDayOfWeek);
                    }
                }
            }

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