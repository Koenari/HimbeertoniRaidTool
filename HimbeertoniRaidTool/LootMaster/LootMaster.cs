using HimbeertoniRaidTool.Data;
using System;

namespace HimbeertoniRaidTool.LootMaster
{

	public class LootMaster : IDisposable
	{
        public readonly LootmasterUI Ui;
        public readonly LootRuling LootRuling;
		private readonly RaidGroup Group;
        private readonly GearRefresherOnExamine GearRefresher;
        public LootMaster(RaidGroup group, LootRuling lr)
        {
            Group = group;
            Ui = new(group);
            GearRefresher = new(Group);
            LootRuling = lr;
        }
        public void OnCommand(string args)
        {
            switch(args)
            {
                case "roll":
                    
                    break;
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
