using Dalamud.Logging;
using HimbeertoniRaidTool.Connectors;
using HimbeertoniRaidTool.Data;
using System;
using static HimbeertoniRaidTool.Data.Player;

namespace HimbeertoniRaidTool.LootMaster
{

	public class LootMaster : IDisposable
	{
        private GearConnector GearDB;
        private HRTPlugin parent;
        public LootmasterUI Ui;
		private RaidGroup Group;
		public LootMaster(HRTPlugin plugin) : this(plugin, new RaidGroup("")) { }
        public LootMaster(HRTPlugin plugin, RaidGroup group)
        {
            this.parent = plugin;
            this.Group = group;
            this.Ui = new(parent, group);
            this.GearDB = new EtroConnector();
        }
#if DEBUG
        public async void Test()
        {
            try
            {
                PluginLog.LogDebug("Lootmaster test started");
                this.Group.Heal1.NickName = "Patrick";
                this.Group.Heal1.SetMainChar(new Character("Mira Sorali"));

                PluginLog.LogDebug("Lootmaster test Pre Get Char");
                Character mira = this.Group.Heal1.GetMainChar();
                mira.AddClass(AvailableClasses.WHM);
                mira.MainClass = AvailableClasses.WHM;
                PluginLog.LogDebug("Lootmaster test Pre Get Class and Gear");
                GearSet gear = mira.getMainClass().Gear;
                PluginLog.LogDebug("Lootmaster test Post Get Class and Gear");
                gear.Weapon = new GearItem(35253);
                PluginLog.LogDebug("Pre Stat Load: " + gear.Weapon.name + " has i Lvl " + gear.Weapon.itemLevel);
                if (!await GearDB.GetGearStats(gear.Weapon))
                    PluginLog.LogError("Etro Failed");
                PluginLog.LogDebug("Post Stat Load: " + gear.Weapon.name + " has i Lvl " + gear.Weapon.itemLevel);

                PluginLog.LogDebug("Lootmaster test finished");
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
                case "test2":
                    PluginLog.LogDebug("Test 2 started");
                    Character mira = this.Group.Heal1.GetMainChar();
                    PluginLog.LogDebug("Loaded Char");
                    GearSet gear = mira.getMainClass().Gear;
                    PluginLog.LogDebug("Loaded Gearset");
                    PluginLog.LogDebug("From Config: " + gear.Weapon.name + " has i Lvl " + gear.Weapon.itemLevel);
                    break;
#endif
                default:
                    this.Ui.Show();
                    break;
            } 
        }

        public void Dispose()
        {
            this.Ui.Dispose();
            parent.Configuration.UpdateRaidGroup(Group);
        }
	}
}
