using System.Globalization;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using HimbeertoniRaidTool.Plugin.UI;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal class CoreModule : IHrtModule
{
    private readonly ChangeLog _changelog;
    private readonly CoreConfig _config;
    private readonly List<HrtCommand> _registeredCommands = new();
    private readonly WelcomeWindow _wcw;

    public CoreModule()
    {
        WindowSystem = new WindowSystem(InternalName);
        _wcw = new WelcomeWindow(this);
        WindowSystem.AddWindow(_wcw);
        _config = new CoreConfig(this);
        _changelog = new ChangeLog(this, _config);
        ServiceManager.CoreModule = this;
        foreach (HrtCommand command in InternalCommands)
        {
            AddCommand(command);
        }
        _config.OnConfigChange += UpdateGearDataProviderConfig;
    }

    private IEnumerable<HrtCommand> InternalCommands => new List<HrtCommand>
    {
        new("/options", ServiceManager.Config.Show)
        {
            AltCommands = new List<string>
            {
                "/option",
                "/config",
            },
            Description = Localize("command:hrt:options", "Shows the Configuration window"),
        },
        new("/welcome", _wcw.Show)
        {
            Description = Localize("command:hrt:welcome",
                                   "Open Welcome Window with explanations on how to use"),
        },
        new("/help", PrintUsage)
        {
            AltCommands = new List<string>
            {
                "/usage",
            },
            Description = CoreLocalization.command_hrt_help,
        },
        new("/changelog", _changelog.Ui.Show)
        {
            Description = GeneralLoc.command_hrt_changelog,
        },
    };
    public bool HideInCombat => _config.Data.HideInCombat;
    public string Name => GeneralLoc.CoreModule_Name;
    public string Description => GeneralLoc.CoreModule_Description;

    public event Action? UiReady;
    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>
    {
        new()
        {
            Command = "/hrt",
            Description = CoreLocalization.command_hrt_help,
            ShowInHelp = true,
            OnCommand = OnCommand,
            ShouldExposeToDalamud = true,
        },
    };

    public string InternalName => "Core";
    public IHrtConfiguration Configuration => _config;
    public WindowSystem WindowSystem { get; }

    public void HandleMessage(HrtUiMessage message)
    {
        switch (message.MessageType)
        {
            case HrtUiMessageType.Discard:
                return;
            case HrtUiMessageType.Failure or HrtUiMessageType.Error:
                ServiceManager.PluginLog.Warning(message.Message);
                break;
            default:
                ServiceManager.PluginLog.Information(message.Message);
                break;
        }
    }

    public void PrintUsage(string command, string args)
    {
        if (!command.Equals("/help")) return;
        string subCommand = '/' + args.Split(' ')[0];
        //Propagate help call to sub command
        if (_registeredCommands.Any(c => c.HandlesCommand(subCommand)))
        {
            string newArgs = $"help {args[(subCommand.Length - 1)..]}".Trim();

            _registeredCommands.First(x => x.HandlesCommand(subCommand)).OnCommand(subCommand, newArgs);
            return;
        }

        SeStringBuilder stringBuilder = new SeStringBuilder()
                                        .AddUiForeground("[Himbeertoni Raid Tool]", 45)
                                        .AddUiForeground("[Help]", 62)
                                        .AddText(Localize("hrt:usage:heading",
                                                          " Commands used for Himbeertoni Raid Tool:"))
                                        .Add(new NewLinePayload());
        foreach (HrtCommand c in _registeredCommands.Where(com => !com.Command.Equals("/hrt") && com.ShowInHelp))
        {
            stringBuilder
                .AddUiForeground($"/hrt {c.Command[1..]}", 37)
                .AddText($" - {c.Description}")
                .Add(new NewLinePayload());
        }

        ServiceManager.ChatGui.Print(stringBuilder.BuiltString);
    }

    public void AfterFullyLoaded()
    {
        UpdateGearDataProviderConfig();
        ServiceManager.TaskManager.RegisterTask(
            new HrtTask(() =>
            {
                ServiceManager.HrtDataManager.CleanupDatabase();
                return HrtUiMessage.Empty;
            }, HandleMessage, "Cleanup database")
        );
        ServiceManager.TaskManager.RegisterTask(
            new HrtTask(
                () => ServiceManager.ConnectorPool.EtroConnector.UpdateEtroSets(_config.Data.UpdateEtroBisOnStartup,
                    _config.Data.EtroUpdateIntervalDays),
                HandleMessage, "Update etro sets")
        );
        if (_config.Data.ShowWelcomeWindow)
        {
            _config.Data.ShowWelcomeWindow = false;
            _config.Data.LastSeenChangelog = _changelog.CurrentVersion;
            _config.Save(ServiceManager.HrtDataManager.ModuleConfigurationManager);
            _wcw.Show();
        }
        if (ServiceManager.ClientState.IsLoggedIn)
            UiReady?.Invoke();
    }
    public void Update() { }
    public void OnLanguageChange(string langCode) => CoreLocalization.Culture = new CultureInfo(langCode);
    public void Dispose()
    {
        _config.OnConfigChange -= UpdateGearDataProviderConfig;
        _changelog.Dispose(this);
    }

    internal void AddCommand(HrtCommand command) => _registeredCommands.Add(command);

    internal void OnCommand(string command, string args)
    {
        if (!command.Equals("/hrt")) return;
        string subCommand = '/' + (args.IsNullOrEmpty() ? "help" : args.Split(' ')[0]);
        string newArgs = args.IsNullOrEmpty() ? "" : args[(subCommand.Length - 1)..].Trim();
        if (_registeredCommands.Any(x => x.HandlesCommand(subCommand)))
            _registeredCommands.First(x => x.HandlesCommand(subCommand)).OnCommand(subCommand, newArgs);
        else
            ServiceManager.PluginLog.Error($"Argument {args} for command \"/hrt\" not recognized");
    }
    internal void MigrateBisUpdateConfig(bool shouldUpdate) => _config.Data.UpdateEtroBisOnStartup = shouldUpdate;
    internal void MigrateBisUpdateInterval(int interval) => _config.Data.EtroUpdateIntervalDays = interval;
    private void UpdateGearDataProviderConfig()
    {
        var newConfig = new GearDataProviderConfiguration(_config.Data.UpdateOwnData, _config.Data.UpdateCombatJobs,
                                                          _config.Data.UpdateDoHJobs, _config.Data.UpdateDoLJobs);
        ServiceManager.OwnCharacterDataProvider.Enable(newConfig);
        ServiceManager.ExamineGearDataProvider.Enable(newConfig);
    }
}