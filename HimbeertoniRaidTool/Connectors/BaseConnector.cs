using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal abstract class WebConnector
{
    private readonly ConcurrentDictionary<string, (DateTime time, string response)> _cachedRequests;
    private readonly TimeSpan _cacheTime;
    private readonly ConcurrentDictionary<string, DateTime> _currentRequests;
    private readonly RateLimit _rateLimit;

    internal WebConnector(RateLimit rateLimit = default, TimeSpan? cacheTime = null)
    {
        _rateLimit = rateLimit;
        _cachedRequests = new ConcurrentDictionary<string, (DateTime time, string response)>();
        _currentRequests = new ConcurrentDictionary<string, DateTime>();
        _cacheTime = cacheTime ?? new TimeSpan(0, 15, 0);
    }

    private void UpdateCache()
    {
        foreach (var req in _cachedRequests.Where(e => e.Value.time + _cacheTime < DateTime.Now))
            _cachedRequests.TryRemove(req.Key, out _);
    }

    protected string? MakeWebRequest(string URL)
    {
        UpdateCache();
        if (_cachedRequests.TryGetValue(URL, out (DateTime time, string response) result))
            return result.response;
        var requestTask = MakeAsyncWebRequest(URL);
        requestTask.Wait();
        return requestTask.Result;
    }

    private async Task<string?> MakeAsyncWebRequest(string URL)
    {
        while (RateLimitHit() || _currentRequests.ContainsKey(URL))
            Thread.Sleep(1000);
        if (_cachedRequests.TryGetValue(URL, out (DateTime time, string response) cached))
            return cached.response;
        _currentRequests.TryAdd(URL, DateTime.Now);
        try
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(URL);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();
            _cachedRequests.TryAdd(URL, (DateTime.Now, result));
            return result;
        }
        catch (Exception e) when (e is HttpRequestException or UriFormatException or TaskCanceledException)
        {
            ServiceManager.PluginLog.Error(e.Message);
            return null;
        }
        finally
        {
            _currentRequests.TryRemove(URL, out _);
        }
    }

    private bool RateLimitHit()
    {
        return _currentRequests.Count + _cachedRequests.Count(e => e.Value.time + _rateLimit.Time > DateTime.Now) >
               _rateLimit.MaxRequests;
    }
}