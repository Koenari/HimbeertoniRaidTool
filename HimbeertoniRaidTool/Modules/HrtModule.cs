using Dalamud.Game;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;

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
    void Update(Framework fw);
    void Dispose();
}
public struct HrtCommand
{
    /// <summary>
    /// Needs to start with a "/"
    /// </summary>
    internal string Command;
    internal string Description;
    internal bool ShowInHelp;
    internal Action<string> OnCommand;
    internal string getCommandBackup(string command) {
        return command.Replace("/", "/hrt");
    }
}
