using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using System;
using static HimbeertoniRaidTool.Connectors.DalamudConnector;

namespace HimbeertoniRaidTool.LootMaster
{

	public class LootMaster : IDisposable
	{
        public LootmasterUI Ui;
		private RaidGroup Group;
        private GearRefresherOnExamine GearRefresher;
        public LootMaster(HRTPlugin plugin) : this(new RaidGroup("")) { }
        public LootMaster(RaidGroup group)
        {
            this.Group = group;
            this.Ui = new(group);
            GearRefresher = new(this);
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
                gear.MainHand = new GearItem(35253);
                if (!GetGearStats(gear.MainHand))
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
            this.GearRefresher.Dispose();
            this.Ui.Dispose();
            HRTPlugin.Plugin.Configuration.UpdateRaidGroup(Group);
        }
	}
}
