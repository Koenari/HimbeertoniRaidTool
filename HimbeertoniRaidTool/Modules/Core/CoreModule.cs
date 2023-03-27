using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;
internal class CoreModule : IHrtModule<CoreConfig.ConfigData, CoreConfig.ConfigUi>
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
    public HRTConfiguration<CoreConfig.ConfigData, CoreConfig.ConfigUi> Configuration => _config;
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
            case string a when a.Contains("option") || a.Contains("config"): ServiceManager.Config.Show(); break;
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
