using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Logging;

namespace HimbeertoniRaidTool.Connectors
{
    internal abstract class WebConnector
    {
        private readonly RateLimit _rateLimit;
        private readonly TimeSpan _cacheTime;
        private readonly ConcurrentDictionary<string, (DateTime time, string response)> _cachedRequests;
        private readonly ConcurrentDictionary<string, DateTime> _currentRequests;
        internal WebConnector(Framework fw, RateLimit rateLimit = default, TimeSpan? cacheTime = null)
        {
            _rateLimit = rateLimit;
            _cachedRequests = new();
            _currentRequests = new();
            _cacheTime = cacheTime ?? new(0, 15, 0);
            fw.Update += Update;
        }
        private void Update(Framework fw)
        {
            foreach (var req in _cachedRequests.Where(e => e.Value.time + _cacheTime < DateTime.Now))
            {
                _cachedRequests.TryRemove(req.Key, out _);
            }
        }
        protected string? MakeWebRequest(string URL)
        {
            if (_cachedRequests.TryGetValue(URL, out var result))
                return result.response;
            var requestTask = MakeAsyncWebRequest(URL);
            requestTask.Wait();
            return requestTask.Result;
        }
        private async Task<string?> MakeAsyncWebRequest(string URL)
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
            public int MaxRequests { get; set; } = 1;
            public TimeSpan Time { get; set; } = new(0, 0, 5);
        }
    }
}
