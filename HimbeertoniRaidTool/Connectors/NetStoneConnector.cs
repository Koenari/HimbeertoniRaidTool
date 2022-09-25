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

        public static async Task DebugToConsole(string charname)
        {
            lodestoneClient ??= await LodestoneClient.GetClientAsync();
            try
            {
                Dalamud.Logging.PluginLog.Log("Got Client with type {0}.", lodestoneClient.GetType());
            }
            catch
            {
                Dalamud.Logging.PluginLog.Error("Something went wrong with fetching Data from Lodestone.");
            }
        }
    }
}
