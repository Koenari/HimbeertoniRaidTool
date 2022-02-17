using HimbeertoniRaidTool.Data;
using System;

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
            GearRefresher = new(Group);
        }
        public void OnCommand(string args)
        {
            switch(args)
            {
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
