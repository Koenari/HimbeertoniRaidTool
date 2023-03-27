using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.HrtServices;
#pragma warning disable CS8618
namespace HimbeertoniRaidTool.Plugin.HrtServices { }
namespace HimbeertoniRaidTool.Plugin
{
    internal class Services
    {
        [PluginService] public static SigScanner SigScanner { get; private set; }
        [PluginService] public static CommandManager CommandManager { get; private set; }
        [PluginService] public static ChatGui ChatGui { get; private set; }
        [PluginService] public static DataManager DataManager { get; private set; }
        [PluginService] public static GameGui GameGui { get; private set; }
        [PluginService] public static TargetManager TargetManager { get; private set; }
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public static ClientState ClientState { get; private set; }
        [PluginService] public static Framework Framework { get; private set; }
        [PluginService] public static ObjectTable ObjectTable { get; private set; }
        [PluginService] public static PartyList PartyList { get; private set; }
        [PluginService] public static Condition Condition { get; private set; }
        public static IconCache IconCache { get; private set; }
        public static HrtDataManager HrtDataManager { get; private set; }
        internal static TaskManager TaskManager { get; private set; }
        internal static ConnectorPool ConnectorPool { get; private set; }
        internal static Configuration Config { get; set; }
        internal static CharacterInfoService CharacterInfoService { get; private set; }
        internal static bool Init(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Services>();
            Common.Services.ServiceManager.Init(DataManager.Excel);
            IconCache ??= new IconCache(PluginInterface, DataManager);
            HrtDataManager ??= new(PluginInterface);
            TaskManager ??= new();
            ConnectorPool ??= new();
            CharacterInfoService ??= new(ObjectTable);
            GearRefresher.Enable();
            return HrtDataManager.Initialized;
        }
        internal static void Dispose()
        {
            GearRefresher.Dispose();
            TaskManager.Dispose();
            IconCache.Dispose();
        }
    }
}
#pragma warning restore CS8618
