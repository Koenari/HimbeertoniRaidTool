using System.Globalization;
using Dalamud.Game.Command;
using Dalamud.Interface;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules;

public interface IHrtModule
{
    static abstract string Name { get; }
    static abstract string InternalName { get; }
    static abstract string Description { get; }
    static abstract bool CanBeDisabled { get; }
    internal IList<HrtCommand> Commands { get; }
    internal IList<ButtenDescriptor> GlobalButtons { get; }
    internal IModuleServiceContainer Services { get; }
    internal void HandleMessage(HrtUiMessage message);
    internal void AfterFullyLoaded();
    internal void OnLanguageChange(CultureInfo culture);
    internal void Dispose();
}

internal interface IHrtModule<out TModule, TConfig> : IHrtModule
    where TModule : IHrtModule where TConfig : IHrtModuleConfiguration
{
    static abstract TModule Create(IModuleServiceContainer services, TConfig configuration);
    static abstract TConfig CreateConfiguration(IModuleServiceContainer services);
}

public readonly record struct ButtenDescriptor(FontAwesomeIcon Icon, string Id, string ToolTip, Action OnClick);

public readonly record struct HrtCommand
{
    /// <summary>
    ///     Command user needs to use in chat. Needs to start with a "/"
    /// </summary>
    internal string Command { get; }
    internal IList<string> AltCommands { get; } = [];
    internal string Description { get; } = string.Empty;
    internal IReadOnlyCommandInfo.HandlerDelegate OnCommand { get; }
    internal bool ShouldExposeToDalamud { get; init; } = false;
    internal bool ShouldExposeAltsToDalamud { get; init; } = false;
    internal IList<(string argument, string helpText)> AdditionalArgumentHelp { get; init; } = [];

    public HrtCommand(string command, IReadOnlyCommandInfo.HandlerDelegate onCommand, string description,
                      IList<string>? altCommands = null)
    {
        Command = command;
        OnCommand = onCommand;
        Description = description;
        AltCommands = altCommands ?? [];
    }

    public HrtCommand(string command, Action onCommand, string description, IList<string>? altCommands = null) : this(
        command, (_, _) => onCommand.Invoke(), description, altCommands) { }

    internal bool HandlesCommand(string command) =>
        Command.Equals(command, StringComparison.OrdinalIgnoreCase)
     || AltCommands.Any(c => c.Equals(command, StringComparison.OrdinalIgnoreCase));

}