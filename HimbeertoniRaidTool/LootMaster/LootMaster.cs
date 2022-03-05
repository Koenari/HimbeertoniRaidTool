using HimbeertoniRaidTool.Data;
using System;

namespace HimbeertoniRaidTool.LootMaster
{
    public class LootMaster : IDisposable
    {
        public readonly LootmasterUI Ui;
        private readonly RaidGroup MainGroup;
        private readonly GearRefresherOnExamine GearRefresher;
        public LootMaster(RaidGroup group)
        {
            MainGroup = group;
            Ui = new(group);
            GearRefresher = new(MainGroup);
        }
        public void OnCommand(string args)
        {
            switch (args)
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
            HRTPlugin.Configuration.GroupInfo = MainGroup;
        }
    }
}
