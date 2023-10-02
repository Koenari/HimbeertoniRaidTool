using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules;

public interface IHrtModule<T, S> : IHrtModule where T : new() where S : IHrtConfigUi
{
    HRTConfiguration<T, S> Configuration { get; }
}

public interface IHrtModule
{
    string Name { get; }
    string InternalName { get; }
    string Description { get; }
    WindowSystem WindowSystem { get; }
    IEnumerable<HrtCommand> Commands { get; }
    void HandleMessage(HrtUiMessage message);
    void AfterFullyLoaded();
    void Update();
    void Dispose();
}

public struct HrtCommand
{
    /// <summary>
    /// Command user needs to use in chat. Needs to start with a "/"
    /// </summary>
    internal string Command = string.Empty;

    internal IEnumerable<string> AltCommands = Array.Empty<string>();
    internal string Description = string.Empty;
    internal bool ShowInHelp = false;
    internal CommandInfo.HandlerDelegate OnCommand = (_, _) => { };
    internal bool ShouldExposeToDalamud = false;
    internal bool ShouldExposeAltsToDalamud = false;

    public HrtCommand()
    {
    }

    internal readonly bool HandlesCommand(string command)
    {
        return Command.Equals(command) || AltCommands.Any(c => c.Equals(command));
    }
}