using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.Modules.Core;
using HimbeertoniRaidTool.Plugin.UI;
using Lumina.Excel;

namespace HimbeertoniRaidTool.Plugin.Services;

public interface IWindowSystem
{
    public IEnumerable<HrtWindow> Windows { get; }
    void Draw();
    void AddWindow(HrtWindow ui);
    void RemoveAllWindows();
    void RemoveWindow(HrtWindow hrtWindow);
}

public interface IUiSystem : IWindowSystem
{
    public EditWindowFactory EditWindows { get; }
    public UiHelpers Helpers { get; }
    public IDalamudTextureWrap? GetIcon(uint id, bool isHq = false);
    public bool DrawConditionsMet();
    public ExcelSheet<TType> GetExcelSheet<TType>() where TType : struct, IExcelRow<TType>;
}

internal abstract class UiSystem : IUiSystem
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
        Helpers = new UiHelpers(this);

    }

    public IDalamudTextureWrap GetIcon(uint id, bool isHq = false) => Services.IconCache.LoadIcon(id, isHq);
    public ExcelSheet<TType> GetExcelSheet<TType>() where TType : struct, IExcelRow<TType> =>
        Services.DataManager.GetExcelSheet<TType>()
     ?? throw new NullReferenceException("UiSystem was not initialized");

    public bool DrawConditionsMet() =>
        !(CoreModule.UiConfig.HideInCombat && Services.Condition[ConditionFlag.InCombat])
     && !Services.Condition[ConditionFlag.BetweenAreas];
    public IEnumerable<HrtWindow> Windows => _windowSystem.Windows;
    public void Draw()
    {
        var toRemove = Windows.Where(window => window is { IsOpen: false, Persistent: false }).ToList();
        foreach (var window in toRemove)
        {
            Services.Logger.Debug($"Cleaning Up Window: {window.WindowName}");
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
    public void RemoveWindow(HrtWindow hrtWindow) => _windowSystem.RemoveWindow(hrtWindow);
}

internal class ModuleScopedUiSystem(IHrtModule module, IModuleServiceContainer services)
    : UiSystem(new DalamudWindowSystem(new WindowSystem($"HRT::{module.InternalName}")), services);

internal class GlobalUiSystem(IGlobalServiceContainer services)
    : UiSystem(new DalamudWindowSystem(new WindowSystem($"HRT")), services);

internal class DalamudWindowSystem(WindowSystem implementation) : IWindowSystem
{
    public void Draw() => implementation.Draw();
    public void AddWindow(HrtWindow window) => implementation.AddWindow(window);
    public void RemoveAllWindows() => implementation.RemoveAllWindows();
    public void RemoveWindow(HrtWindow hrtWindow) => implementation.RemoveWindow(hrtWindow);
    public IEnumerable<HrtWindow> Windows => implementation.Windows.Cast<HrtWindow>();
}