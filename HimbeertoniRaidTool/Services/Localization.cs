using System.IO;
using Dalamud.Logging;
using Dalamud.Plugin;
namespace HimbeertoniRaidTool.Plugin.Services;

internal static class Localization
{
    private static bool FallBack = true;
    private static Dalamud.Localization? Loc;
    private static readonly Dictionary<string, string> _localizationCache = new();
    internal static void Init(DalamudPluginInterface pluginInterface)
    {
        if (Loc is not null)
            return;
        string localePath = Path.Combine(pluginInterface.AssemblyLocation.DirectoryName!, @"locale");
        Loc = new(localePath, "HimbeertoniRaidTool_");
        Loc.SetupWithLangCode(pluginInterface.UiLanguage);
        pluginInterface.LanguageChanged += OnLanguageChanged;
    }
    public static string Localize(string id, string fallBack)
    {
        if (FallBack || Loc is null)
            return fallBack;
        if (_localizationCache.TryGetValue(id, out string? val))
            return val;
        else
            return _localizationCache[id] = Dalamud.Localization.Localize(id, fallBack);
    }
    private static void OnLanguageChanged(string langCode)
    {
        FallBack = langCode.Equals("en");
        PluginLog.Information($"Loading localization for {langCode}");
        Loc?.SetupWithLangCode(langCode);
        _localizationCache.Clear();
    }
    public static void Dispose()
    {
        ServiceManager.PluginInterface.LanguageChanged -= OnLanguageChanged;
    }
#if DEBUG
    internal static void ExportLocalizable(bool ignore = true) => Loc?.ExportLocalizable(ignore);
#endif
}
