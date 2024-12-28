using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Plugin.Connectors.Utils;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Connectors;

public interface IReadOnlyGearConnector
{
    public bool BelongsToThisService(string url);
    public string GetId(string url);
    public string GetWebUrl(string id);
    public IList<string> GetNames(string id);
    public IReadOnlyDictionary<string, string> GetBiSList(Job job);
    internal HrtUiMessage UpdateAllSets(bool updateAll, int maxAgeInDays);
    public void RequestGearSetUpdate(GearSet set, Action<HrtUiMessage>? messageCallback = null,
                                     string taskName = "Gearset Update");
    public HrtUiMessage UpdateGearSet(GearSet set);
}

internal abstract class WebConnector
{
    private readonly ConcurrentDictionary<string, (DateTime time, HttpResponseMessage response)> _cachedRequests;
    private readonly TimeSpan _cacheTime;
    private readonly ConcurrentDictionary<string, DateTime> _currentRequests;
    private readonly RateLimit _rateLimit;
    protected readonly ILogger Logger;

    internal WebConnector(ILogger logger, RateLimit rateLimit = default, TimeSpan? cacheTime = null)
    {
        Logger = logger;
        _rateLimit = rateLimit;
        _cachedRequests = new ConcurrentDictionary<string, (DateTime time, HttpResponseMessage response)>();
        _currentRequests = new ConcurrentDictionary<string, DateTime>();
        _cacheTime = cacheTime ?? new TimeSpan(0, 15, 0);
    }

    private void UpdateCache()
    {
        foreach (var req in _cachedRequests.Where(e => e.Value.time + _cacheTime < DateTime.Now))
        {
            _cachedRequests.TryRemove(req.Key, out _);
        }
    }

    protected static string? GetContent(HttpResponseMessage? httpResponseMessage)
    {
        if (httpResponseMessage is null) return null;
        var stringTask = httpResponseMessage.Content.ReadAsStringAsync();
        stringTask.Wait();
        return stringTask.Result;
    }

    protected HttpResponseMessage? MakeWebRequest(string url)
    {
        UpdateCache();
        if (_cachedRequests.TryGetValue(url, out var result))
            return result.response;
        var requestTask = MakeAsyncWebRequest(url);
        requestTask.Wait();
        return requestTask.Result;
    }

    private async Task<HttpResponseMessage?> MakeAsyncWebRequest(string url)
    {
        while (RateLimitHit() || _currentRequests.ContainsKey(url))
        {
            Thread.Sleep(1000);
        }
        if (_cachedRequests.TryGetValue(url, out var cached))
            return cached.response;
        _currentRequests.TryAdd(url, DateTime.Now);
        try
        {
            HttpClient client = new();
            var response = await client.GetAsync(url);
            _cachedRequests.TryAdd(url, (DateTime.Now, response));
            return response;
        }
        catch (Exception e) when (e is HttpRequestException or UriFormatException or TaskCanceledException)
        {
            Logger.Error(e.Message);
            return null;
        }
        finally
        {
            _currentRequests.TryRemove(url, out _);
        }
    }

    private bool RateLimitHit() =>
        _currentRequests.Count + _cachedRequests.Count(e => e.Value.time + _rateLimit.Time > DateTime.Now) >
        _rateLimit.MaxRequests;
}