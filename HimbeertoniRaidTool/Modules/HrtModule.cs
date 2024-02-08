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
    event Action UiReady;
    void HandleMessage(HrtUiMessage message);
    void AfterFullyLoaded();
    void PrintUsage(string command, string args);
    void Update();
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

internal class DalamudWindowSystem : IWindowSystem
{
    private readonly WindowSystem _windowSystemImplementation;
    public DalamudWindowSystem(WindowSystem implementation)
    {
        _windowSystemImplementation = implementation;
    }
    public void Draw() => _windowSystemImplementation.Draw();

    public void AddWindow(HrtWindow window) => _windowSystemImplementation.AddWindow(window);
    public void RemoveAllWindows() => _windowSystemImplementation.RemoveAllWindows();
    public void RemoveWindow(HrtWindow hrtWindow) => _windowSystemImplementation.RemoveWindow(hrtWindow);
    public IEnumerable<HrtWindow> Windows => _windowSystemImplementation.Windows.Cast<HrtWindow>();
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
    internal CommandInfo.HandlerDelegate OnCommand = (_, _) => { };
    internal bool ShouldExposeToDalamud = false;
    internal bool ShouldExposeAltsToDalamud = false;

    public HrtCommand()
    {
    }

    public HrtCommand(string command, CommandInfo.HandlerDelegate onCommand)
    {
        Command = command;
        OnCommand = onCommand;
    }

    public HrtCommand(string command, Action onCommand) : this(command, (_, _) => onCommand.Invoke()) { }

    internal readonly bool HandlesCommand(string command) =>
        Command.Equals(command) || AltCommands.Any(c => c.Equals(command));
}