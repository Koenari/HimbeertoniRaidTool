using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.HrtServices;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;
internal class CoreModule : IHrtModule<CoreConfig.ConfigData, IHrtConfigUi>
{
    private readonly CoreConfig _config;
    private readonly Ui.WelcomeWindow _wcw;
    public string Name => "Core Functions";
    public string Description => "Core functionality of Himbeertoni Raid Tool";
    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>()
    {
        new HrtCommand()
            {
                Command = "/hrt",
                Description = Localization.Localize("/hrt", "Open Welcome Window with explanations"),
                ShowInHelp = true,
                OnCommand = OnCommand
            },
    };
    public string InternalName => "Core";
    public HRTConfiguration<CoreConfig.ConfigData, IHrtConfigUi> Configuration => _config;
    public Dalamud.Interface.Windowing.WindowSystem WindowSystem { get; }
    public CoreModule()
    {
        WindowSystem = new(InternalName);
        _wcw = new(this);
        WindowSystem.AddWindow(_wcw);
        _config = new CoreConfig(this);
    }
    public void HandleMessage(HrtUiMessage message)
    {
        if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
            PluginLog.Warning(message.Message);
        else
            PluginLog.Information(message.Message);
    }
    private void OnCommand(string args)
    {
        switch (args)
        {
            case string a when a.Contains("option") || a.Contains("config"): Services.Config.Ui.Show(); break;
#if DEBUG
            case string b when b.Contains("exportlocale"): Localization.ExportLocalizable(); break;
#endif
            case string when args.IsNullOrEmpty() || args.Contains("help"): _wcw.Show(); break;
            default:
                PluginLog.LogError($"Argument {args} for command \"/hrt\" not recognized");
                break;
        }
    }
    public void AfterFullyLoaded()
    {
        if (_config.Data.ShowWelcomeWindow)
            _wcw.Show();
    }

    public void Update(Framework fw) { }

    public void Dispose() { }
}
internal sealed class CoreConfig : HRTConfiguration<CoreConfig.ConfigData, IHrtConfigUi>
{
    public override IHrtConfigUi? Ui => null;

    public CoreConfig(CoreModule module) : base(module.InternalName, Localization.Localize("General", "General"))
    {
    }
    public override void AfterLoad() { }

    internal sealed class ConfigData
    {
        [JsonProperty]
        internal bool ShowWelcomeWindow = true;
        public ConfigData() { }
    }
}
