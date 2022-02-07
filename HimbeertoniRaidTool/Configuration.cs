using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace HimbeertoniRaidTool
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;
        [NonSerialized]
        public HrtDB? LocalDB;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            this.LocalDB = new HrtDB();
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
    public class HrtDB 
    { 
    }
}
