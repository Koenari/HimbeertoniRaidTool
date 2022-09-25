using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace HimbeertoniRaidTool.Connectors
{
    internal abstract class BaseConnector
    {
        private readonly RateLimit _rateLimit;
        private readonly TimeSpan _cacheTime;
        private readonly ConcurrentDictionary<string, (DateTime time, string response)> _cachedRequests;
        private readonly ConcurrentDictionary<string, DateTime> _currentRequests;

        internal BaseConnector(RateLimit rateLimit, TimeSpan cacheTime)
        {
            _rateLimit = rateLimit;
            _cachedRequests = new();
            _currentRequests = new();
            _cacheTime = cacheTime;
        }
        internal void Update()
        {
            foreach (var req in _cachedRequests.Where(e => e.Value.time + _cacheTime < DateTime.Now))
            {
                _cachedRequests.TryRemove(req.Key, out _);
            }
        }
        internal string? MakeWebRequest(string URL)
        {
            if (_cachedRequests.TryGetValue(URL, out var result))
                return result.response;
            var requestTask = MakeAsyncWebRequest(URL);
            requestTask.Wait();
            return requestTask.Result;
        }
        internal async Task<string?> MakeAsyncWebRequest(string URL)
        {
            while (RateLimitHit() || _currentRequests.ContainsKey(URL))
                Thread.Sleep(1000);
            if (_cachedRequests.TryGetValue(URL, out var cached))
                return cached.response;
            _currentRequests.TryAdd(URL, DateTime.Now);
            try
            {
                HttpClient client = new();
                var response = await client.GetAsync(URL);
                response.EnsureSuccessStatusCode();
                string result = await response.Content.ReadAsStringAsync();
                _cachedRequests.TryAdd(URL, (DateTime.Now, result));
                return result;
            }
            catch (Exception e)
            {
                PluginLog.LogError(e.Message);
                return null;
            }
            finally
            {
                _currentRequests.TryRemove(URL, out _);
            }
        }
        private bool RateLimitHit()
        {
            return (_currentRequests.Count + _cachedRequests.Count(e => e.Value.time + _rateLimit.Time > DateTime.Now)) > _rateLimit.MaxRequests;
        }
        public struct RateLimit
        {
            public RateLimit(int requests, TimeSpan time)
            {
                MaxRequests = requests;
                Time = time;
            }
            public int MaxRequests { get; set; }
            public TimeSpan Time { get; set; }
        }
    }
}
