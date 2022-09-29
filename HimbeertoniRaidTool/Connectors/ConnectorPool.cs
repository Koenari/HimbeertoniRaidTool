using Dalamud.Game;

namespace HimbeertoniRaidTool.Connectors
{
    internal class ConnectorPool
    {
        internal readonly EtroConnector EtroConnector;

        internal ConnectorPool(Framework fw)
        {
            EtroConnector = new(fw);
        }
    }
}
