using HimbeertoniRaidTool.Data;
using System;

namespace HimbeertoniRaidTool.LootMaster
{
	public class LootMaster : IDisposable
	{
        public readonly LootmasterUI Ui;
		private readonly RaidGroup Group;
        private readonly GearRefresherOnExamine GearRefresher;
        public LootMaster(RaidGroup group, LootRuling lr)
        {
            Group = group;
            Ui = new(group);
            GearRefresher = new(Group);
        }
        public void OnCommand(string args)
        {
            switch(args)
            {
                default:
                    Ui.Show();
                    break;
            } 
        }
        public void Dispose()
        {
            GearRefresher.Dispose();
            Ui.Dispose();
            HRTPlugin.Configuration.GroupInfo = Group;
        }
	}
}
