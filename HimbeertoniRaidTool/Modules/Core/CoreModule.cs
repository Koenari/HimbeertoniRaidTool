using System.Globalization;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules.Core.Ui;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Core;

internal class CoreModule : IHrtModule<CoreModule>
{
    #region Static

    public static string Name => CoreLoc.Module_Name;

    public static string Description => CoreLoc.Module_Description;

    public static string InternalName => "Core";

    public static bool CanBeDisabled => false;

    #endregion

    private static readonly UiConfig FalseConfig = new(false);
    private static readonly UiConfig TrueConfig = new(true);
    private static CoreModule? _instance;

    public static UiConfig UiConfig =>
        _instance?._config.Data.HideInCombat ?? false ? TrueConfig : FalseConfig;

    private readonly ChangeLog _changelog;
    private readonly CoreConfig _config;
    private readonly List<HrtCommand> _registeredCommands = new();
    private readonly WelcomeWindow _wcw;

    private CoreModule(IModuleServiceContainer services)
    {
        Services = services;
        CoreLoc.Culture = Services.LocalizationManager.CurrentLocale;
        _wcw = new WelcomeWindow(this);
        Services.UiSystem.AddWindow(_wcw);
        _config = new CoreConfig(this);
        _changelog = new ChangeLog(this, new ChangelogOptionsWrapper(_config));

        foreach (var command in InternalCommands)
        {
            AddCommand(command);
        }
        _config.OnConfigChange += OnConfigChange;
        _instance = this;
    }

    public static CoreModule Create(IModuleServiceContainer services) => new(services);

    private IEnumerable<HrtCommand> InternalCommands => new List<HrtCommand>
    {
        new("/options", Services.ConfigManager.Show)
        {
            AltCommands = new List<string>
            {
                "/option",
                "/config",
            },
            Description = CoreLoc.Command_hrt_options,
        },
        new("/welcome", _wcw.Show)
        {
            Description = CoreLoc.Command_hrt_welcome,
        },
        new("/help", PrintUsage)
        {
            AltCommands = new List<string>
            {
                "/usage",
            },
            Description = CoreLoc.Command_hrt_help,
        },
        new("/changelog", _changelog.ShowUi)
        {
            Description = CoreLoc.command_hrt_changelog,
        },
    };
    public bool HideInCombat => _config.Data.HideInCombat;


    public IModuleServiceContainer Services { get; }
    public event Action? UiReady;
    public IEnumerable<HrtCommand> Commands => new List<HrtCommand>
    {
        new("/hrt", OnCommand)
        {
            Description = CoreLoc.Command_hrt_help,
            ShowInHelp = true,
            ShouldExposeToDalamud = true,
        },
    };
    public IHrtConfiguration Configuration => _config;

    public void HandleMessage(HrtUiMessage message)
    {
        switch (message.MessageType)
        {
            case HrtUiMessageType.Discard:
                return;
            case HrtUiMessageType.Failure or HrtUiMessageType.Error:
                Services.Logger.Warning(message.Message);
                break;
            default:
                Services.Logger.Information(message.Message);
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

        var stringBuilder = new SeStringBuilder()
                            .AddUiForeground("[Himbeertoni Raid Tool]", 45)
                            .AddUiForeground("[Help]", 62)
                            .AddText(CoreLoc.Chat_help_heading)
                            .Add(new NewLinePayload());
        foreach (var c in _registeredCommands.Where(com => !com.Command.Equals("/hrt") && com.ShowInHelp))
        {
            stringBuilder
                .AddUiForeground($"/hrt {c.Command[1..]}", 37)
                .AddText($" - {c.Description}")
                .Add(new NewLinePayload());
        }

        Services.Chat.Print(stringBuilder.BuiltString);
    }

    public void AfterFullyLoaded()
    {
        OnConfigChange();
        Services.TaskManager.RegisterTask(
            new HrtTask<HrtUiMessage>(() =>
            {
                Services.HrtDataManager.CleanupDatabase();
                return HrtUiMessage.Empty;
            }, HandleMessage, "Cleanup database")
        );

        foreach (var serviceType in Enum.GetValues<GearSetManager>())
        {
            if (!Services.ConnectorPool.TryGetConnector(serviceType, out var connector)) continue;
            Services.TaskManager.RegisterTask(
                new HrtTask<HrtUiMessage>(
                    () => connector.UpdateAllSets(_config.Data.UpdateEtroBisOnStartup,
                                                  _config.Data.EtroUpdateIntervalDays),
                    HandleMessage, $"Update {serviceType.FriendlyName()} sets")
            );
        }
        if (_config.Data.ShowWelcomeWindow)
        {
            _config.Data.ShowWelcomeWindow = false;
            _config.Data.LastSeenChangelog = ChangeLog.CurrentVersion;
            _config.Save(Services.HrtDataManager.ModuleConfigurationManager);
            _wcw.Show();
        }
        if (Services.ClientState.IsLoggedIn)
            UiReady?.Invoke();
    }
    public void OnLanguageChange(CultureInfo culture) => CoreLoc.Culture = culture;
    public void Dispose()
    {
        _config.OnConfigChange -= OnConfigChange;
        _changelog.Dispose(this);
    }

    internal void AddCommand(HrtCommand command) => _registeredCommands.Add(command);

    internal void RemoveCommand(HrtCommand command) => _registeredCommands.Remove(command);

    internal void OnCommand(string command, string args)
    {
        if (!command.Equals("/hrt")) return;
        string subCommand = '/' + (args.IsNullOrEmpty() ? "help" : args.Split(' ')[0]);
        string newArgs = args.IsNullOrEmpty() ? "" : args[(subCommand.Length - 1)..].Trim();
        if (_registeredCommands.Any(x => x.HandlesCommand(subCommand)))
            _registeredCommands.First(x => x.HandlesCommand(subCommand)).OnCommand(subCommand, newArgs);
        else
            Services.Logger.Error($"Argument {args} for command \"/hrt\" not recognized");
    }

    private void OnConfigChange() => Services.TaskManager.RunOnFrameworkThread(UpdateGearDataProviderConfig);
    private void UpdateGearDataProviderConfig()
    {
        int minILvl = (RestrictToCurrentTier: _config.Data.GearUpdateRestrictToCurrentTier,
                RestrictToCustomILvL: _config.Data.GearUpdateRestrictToCustomILvL) switch
            {
                (true, true) => Math.Min((GameInfo.PreviousSavageTier?.ArmorItemLevel ?? -10) + 10,
                                         _config.Data.GearUpdateCustomILvlCutoff),
                (true, false) => (GameInfo.PreviousSavageTier?.ArmorItemLevel ?? -10) + 10,
                (false, true) => _config.Data.GearUpdateCustomILvlCutoff,
                _             => 0,
            };

        var newOwnConfig = new GearDataProviderConfiguration(_config.Data.UpdateOwnData, _config.Data.UpdateCombatJobs,
                                                             _config.Data.UpdateDoHJobs, _config.Data.UpdateDoLJobs,
                                                             minILvl);
        var newExamineConfig = new GearDataProviderConfiguration(_config.Data.UpdateGearOnExamine,
                                                                 _config.Data.UpdateCombatJobs,
                                                                 _config.Data.UpdateDoHJobs, _config.Data.UpdateDoLJobs,
                                                                 minILvl);
        Services.OwnCharacterDataProvider.Enable(newOwnConfig);
        Services.ExamineGearDataProvider.Enable(newExamineConfig);
    }

    private class ChangelogOptionsWrapper(CoreConfig coreConfig) : ChangeLog.IConfigOptions
    {
        public Version LastSeenChangelog
        {
            get => coreConfig.Data.LastSeenChangelog;
            set => coreConfig.Data.LastSeenChangelog = value;
        }
        public ChangelogShowOptions ChangelogNotificationOptions
        {
            get => coreConfig.Data.ChangelogNotificationOptions;
            set => coreConfig.Data.ChangelogNotificationOptions = value;
        }
    }
}