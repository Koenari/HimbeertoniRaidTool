using HimbeertoniRaidTool.Data;
using System;
using System.Collections.Generic;

namespace HimbeertoniRaidTool.LootMaster
{
    public static class LootMaster
    {
        internal static readonly LootmasterUI Ui = new();
        [Obsolete("")]
        internal static RaidGroup MainGroup => RaidGroups[0];
        internal static List<RaidGroup> RaidGroups => HRTPlugin.Configuration.RaidGroups;
        static LootMaster()
        {
            if (RaidGroups.Count == 0)
                RaidGroups.Add(new());
            GearRefresherOnExamine.Enable();
        }
        public static void OnCommand(string args)
        {
            switch (args)
            {
                default:
                    Ui.Show();
                    break;
            }
        }
        public static void Dispose()
        {
            GearRefresherOnExamine.Dispose();
            Ui.Dispose();
        }
    }
}
