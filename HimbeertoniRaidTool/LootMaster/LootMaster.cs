using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using System;
using static HimbeertoniRaidTool.Connectors.EtroConnector;

namespace HimbeertoniRaidTool.LootMaster
{

	public class LootMaster : IDisposable
	{
        private HRTPlugin parent;
        public LootmasterUI Ui;
		private RaidGroup Group;
		public LootMaster(HRTPlugin plugin) : this(plugin, new RaidGroup("")) { }
        public LootMaster(HRTPlugin plugin, RaidGroup group)
        {
            this.parent = plugin;
            this.Group = group;
            this.Ui = new(parent, group);
        }
#if DEBUG
        public void Test()
        {
            try
            {
                this.Group.Heal1.NickName = "Patrick";
                this.Group.Heal1.MainChar = new Character("Mira Sorali");
                this.Group.Heal1.MainChar.MainClassType = AvailableClasses.WHM;
                GearSet gear = this.Group.Heal1.MainChar.MainClass.Gear;
                gear.Weapon = new GearItem(35253);
                if (!GetGearStats(gear.Weapon))
                    PluginLog.LogError("Etro Failed");
            }catch (Exception e)
            {
                
                PluginLog.LogFatal(e.Message);
                PluginLog.LogFatal(e.StackTrace ?? "");
                PluginLog.LogFatal(e.Data.ToString() ?? "");
            }
        }
#endif
        public void OnCommand(string args)
        {
            switch(args)
            {
#if DEBUG
                case "test":
                    Test();
                    break;
#endif
                default:
                    this.Ui.Show();
                    break;
            } 
        }

        public void Dispose()
        {
            this.Ui.Dispose();
            parent.Configuration.UpdateRaidGroup(Group);
        }
	}
}
