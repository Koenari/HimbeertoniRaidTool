namespace HimbeertoniRaidTool.Plugin.Connectors.Utils;

public readonly struct RateLimit
{
    public RateLimit(int requests, TimeSpan time)
    {
        MaxRequests = requests;
        Time = time;
    }
    public int MaxRequests { get; } = 1;
    public TimeSpan Time { get; } = new(0, 0, 5);
}