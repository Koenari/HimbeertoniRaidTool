using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;
internal class CoreModule : IHrtModule<CoreConfig.ConfigData, CoreConfig.ConfigUi>
{
    private readonly CoreConfig _config;
    private readonly Ui.WelcomeWindow _wcw;
    private readonly List<HrtCommand> RegisteredCommands = new();
    public string Name => "Core Functions";
    public string Description => "Core functionality of Himbeertoni Raid Tool";
    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>()
    {
        new HrtCommand()
            {
                Command = "/hrt",
                Description = Localization.Localize("/hrt", "Open Welcome Window with explanations"),
                ShowInHelp = true,
                OnCommand = OnCommand,
                ShouldExposeToDalamud = true
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
        ServiceManager.CoreModule = this;
    }
    public void HandleMessage(HrtUiMessage message)
    {
        if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
            PluginLog.Warning(message.Message);
        else
            PluginLog.Information(message.Message);
    }
    internal void AddCommand(HrtCommand command)
    {
        RegisteredCommands.Add(command);
    }
    private void OnCommand(string command, string args)
    {
        if (command.Equals("/hrt"))
        {
            string subCommand = '/' + args.Split(' ')[0];
            if (RegisteredCommands.Any(x => x.Command == subCommand))
            {
                string newArgs = args[(subCommand.Length - 1)..].Trim();
                RegisteredCommands.First(x => x.Command == subCommand).OnCommand(subCommand, newArgs);
                return;
            }
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
    }
    public void AfterFullyLoaded()
    {
        if (_config.Data.ShowWelcomeWindow)
            _wcw.Show();
    }

    public void Update(Framework fw) { }

    public void Dispose() { }
}
