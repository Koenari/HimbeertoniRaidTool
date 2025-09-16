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
    public EditWindowFactory EditWindows { get; }
    public UiHelpers Helpers { get; }
    public IDalamudTextureWrap GetIcon(Item item);
    public IDalamudTextureWrap GetIcon(uint iconId, bool hq);
    public bool DrawConditionsMet();
    public ExcelSheet<TType> GetExcelSheet<TType>() where TType : struct, IExcelRow<TType>;

    public HrtDataManager GetHrtDataManager();

    public void OpenSettingsWindow();
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
        private IGlobalServiceContainer Services { get; }

        protected UiSystem(DalamudWindowSystem windowSystem, IGlobalServiceContainer services)
        {
            _windowSystem = windowSystem;
            Services = services;
            EditWindows = new EditWindowFactory(Services);
            Helpers = new UiHelpers(this, Services);

        }

        public IDalamudTextureWrap GetIcon(Item item) => GetIcon(item.Icon, item is HqItem { IsHq: true });
        public IDalamudTextureWrap GetIcon(uint iconId, bool hq) => Services.IconCache.LoadIcon(iconId, hq);
        public ExcelSheet<TType> GetExcelSheet<TType>() where TType : struct, IExcelRow<TType> =>
            Services.DataManager.GetExcelSheet<TType>()
         ?? throw new NullReferenceException("UiSystem was not initialized");

        public HrtDataManager GetHrtDataManager() => Services.HrtDataManager;

        public bool DrawConditionsMet() =>
            !(CoreModule.UiConfig.HideInCombat && Services.Condition[ConditionFlag.InCombat])
         && !Services.Condition[ConditionFlag.BetweenAreas];

        public void OpenSettingsWindow() => Services.ConfigManager.Show();

        public void Draw()
        {
            var toRemove = _windowSystem.Windows.Where(window => window is { IsOpen: false, Persistent: false })
                                        .ToList();
            foreach (var window in toRemove)
            {
                Services.Logger.Debug("Cleaning Up Window: {WindowWindowName}", window.WindowName);
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