using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Modules.Calendar;

internal class CalendarModuleConfig : ModuleConfiguration<CalendarModuleConfig.ConfigData>
{
    public CalendarModuleConfig(IHrtModule module) : base(module)
    {
        _ui = new ConfigUi(this);
    }

    private readonly ConfigUi _ui;
    public override IHrtConfigUi? Ui => _ui;

    public override void AfterLoad()
    {
    }

    internal class ConfigUi : IHrtConfigUi
    {
        private readonly CalendarModuleConfig _parent;
        private ConfigData _dataCopy;

        public ConfigUi(CalendarModuleConfig parent)
        {
            _parent = parent;
            _dataCopy = parent.Data.Clone();
        }

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

        public void OnShow() => _dataCopy = _parent.Data.Clone();

        public void Save() => _parent.Data = _dataCopy;
    }

    internal class ConfigData : IHrtConfigData
    {
        [JsonProperty("BeginOfWeek")] public DayOfWeek FirstDayOfWeek = DayOfWeek.Monday;
        public void AfterLoad() { }

        public void BeforeSave() { }
    }
}