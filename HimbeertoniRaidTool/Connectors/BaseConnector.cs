using System;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace HimbeertoniRaidTool.Connectors
{
    internal abstract class BaseConnector
    {
        private readonly RateLimit _rateLimit;
        {
        }
        internal string? MakeWebRequest(string URL)
            var requestTask = MakeAsyncWebRequest(URL);
            requestTask.Wait();
            return requestTask.Result;
        }
        internal async Task<string?> MakeAsyncWebRequest(string URL)
        {
            HttpClient client = new();
            try
            {
                var response = await client.GetAsync(URL);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();

            }
            catch (Exception e)
            {
                PluginLog.LogError(e.Message);
                return null;
            }
        }
    }
}
