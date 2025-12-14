using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Core;
using HimbeertoniRaidTool.Plugin.UI;
using Lumina.Excel;

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
    public static IUiSystem CreateUiSystem<TModule>(IModuleServiceContainer services)
        where TModule : IHrtModule =>
        new ModuleScopedUiSystem<TModule>(services);
    public static IUiSystem CreateGlobalUiSystem(IGlobalServiceContainer services) => new GlobalUiSystem(services);

    private abstract class UiSystem : IUiSystem
    {
        private readonly DalamudWindowSystem _windowSystem;
        public EditWindowFactory EditWindows { get; }
        public UiHelpers Helpers { get; }
        private IGlobalServiceContainer _services { get; }

        protected UiSystem(DalamudWindowSystem windowSystem, IGlobalServiceContainer services)
        {
            _windowSystem = windowSystem;
            _services = services;
            EditWindows = new EditWindowFactory(_services);
            Helpers = new UiHelpers(this, _services);

        }

        public IDalamudTextureWrap GetIcon(Item item) => GetIcon(item.Icon, item is HqItem { IsHq: true });
        public IDalamudTextureWrap GetIcon(uint iconId, bool hq) => _services.IconCache.LoadIcon(iconId, hq);
        public ExcelSheet<TType> GetExcelSheet<TType>() where TType : struct, IExcelRow<TType> =>
            _services.DataManager.GetExcelSheet<TType>()
         ?? throw new NullReferenceException("UiSystem was not initialized");

        public void OpenSearchWindow<TData>(Action<TData> onSelect, Action? onCancel = null)
            where TData : class, IHrtDataTypeWithId<TData> => _services.HrtDataManager.GetTable<TData>()
                                                                       .OpenSearchWindow(this, onSelect, onCancel);

        public IDataBaseTable<TData> GetDbTable<TData>() where TData : class, IHrtDataTypeWithId<TData>
            => _services.HrtDataManager.GetTable<TData>();

        public bool DrawConditionsMet() =>
            !(CoreModule.UiConfig.HideInCombat && _services.Condition[ConditionFlag.InCombat])
         && !_services.Condition[ConditionFlag.BetweenAreas];

        public void OpenSettingsWindow() => _services.ConfigManager.Show();

        public void Draw()
        {
            var toRemove = _windowSystem.Windows.Where(window => window is { IsOpen: false, Persistent: false })
                                        .ToList();
            foreach (var window in toRemove)
            {
                _services.Logger.Debug("Cleaning Up Window: {WindowWindowName}", window.WindowName);
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

    private class ModuleScopedUiSystem<TModule>(IModuleServiceContainer services)
        : UiSystem(new DalamudWindowSystem(new WindowSystem($"HRT::{TModule.InternalName}")), services)
        where TModule : IHrtModule;

    private class GlobalUiSystem(IGlobalServiceContainer services)
        : UiSystem(new DalamudWindowSystem(new WindowSystem($"HRT")), services);

    private class DalamudWindowSystem(WindowSystem implementation) : IWindowSystem
    {
        public void Draw() => implementation.Draw();
        public void AddWindow(HrtWindow window) => implementation.AddWindow(window);
        public void RemoveAllWindows() => implementation.RemoveAllWindows();
        public void RemoveWindow(HrtWindow hrtWindow) => implementation.RemoveWindow(hrtWindow);
        public IEnumerable<HrtWindow> Windows => implementation.Windows.Cast<HrtWindow>();
    }
}