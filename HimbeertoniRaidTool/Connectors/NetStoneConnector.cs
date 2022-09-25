using NetStone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Connectors
{
    internal static class NetStoneConnector
    {
        private static LodestoneClient? lodestoneClient;

        public static bool DebugConnector()
        {
            string? response = MakeNetstoneRequest();
            if (response == null)
                return false;
            Dalamud.Logging.PluginLog.Log(response);
            return true;
        }

        internal static string? MakeNetstoneRequest()
        {
            var requestedNetstoneTask = MakeAsyncNetstoneRequest();
            requestedNetstoneTask.Wait();
            return requestedNetstoneTask.Result;
        }

        internal static async Task<string?> MakeAsyncNetstoneRequest()
        {
            lodestoneClient ??= await LodestoneClient.GetClientAsync();
            return lodestoneClient.GetType().Name;
        }
    }
}
