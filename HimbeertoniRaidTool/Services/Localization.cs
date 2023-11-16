using System.IO;
using Dalamud.Plugin;

namespace HimbeertoniRaidTool.Plugin.Services;

internal static class Localization
{
    private static bool _fallBack = true;
    private static Dalamud.Localization? _loc;
    private static readonly Dictionary<string, string> _localizationCache = new();

    internal static void Init(DalamudPluginInterface pluginInterface)
    {
        if (_loc is not null)
            return;
        string localePath = Path.Combine(pluginInterface.AssemblyLocation.DirectoryName!, @"locale");
        _loc = new Dalamud.Localization(localePath, "HimbeertoniRaidTool_");
        _loc.SetupWithLangCode(pluginInterface.UiLanguage);
        pluginInterface.LanguageChanged += OnLanguageChanged;
    }
    public static string Localize(string id) => Localize(id, id);
    public static string Localize(string id, string fallBack)
    {
        if (_fallBack || _loc is null)
            return fallBack;
        if (_localizationCache.TryGetValue(id, out string? val))
            return val;
        else
            return _localizationCache[id] = Dalamud.Localization.Localize(id, fallBack);
    }

    private static void OnLanguageChanged(string langCode)
    {
        _fallBack = langCode.Equals("en");
        ServiceManager.PluginLog.Information($"Loading localization for {langCode}");
        _loc?.SetupWithLangCode(langCode);
        _localizationCache.Clear();
    }

    public static void Dispose()
    {
        ServiceManager.PluginInterface.LanguageChanged -= OnLanguageChanged;
    }

    internal static void ExportLocalizable(bool ignore = true)
    {
        _loc?.ExportLocalizable(ignore);
    }
}