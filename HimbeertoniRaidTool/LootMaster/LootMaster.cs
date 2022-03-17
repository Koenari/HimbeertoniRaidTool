using HimbeertoniRaidTool.Data;
using System.Collections.Generic;

namespace HimbeertoniRaidTool.LootMaster
{
    public static class LootMaster
    {
        internal static readonly LootmasterUI Ui = new();
        internal static List<RaidGroup> RaidGroups => HRTPlugin.Configuration.RaidGroups;
        internal static void Init()
        {
            if (RaidGroups.Count == 0)
                RaidGroups.Add(new("Solo", GroupType.Solo));
            if (RaidGroups[0].Type != GroupType.Solo || !RaidGroups[0].Name.Equals("Solo"))
                RaidGroups.Insert(0, new("Solo", GroupType.Solo));
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
