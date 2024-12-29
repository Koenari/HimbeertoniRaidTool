using Dalamud.Game.Command;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules;

public interface IHrtModule
{
    string Name { get; }
    string InternalName { get; }
    string Description { get; }
    IHrtConfiguration Configuration { get; }
    IEnumerable<HrtCommand> Commands { get; }
    IModuleServiceContainer Services { get; }
    event Action UiReady;
    void HandleMessage(HrtUiMessage message);
    void AfterFullyLoaded();
    void PrintUsage(string command, string args);
    void OnLanguageChange(string langCode);
    void Dispose();
}

public struct HrtCommand
{
    /// <summary>
    ///     Command user needs to use in chat. Needs to start with a "/"
    /// </summary>
    internal string Command = string.Empty;

    internal IEnumerable<string> AltCommands = Array.Empty<string>();
    internal string Description = string.Empty;
    internal bool ShowInHelp = true;
    internal IReadOnlyCommandInfo.HandlerDelegate OnCommand = (_, _) => { };
    internal bool ShouldExposeToDalamud = false;
    internal bool ShouldExposeAltsToDalamud = false;

    public HrtCommand()
    {
    }

    public HrtCommand(string command, IReadOnlyCommandInfo.HandlerDelegate onCommand)
    {
        Command = command;
        OnCommand = onCommand;
    }

    public HrtCommand(string command, Action onCommand) : this(command, (_, _) => onCommand.Invoke()) { }

    internal readonly bool HandlesCommand(string command) =>
        Command.Equals(command) || AltCommands.Any(c => c.Equals(command));
}