using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

public class CommandManager
{

    private readonly List<HrtCommand> _registeredCommands = [];
    private readonly HashSet<string> _dalamudRegisteredCommands = [];
    private readonly ICommandManager _dalamudCommandManager;
    private readonly ILogger _logger;
    private readonly ConfigurationManager _configManager;
    private readonly IChatProvider _chat;
    private readonly OutOfTheBoxExperience _oobe;
    private readonly ChangelogService _changelog;
    public CommandManager(ICommandManager dalamudCommandManager,
                          ILogger logger,
                          ConfigurationManager configManager,
                          IChatProvider chat,
                          OutOfTheBoxExperience oobe,
                          ChangelogService changelog)
    {
        _dalamudCommandManager = dalamudCommandManager;
        _logger = logger;
        _configManager = configManager;
        _chat = chat;
        _oobe = oobe;
        _changelog = changelog;
        AddCommands(_internalCommands);
        RegisterToDalamud(new HrtCommand("/hrt", OnCommand, CoreLoc.Command_hrt_help)
        {
            ShouldExposeToDalamud = true,
        });
    }

    private IList<HrtCommand> _internalCommands =>
    [
        new("/options", _configManager.Show, CoreLoc.Command_hrt_options, ["/option", "/config"]),
        new("/welcome", _oobe.Show, CoreLoc.Command_hrt_welcome),
        new("/help", PrintUsage, CoreLoc.Command_hrt_help, ["/usage"]),
        new("/changelog", _changelog.ShowUi, CoreLoc.command_hrt_changelog),
    ];

    internal void AddCommand(HrtCommand command) => _registeredCommands.Add(command);

    internal void RemoveCommand(HrtCommand command) => _registeredCommands.Remove(command);

    internal void RemoveCommands(IEnumerable<HrtCommand> commands)
    {
        foreach (var command in commands)
        {
            if (_dalamudRegisteredCommands.Remove(command.Command))
                _dalamudCommandManager.RemoveHandler(command.Command);
            foreach (string altCommand in command.AltCommands)
            {
                if (_dalamudRegisteredCommands.Remove(altCommand))
                    _dalamudCommandManager.RemoveHandler(altCommand);
            }
            RemoveCommand(command);
        }
    }

    private void RegisterToDalamud(HrtCommand command)
    {
        if (!command.ShouldExposeToDalamud) return;
        if (!_dalamudRegisteredCommands.Contains(command.Command) && _dalamudCommandManager.AddHandler(
                command.Command,
                new CommandInfo(command.OnCommand)
                {
                    HelpMessage = command.Description,
                    ShowInHelp = command.ShowInHelp,
                }))
        {
            _dalamudRegisteredCommands.Add(command.Command);
        }


        if (!command.ShouldExposeAltsToDalamud) return;
        foreach (string alt in command.AltCommands)
        {
            if (!_dalamudRegisteredCommands.Contains(alt) && _dalamudCommandManager.AddHandler(alt,
                    new CommandInfo(command.OnCommand)
                    {
                        HelpMessage = command.Description,
                        ShowInHelp = false,
                    }))
                _dalamudRegisteredCommands.Add(alt);
        }
    }

    internal void AddCommands(IEnumerable<HrtCommand> commands)
    {
        foreach (var command in commands)
        {
            RegisterToDalamud(command);
            AddCommand(command);
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

        _chat.Print(stringBuilder.BuiltString);
    }

    internal void OnCommand(string command, string args)
    {
        if (!command.Equals("/hrt")) return;
        string subCommand = '/' + (args.IsNullOrEmpty() ? "help" : args.Split(' ')[0]);
        string newArgs = args.IsNullOrEmpty() ? "" : args[(subCommand.Length - 1)..].Trim();
        if (_registeredCommands.Any(x => x.HandlesCommand(subCommand)))
        {
            var handler = _registeredCommands.First(x => x.HandlesCommand(subCommand));
            _logger.Debug(
                "Send command \"{SubCommand}\" and args \"{NewArgs}\" to handler for {HandlerCommand}", subCommand,
                newArgs, handler.Command);
            handler.OnCommand(subCommand, newArgs);
        }
        else
            _logger.Error("Argument {Args} for command \"/hrt\" not recognized", args);
    }

    public void Dispose()
    {
        foreach (string command in _dalamudRegisteredCommands)
        {
            _logger.Error("Command \"{Command}\" was not removed by module unload", command);
            _dalamudCommandManager.RemoveHandler(command);
        }

    }
}