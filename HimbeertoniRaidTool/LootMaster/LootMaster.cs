using System.Collections.Generic;
using HimbeertoniRaidTool.Data;

namespace HimbeertoniRaidTool.LootMaster
{
    public static class LootMaster
    {
        internal static readonly LootmasterUI Ui = new();
        internal static List<RaidGroup> RaidGroups => DataManagement.DataManager.Groups;
        internal static void Init()
        {
            bool fillSolo = false;
            if (RaidGroups.Count == 0)
            {
                RaidGroups.Add(new("Solo", GroupType.Solo));
                fillSolo = true;
            }
            if (RaidGroups[0].Type != GroupType.Solo || !RaidGroups[0].Name.Equals("Solo"))
            {
                RaidGroups.Insert(0, new("Solo", GroupType.Solo));
                fillSolo = true;
            }
            if (fillSolo)
                FillSoloChar();
            GearRefresherOnExamine.Enable();
        }
        private static void FillSoloChar()
        {
            var character = Helper.Self;
            if (character == null)
                return;
            Player p = RaidGroups[0].Tank1;
            Character c = new Character(character.Name.TextValue, character.HomeWorld.Id);
            DataManagement.DataManager.GetManagedCharacter(ref c);
            p.MainChar = c;
            c.MainClassType = Helper.GetClass(character) ?? AvailableClasses.AST;
            c.MainClass.Level = character.Level;
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
