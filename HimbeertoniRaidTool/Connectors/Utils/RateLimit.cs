namespace HimbeertoniRaidTool.Plugin.Connectors.Utils;
public struct RateLimit
{
    public RateLimit(int requests, TimeSpan time)
    {
        MaxRequests = requests;
        Time = time;
    }
    public int MaxRequests { get; set; } = 1;
    public TimeSpan Time { get; set; } = new(0, 0, 5);
}
