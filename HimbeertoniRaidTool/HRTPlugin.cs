using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using HimbeertoniRaidTool.LootMaster;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using System;
using Dalamud.Logging;

namespace HimbeertoniRaidTool
{
    public sealed class HRTPlugin : IDalamudPlugin
    {
        public string Name => "Himbeertoni Raid Tool";

        private readonly List<Tuple<string, string, bool>> Commands = new List<Tuple<string, string, bool>> {
            new Tuple<string, string, bool>("/hrt" , "Does nothing at the moment", true ),
            new Tuple<string, string, bool>("/lootmaster", "Opens LootMaster Window", false ),
            new Tuple<string, string, bool>("/lm" , "Opens LootMaster Window (short version)", true )
        };

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

            InitCommands();
            //this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += OptionsUi.Draw;
        }

        private void InitCommands()
        {
            foreach(var command in Commands)
            {
                this.CommandManager.AddHandler(command.Item1, new CommandInfo(OnCommand)
                {
                    HelpMessage = command.Item2,
                    ShowInHelp = command.Item3
                });
            }
        }

        public void Dispose()
        {
            this.OptionsUi.Dispose();
            this.LM.Dispose();
            foreach(var command in Commands)
            {
                this.CommandManager.RemoveHandler(command.Item1);
            }
        }
        private void OnCommand(string command, string args)
        {
            switch(command)
            {
                case "/hrt":
                    this.OptionsUi.Show();
                    break;
                case "/lm":
                case "/lootmaster":
                    this.LM.OnCommand(args);
                    break;
                default:
                    PluginLog.LogError("Command \"" + command + "\" not found");
                    break;
            }            
        }
    }
}
