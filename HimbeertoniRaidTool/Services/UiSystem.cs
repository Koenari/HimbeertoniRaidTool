using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;
using Lumina.Excel;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

public interface IWindowSystem
{
    void Draw();
    void AddWindow(HrtWindow ui);
    void RemoveAllWindows();
}

public interface IUiSystem : IWindowSystem
{
    EditWindowFactory EditWindows { get; }
    UiHelpers Helpers { get; }
    IDalamudTextureWrap GetIcon(Item item);
    IDalamudTextureWrap GetIcon(uint iconId, bool hq);
    bool DrawConditionsMet();
    ExcelSheet<TType> GetExcelSheet<TType>() where TType : struct, IExcelRow<TType>;

    void OpenSearchWindow<TData>(Action<TData> onSelect, Action? onCancel = null)
        where TData : class, IHrtDataTypeWithId<TData>;
    IDataBaseTable<TData> GetDbTable<TData>() where TData : class, IHrtDataTypeWithId<TData>;

    void OpenSettingsWindow();
}

internal static class UiSystemFactory
{
    public static IUiSystem CreateUiSystem<TModule>(
        HrtDataManager hrtDataManager,
        ConnectorPool connectorPool,
        IDataManager dalamudDataManager,
        CharacterInfoService characterInfoService,
        TaskManager taskManager,
        ConfigurationManager configManager,
        IconCache iconCache,
        ICondition condition,
        ILogger logger)
        where TModule : IHrtModule =>
        new ModuleScopedUiSystem<TModule>(hrtDataManager, connectorPool, dalamudDataManager, characterInfoService,
                                          taskManager, configManager, iconCache, condition, logger);
    public static IUiSystem CreateGlobalUiSystem(
        HrtDataManager hrtDataManager,
        ConnectorPool connectorPool,
        IDataManager dalamudDataManager,
        CharacterInfoService characterInfoService,
        TaskManager taskManager,
        ConfigurationManager configManager,
        IconCache iconCache,
        ICondition condition,
        ILogger logger) => new GlobalUiSystem(hrtDataManager, connectorPool, dalamudDataManager, characterInfoService,
                                              taskManager, configManager, iconCache, condition, logger);

    private abstract class UiSystem : IUiSystem
    {
        private readonly DalamudWindowSystem _windowSystem;
        public EditWindowFactory EditWindows { get; }
        public UiHelpers Helpers { get; }
        private readonly IconCache _iconCache;
        private readonly IDataManager _dalamudDataManager;
        private readonly HrtDataManager _hrtDataManager;
        private readonly ICondition _condition;
        private readonly ConfigurationManager _configManager;
        private readonly ILogger _logger;

        protected UiSystem(DalamudWindowSystem windowSystem, HrtDataManager hrtDataManager, ConnectorPool connectorPool,
                           IDataManager dalamudDataManager, CharacterInfoService characterInfoService,
                           TaskManager taskManager, ConfigurationManager configManager, IconCache iconCache,
                           ICondition condition, ILogger logger)
        {
            _windowSystem = windowSystem;
            _dalamudDataManager = dalamudDataManager;
            _iconCache = iconCache;
            _hrtDataManager = hrtDataManager;
            _condition = condition;
            _configManager = configManager;
            _logger = logger;
            EditWindows = new EditWindowFactory(this, hrtDataManager, connectorPool,
                                                characterInfoService, taskManager);
            Helpers = new UiHelpers(this, configManager, hrtDataManager);

        }
        public IDalamudTextureWrap GetIcon(Item item) => GetIcon(item.Icon, item is HqItem { IsHq: true });
        public IDalamudTextureWrap GetIcon(uint iconId, bool hq) => _iconCache.LoadIcon(iconId, hq);
        public ExcelSheet<TType> GetExcelSheet<TType>() where TType : struct, IExcelRow<TType> =>
            _dalamudDataManager.GetExcelSheet<TType>()
         ?? throw new NullReferenceException("UiSystem was not initialized");

        public void OpenSearchWindow<TData>(Action<TData> onSelect, Action? onCancel = null)
            where TData : class, IHrtDataTypeWithId<TData> => _hrtDataManager.GetTable<TData>()
                                                                             .OpenSearchWindow(
                                                                                 this, onSelect, onCancel);

        public IDataBaseTable<TData> GetDbTable<TData>() where TData : class, IHrtDataTypeWithId<TData>
            => _hrtDataManager.GetTable<TData>();

        public bool DrawConditionsMet() =>
            !(_configManager.CoreConfig.Data.HideInCombat && _condition[ConditionFlag.InCombat])
         && !_condition[ConditionFlag.BetweenAreas];

        public void OpenSettingsWindow() => _configManager.Show();

        public void Draw()
        {
            var toRemove = _windowSystem.Windows.Where(window => window is { IsOpen: false, Persistent: false })
                                        .ToList();
            foreach (var window in toRemove)
            {
                _logger.Debug("Cleaning Up Window: {WindowWindowName}", window.WindowName);
                window.Dispose();
                _windowSystem.RemoveWindow(window);
            }

            _windowSystem.Draw();
        }
        public void AddWindow(HrtWindow ui)
        {
            if (!_windowSystem.Windows.Any(ui.Equals))
                _windowSystem.AddWindow(ui);
        }

        public void RemoveAllWindows() => _windowSystem.RemoveAllWindows();
    }

    private class ModuleScopedUiSystem<TModule>(
        HrtDataManager hrtDataManager,
        ConnectorPool connectorPool,
        IDataManager dalamudDataManager,
        CharacterInfoService characterInfoService,
        TaskManager taskManager,
        ConfigurationManager configManager,
        IconCache iconCache,
        ICondition condition,
        ILogger logger)
        : UiSystem(new DalamudWindowSystem(new WindowSystem($"HRT::{TModule.InternalName}")), hrtDataManager,
                   connectorPool, dalamudDataManager, characterInfoService, taskManager, configManager, iconCache,
                   condition, logger)
        where TModule : IHrtModule;

    private class GlobalUiSystem(
        HrtDataManager hrtDataManager,
        ConnectorPool connectorPool,
        IDataManager dalamudDataManager,
        CharacterInfoService characterInfoService,
        TaskManager taskManager,
        ConfigurationManager configManager,
        IconCache iconCache,
        ICondition condition,
        ILogger logger)
        : UiSystem(new DalamudWindowSystem(new WindowSystem($"HRT")), hrtDataManager, connectorPool, dalamudDataManager,
                   characterInfoService, taskManager, configManager, iconCache, condition, logger);

    private class DalamudWindowSystem(WindowSystem implementation) : IWindowSystem
    {
        public void Draw() => implementation.Draw();
        public void AddWindow(HrtWindow window) => implementation.AddWindow(window);
        public void RemoveAllWindows() => implementation.RemoveAllWindows();
        public void RemoveWindow(HrtWindow hrtWindow) => implementation.RemoveWindow(hrtWindow);
        public IEnumerable<HrtWindow> Windows => implementation.Windows.Cast<HrtWindow>();
    }
}