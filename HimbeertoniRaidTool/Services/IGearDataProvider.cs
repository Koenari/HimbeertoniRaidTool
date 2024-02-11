namespace HimbeertoniRaidTool.Plugin.Services;

internal interface IGearDataProvider : IDisposable
{
    public void Enable(GearDataProviderConfiguration config);
    public void Disable();
}

public readonly struct GearDataProviderConfiguration
{
    public static GearDataProviderConfiguration Disabled => new(false, false, false, false, 0);

    public readonly bool Enabled;
    public readonly bool CombatJobsEnabled;
    public readonly bool DoHEnabled;
    public readonly bool DoLEnabled;
    public readonly int MinILvlDowngrade;

    public GearDataProviderConfiguration(bool enabled, bool combat, bool doh, bool dol, int minILvl)
    {
        Enabled = enabled;
        CombatJobsEnabled = combat;
        DoHEnabled = doh;
        DoLEnabled = dol;
        MinILvlDowngrade = minILvl;
    }
}