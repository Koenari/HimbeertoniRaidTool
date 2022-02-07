using HimbeertoniRaidTool.Connectors;
using HimbeertoniRaidTool.Data;
using System;

namespace HimbeertoniRaidTool.LootMaster
{

	public class LootMaster
	{
        private GearConnector GearDB;
        private HRTPlugin parent;
        public LootmasterUI Ui;
		private RaidGroup group = new();
		public LootMaster(HRTPlugin plugin)
		{
            this.parent = plugin;
            this.Ui = new(parent);
            this.GearDB = new EtroConnector();
		}
		public void Test()
        {

        }
		public async void OnCommand(string args)
        {
            switch(args)
            {
                case "test":
                    Dalamud.Logging.PluginLog.LogDebug("Lootmaster test started");
                    this.group.Heal1.NickName = "Patrick";
                    this.group.Heal1.SetMainChar(new Character("Mira Sorali"));
                    Character mira = this.group.Heal1.GetMainChar();
                    mira.AddClass(AvailableClasses.WHM);
                    GearSet gear = mira.getClass(AvailableClasses.WHM).Gear;
                    gear.Weapon = new GearItem(35253);
                    Dalamud.Logging.PluginLog.LogDebug("Pre Stat Load: " + gear.Weapon.name + " has i Lvl " + gear.Weapon.itemLevel);
                    if (!await GearDB.GetGearStats(gear.Weapon))
                        Dalamud.Logging.PluginLog.LogError("Etro Failed");
                    Dalamud.Logging.PluginLog.LogDebug("Pre Stat Load: " + gear.Weapon.name + " has i Lvl " + gear.Weapon.itemLevel);

                    Dalamud.Logging.PluginLog.LogDebug("Lootmaster test finished");
                    break;
                default:
                    this.Ui.Show();
                    break;
            } 
        }
	}
}
