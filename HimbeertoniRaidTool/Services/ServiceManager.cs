using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules.Core;
using HimbeertoniRaidTool.Plugin.UI;

#pragma warning disable CS8618
namespace HimbeertoniRaidTool.Plugin.Services;

internal class ServiceManager
{
    [PluginService] public static ISigScanner SigScanner { get; private set; }
    [PluginService] public static ICommandManager CommandManager { get; private set; }
    [PluginService] public static ChatGui ChatGui { get; private set; }
    [PluginService] public static IDataManager DataManager { get; private set; }
    [PluginService] public static IGameGui GameGui { get; private set; }
    [PluginService] public static ITargetManager TargetManager { get; private set; }
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; }
    [PluginService] public static IClientState ClientState { get; private set; }
    [PluginService] public static Framework Framework { get; private set; }
    [PluginService] public static IObjectTable ObjectTable { get; private set; }
    [PluginService] public static IPartyList PartyList { get; private set; }
    [PluginService] public static Condition Condition { get; private set; }
    public static IconCache IconCache { get; private set; }
    public static HrtDataManager HrtDataManager { get; private set; }
    internal static TaskManager TaskManager { get; private set; }
    internal static ConnectorPool ConnectorPool { get; private set; }
    internal static Configuration Config { get; set; }
    internal static CoreModule CoreModule { get; set; }
    internal static CharacterInfoService CharacterInfoService { get; private set; }
    internal static GameInfo GameInfo => Common.Services.ServiceManager.GameInfo;
    internal static ItemInfo ItemInfo => Common.Services.ServiceManager.ItemInfo;

    internal static bool Init(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<ServiceManager>();
        Common.Services.ServiceManager.Init(DataManager.Excel);
        IconCache ??= new IconCache(PluginInterface, DataManager);
        HrtDataManager ??= new HrtDataManager(PluginInterface);
        TaskManager ??= new TaskManager();
        ConnectorPool ??= new ConnectorPool();
        CharacterInfoService ??= new CharacterInfoService(ObjectTable, PartyList);
        //TODO: Move somewhere else
        TaskManager.RegisterTask(
            new HrtTask(() =>
            {
                HrtDataManager.PruneDatabase();
                return new HrtUiMessage();
            }, _ => { })
        );
        GearRefresher.Instance.Enable();
        return HrtDataManager.Initialized;
    }

    internal static void Dispose()
    {
        GearRefresher.Instance.Dispose();
        TaskManager.Dispose();
        IconCache.Dispose();
    }
}

public class FailedToLoadException : Exception
{
    public FailedToLoadException(string? message) : base(message)
    {
    }
}
#pragma warning restore CS8618