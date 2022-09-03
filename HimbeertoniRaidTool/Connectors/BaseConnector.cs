using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace HimbeertoniRaidTool.Connectors
{
    internal static class BaseConnector
    {
        internal static string? MakeWebRequest(string URL)
        {
            var requestTask = MakeAsyncWebRequest(URL);
            requestTask.Wait();
            return requestTask.Result;
        }
        internal static async Task<string?> MakeAsyncWebRequest(string URL)
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
