using System.Globalization;
using Dalamud.Game.Command;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules;

public interface IHrtModule
{
    static abstract string Name { get; }
    static abstract string InternalName { get; }
    static abstract string Description { get; }
    static abstract bool CanBeDisabled { get; }
    IHrtConfiguration Configuration { get; }
    IEnumerable<HrtCommand> Commands { get; }
    IModuleServiceContainer Services { get; }
    event Action UiReady;
    void HandleMessage(HrtUiMessage message);
    void AfterFullyLoaded();
    void PrintUsage(string command, string args);
    void OnLanguageChange(CultureInfo culture);
    void Dispose();
}

internal interface IHrtModule<TModule> : IHrtModule where TModule : IHrtModule
{
    public static abstract TModule Create(IModuleServiceContainer services);
}

public readonly record struct HrtCommand
{
    /// <summary>
    ///     Command user needs to use in chat. Needs to start with a "/"
    /// </summary>
    internal string Command { get; }
    internal IEnumerable<string> AltCommands { get; init; } = Array.Empty<string>();
    internal string Description { get; init; } = string.Empty;
    internal bool ShowInHelp { get; init; } = true;
    internal IReadOnlyCommandInfo.HandlerDelegate OnCommand { get; }
    internal bool ShouldExposeToDalamud { get; init; } = false;
    internal bool ShouldExposeAltsToDalamud { get; init; } = false;

    public HrtCommand(string command, IReadOnlyCommandInfo.HandlerDelegate onCommand)
    {
        Command = command;
        OnCommand = onCommand;
    }

    public HrtCommand(string command, Action onCommand) : this(command, (_, _) => onCommand.Invoke()) { }

    internal readonly bool HandlesCommand(string command) =>
        Command.Equals(command) || AltCommands.Any(c => c.Equals(command));
}