using System.Globalization;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Plugin.Localization;

namespace HimbeertoniRaidTool.Plugin;

// ReSharper disable once UnusedMember.Global
public sealed class HrtPlugin : IDalamudPlugin
{

    private readonly IGlobalServiceContainer _services;

    public HrtPlugin(IDalamudPluginInterface pluginInterface)
    {
        GeneralLoc.Culture = new CultureInfo(pluginInterface.UiLanguage);
        //Init all services
        _services = ServiceManager.Init(pluginInterface);
        _services.ModuleManager.LoadAllModules();
        //Init Localization
        pluginInterface.LanguageChanged += OnLanguageChange;
        OnLanguageChange(pluginInterface.UiLanguage);
    }

    public void Dispose() => _services.Dispose();

    private void OnLanguageChange(string languageCode)
    {
        _services.Logger.Information($"Loading Localization for {languageCode}");
        CommonLibrary.SetLanguage(languageCode);
        try
        {
            var newLanguage = new CultureInfo(languageCode);
            GeneralLoc.Culture = newLanguage;
            _services.ModuleManager.OnLanguageChange(languageCode);
        }
        catch (Exception ex)
        {
            _services.Logger.Error(ex, $"Unable to Load localization for {languageCode}");
        }
    }
}