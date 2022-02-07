using HimbeertoniRaidTool.Connectors;
using HimbeertoniRaidTool.Data;
using System;

namespace HimbeertoniRaidTool.LootMaster
{

	public class LootMaster : IDisposable
	{
        private GearConnector GearDB;
        private HRTPlugin parent;
        public LootmasterUI Ui;
		private RaidGroup Group;
		public LootMaster(HRTPlugin plugin) : this(plugin, new RaidGroup()) { }
        public LootMaster(HRTPlugin plugin, RaidGroup group)
        {
            this.parent = plugin;
            this.Group = group;
            this.Ui = new(parent, group);
            this.GearDB = new EtroConnector();
        }

		public async void Test()
        {
            Dalamud.Logging.PluginLog.LogDebug("Lootmaster test started");
            this.Group.Heal1.NickName = "Patrick";
            this.Group.Heal1.SetMainChar(new Character("Mira Sorali"));
            Character mira = this.Group.Heal1.GetMainChar();
            mira.AddClass(AvailableClasses.WHM);
            GearSet gear = mira.getClass(AvailableClasses.WHM).Gear;
            gear.Weapon = new GearItem(35253);
            Dalamud.Logging.PluginLog.LogDebug("Pre Stat Load: " + gear.Weapon.name + " has i Lvl " + gear.Weapon.itemLevel);
            if (!await GearDB.GetGearStats(gear.Weapon))
                Dalamud.Logging.PluginLog.LogError("Etro Failed");
            Dalamud.Logging.PluginLog.LogDebug("Pre Stat Load: " + gear.Weapon.name + " has i Lvl " + gear.Weapon.itemLevel);

            Dalamud.Logging.PluginLog.LogDebug("Lootmaster test finished");
        }
		public void OnCommand(string args)
        {
            switch(args)
            {
                case "test":
                    Test();
                    break;
                default:
                    this.Ui.Show();
                    break;
            } 
        }

        public void Dispose()
        {
            parent.Configuration.UpdateRaidGroup(Group);
        }
	}
}
