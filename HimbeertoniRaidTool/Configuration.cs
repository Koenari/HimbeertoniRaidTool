using Dalamud.Configuration;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Data;
using System;

namespace HimbeertoniRaidTool
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        public RaidGroup? GroupInfo;
        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }

        internal void UpdateRaidGroup(RaidGroup group)
        {
            this.GroupInfo = group;
            this.Save();
        }
    }
}
