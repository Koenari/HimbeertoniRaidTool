using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules;

public interface IHrtModule
{
    string Name { get; }
    string InternalName { get; }
    string Description { get; }
    IHrtConfiguration Configuration { get; }
    IWindowSystem WindowSystem { get; }
    IEnumerable<HrtCommand> Commands { get; }
    IServiceContainer Services { get; }
    event Action UiReady;
    void HandleMessage(HrtUiMessage message);
    void AfterFullyLoaded();
    void PrintUsage(string command, string args);
    void OnLanguageChange(string langCode);
    void Dispose();
}

public interface IWindowSystem
{
    public IEnumerable<HrtWindow> Windows { get; }
    void Draw();
    void AddWindow(HrtWindow ui);
    void RemoveAllWindows();
    void RemoveWindow(HrtWindow hrtWindow);
}

internal class DalamudWindowSystem(WindowSystem implementation) : IWindowSystem
{
    public void Draw() => implementation.Draw();

    public void AddWindow(HrtWindow window) => implementation.AddWindow(window);
    public void RemoveAllWindows() => implementation.RemoveAllWindows();
    public void RemoveWindow(HrtWindow hrtWindow) => implementation.RemoveWindow(hrtWindow);
    public IEnumerable<HrtWindow> Windows => implementation.Windows.Cast<HrtWindow>();
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