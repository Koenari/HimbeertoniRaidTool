namespace HimbeertoniRaidTool.Plugin.Services;

internal interface IGearDataProvider : IDisposable
{
    public void Enable(GearDataProviderConfiguration config);
    public void Disable();
}

public readonly struct GearDataProviderConfiguration
{
    public static GearDataProviderConfiguration Disabled => new(false, false, false, false);

    public readonly bool Enabled;
    public readonly bool CombatJobsEnabled;
    public readonly bool DoHEnabled;
    public readonly bool DoLEnabled;

    public GearDataProviderConfiguration(bool enabled, bool combat, bool doh, bool dol)
    {
        Enabled = enabled;
        CombatJobsEnabled = combat;
        DoHEnabled = doh;
        DoLEnabled = dol;
    }
}