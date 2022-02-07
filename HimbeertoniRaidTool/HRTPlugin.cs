using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using HimbeertoniRaidTool.LootMaster;
using Dalamud.Game.Gui;

namespace HimbeertoniRaidTool
{
    public sealed class HRTPlugin : IDalamudPlugin
    {
        public string Name => "Himbeertoni Raid Tool";

        private const string commandName = "/hrt";
        private const string LootMasterCommand = "/lootmaster";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        internal Configuration Configuration { get; init; }
        private ConfigUI OptionsUi { get; init; }
        private LootMaster.LootMaster LM { get; init; }
        public ChatGui chat;
        public HRTPlugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ChatGui chat)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.chat = chat;
            chat.Enable();
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.LM = new(this);//TODO: Get Saved Values
            this.OptionsUi = new(this);
            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });
            this.CommandManager.AddHandler(LootMasterCommand, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens LootMaster Window"
            });

            //this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += OptionsUi.Draw;
        }

        public void Dispose()
        {
            this.OptionsUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
            this.CommandManager.RemoveHandler(LootMasterCommand);
        }
        private void OnCommand(string command, string args)
        {
            switch(command)
            {
                case "/hrt":
                    this.OptionsUi.Show();
                    break;
                case "/lootmaster":
                    this.LM.OnCommand(args);
                    break;
                default:
                    chat.PrintError("Command \"" + command + "not found");
                    break;
            }            
        }
    }
}
