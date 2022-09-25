using Dalamud.Game;

namespace HimbeertoniRaidTool.Connectors
{
    internal class ConnectorPool
    {
        internal readonly EtroConnector EtroConnector;

        internal ConnectorPool(Framework fw)
        {
            EtroConnector = new();
            fw.Update += Update;
        }
        private void Update(Framework fw)
        {
            EtroConnector.Update();
        }
    }
}
