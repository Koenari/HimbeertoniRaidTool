using System.IO;
using Dalamud.Plugin;

namespace HimbeertoniRaidTool.Plugin.Services;

internal static class Localization
{
    private static bool FallBack = true;
    private static Dalamud.Localization? Loc;
    private static readonly Dictionary<string, string> LocalizationCache = new();

    internal static void Init(DalamudPluginInterface pluginInterface)
    {
        if (Loc is not null)
            return;
        string localePath = Path.Combine(pluginInterface.AssemblyLocation.DirectoryName!, @"locale");
        Loc = new Dalamud.Localization(localePath, "HimbeertoniRaidTool_");
        Loc.SetupWithLangCode(pluginInterface.UiLanguage);
        pluginInterface.LanguageChanged += OnLanguageChanged;
    }

    public static string Localize(string id, string fallBack)
    {
        if (FallBack || Loc is null)
            return fallBack;
        if (LocalizationCache.TryGetValue(id, out string? val))
            return val;
        else
            return LocalizationCache[id] = Dalamud.Localization.Localize(id, fallBack);
    }

    private static void OnLanguageChanged(string langCode)
    {
        FallBack = langCode.Equals("en");
        ServiceManager.PluginLog.Information($"Loading localization for {langCode}");
        Loc?.SetupWithLangCode(langCode);
        LocalizationCache.Clear();
    }

    public static void Dispose()
    {
        ServiceManager.PluginInterface.LanguageChanged -= OnLanguageChanged;
    }

    internal static void ExportLocalizable(bool ignore = true)
    {
        Loc?.ExportLocalizable(ignore);
    }
}