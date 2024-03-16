namespace HimbeertoniRaidTool.Plugin.Services;

internal interface IGearDataProvider : IDisposable
{
    public void Enable(GearDataProviderConfiguration config);
    public void Disable();
}

public readonly struct GearDataProviderConfiguration(bool enabled, bool combat, bool doh, bool dol, int minILvl)
{
    public static GearDataProviderConfiguration Disabled => new(false, false, false, false, 0);

    public readonly bool Enabled = enabled;
    public readonly bool CombatJobsEnabled = combat;
    public readonly bool DoHEnabled = doh;
    public readonly bool DoLEnabled = dol;
    public readonly int MinILvlDowngrade = minILvl;

}