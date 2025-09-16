using System.Globalization;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.Localization;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class LocalizationManager : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ILogger _logger;
    public CultureInfo CurrentLocale { get; private set; }
    public LocalizationManager(IDalamudPluginInterface pluginInterface, ILogger logger)
    {
        _pluginInterface = pluginInterface;
        _logger = logger;
        _logger.Debug(
            "Initializing Localization Manager with locale {PluginInterfaceUiLanguage}", pluginInterface.UiLanguage);
        _pluginInterface.LanguageChanged += OnLanguageChange;
        OnLanguageChanged += CommonLibrary.SetLanguage;
        CurrentLocale = new CultureInfo(_pluginInterface.UiLanguage);
        OnLanguageChange(_pluginInterface.UiLanguage);
    }

    public event Action<CultureInfo>? OnLanguageChanged;
    private void OnLanguageChange(string languageCode)
    {

        try
        {
            _logger.Information("Loading Localization for {LanguageCode}", languageCode);
            CurrentLocale = new CultureInfo(languageCode);
            GeneralLoc.Culture = CurrentLocale;
            OnLanguageChanged?.Invoke(CurrentLocale);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unable to Load localization for {LanguageCode}", languageCode);
        }
    }
    public void Dispose()
    {
        OnLanguageChanged = null;
        _pluginInterface.LanguageChanged -= OnLanguageChange;
    }
}