using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    internal sealed class LootMasterModule : IHrtModule<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
    {
        //Interface Properties
        public string Name => "Loot Master";
        public string InternalName => "LootMaster";
        public HRTConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi> Configuration => _config;
        public string Description => "";
        public IEnumerable<HrtCommand> Commands => new List<HrtCommand>()
        {
            new()
            {
                Command = "/lootmaster",
                Description = Localize("/lootmaster", "Opens LootMaster Window (or /lm as short variant)"),
                ShowInHelp = true,
                OnCommand = OnCommand,
            },
            new()
            {
                Command = "/lm",
                Description = Localize("/lootmaster", "Opens LootMaster Window (or /lm as short variant)"),
                ShowInHelp = false,
                OnCommand = OnCommand,
            }
        };
        //Properties
        private static List<RaidGroup> RaidGroups => Services.HrtDataManager.Groups;
        private readonly LootmasterUI Ui;
        private readonly LootMasterConfiguration _config;
        private bool _fillSoloOnLogin = false;
        public LootMasterModule()
        {
            if (RaidGroups.Count == 0 || RaidGroups[0].Type != GroupType.Solo || !RaidGroups[0].Name.Equals("Solo"))
            {
                RaidGroups.Insert(0, new("Solo", GroupType.Solo));
                _fillSoloOnLogin = true;
            }

            _config = new(this);
            Ui = new(this);
            Services.ClientState.Login += OnLogin;
        }
        public void AfterFullyLoaded()
        {
            GearRefresherOnExamine.Enable();
            if (Services.ClientState.IsLoggedIn)
                OnLogin(null, new());
            if (Configuration.Data.UpdateEtroBisOnStartup)
                Services.HrtDataManager.UpdateEtroSets(Configuration.Data.EtroUpdateIntervalDays);
        }
        public void OnLogin(object? sender, EventArgs e)
        {
            if (_fillSoloOnLogin)
                FillSoloChar(RaidGroups[0][0], true);
            _fillSoloOnLogin = false;
            if (_config.Data.OpenOnStartup)
                Ui.Show();
        }

        public void Update(Framework fw)
        {

        }
        private void FillSoloChar(Player p, bool useSelf = false)
        {
            PlayerCharacter? character = null;
            if (useSelf)
                character = Services.TargetManager.Target as PlayerCharacter;
            if (character == null)
                Services.CharacterInfoService.TryGetChar(out character, p.MainChar.Name, p.MainChar.HomeWorld);
            if (character == null)
                return;
            Character c = new(character.Name.TextValue, character.HomeWorld.Id);
            Services.HrtDataManager.GetManagedCharacter(ref c);
            p.NickName = c.Name.Split(' ')[0];
            p.MainChar = c;
            c.MainJob ??= character.GetJob();
            if (c.MainClass != null)
            {
                c.MainClass.Level = character.Level;
                GearSet bis = new(GearSetManager.Etro, c, c.MainClass.Job)
                {
                    EtroID = _config.Data.GetDefaultBiS(c.MainClass.Job)
                };
                Services.HrtDataManager.GetManagedGearSet(ref bis);
                c.MainClass.BIS = bis;
            }
            Services.HrtDataManager.Save();
        }
        internal void AddGroup(RaidGroup group, bool getGroupInfos)
        {
            RaidGroups.Add(group);
            if (!getGroupInfos)
                return;
            //Determine group type
            if (Services.PartyList.Length < 2)
                group.Type = GroupType.Solo;
            else if (Services.PartyList.Length < 5)
                group.Type = GroupType.Group;
            else
                group.Type = GroupType.Raid;
            //Get Infos
            if (group.Type == GroupType.Solo)
            {
                PlayerCharacter? target = Services.TargetManager.Target as PlayerCharacter;
                if (target is not null)
                {
                    group[0].NickName = target.Name.TextValue;
                    group[0].MainChar.Name = target.Name.TextValue;
                    group[0].MainChar.HomeWorld = target.HomeWorld.GameData;
                    FillSoloChar(group[0]);
                }
                else
                    FillSoloChar(group[0], true);
                return;
            }
            List<PartyMember> players = new();
            List<PartyMember> fill = new();
            for (int i = 0; i < Services.PartyList.Length; i++)
            {
                var p = Services.PartyList[i];
                if (p != null)
                    players.Add(p);
            }
            foreach (var p in players)
            {
                if (!Enum.TryParse(p.ClassJob.GameData?.Abbreviation.RawString, out Job c))
                {
                    fill.Add(p);
                    continue;
                }
                var r = c.GetRole();
                switch (r)
                {
                    case Role.Tank:
                        if (!group[0].Filled)
                            FillPosition(0, p);
                        else if (!group[1].Filled && group.Type == GroupType.Raid)
                            FillPosition(1, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Healer:
                        if (!group[2].Filled)
                            FillPosition(2, p);
                        else if (!group[3].Filled && group.Type == GroupType.Raid)
                            FillPosition(3, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Melee:
                        if (!group[4].Filled)
                            FillPosition(4, p);
                        else if (!group[5].Filled && group.Type == GroupType.Raid)
                            FillPosition(5, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Caster:
                        if (!group[6].Filled && group.Type == GroupType.Raid)
                            FillPosition(6, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Ranged:
                        if (!group[7].Filled)
                            FillPosition(7, p);
                        else
                            fill.Add(p);
                        break;
                    default:
                        fill.Add(p);
                        break;
                }
            }
            foreach (var pm in fill)
            {
                int pos = 0;
                while (group[pos].Filled) { pos++; }
                if (pos > 7) break;
                FillPosition(pos, pm);
            }
            void FillPosition(int pos, PartyMember pm)
            {
                var p = group[pos];
                p.NickName = pm.Name.TextValue.Split(' ')[0];
                Character character = new(pm.Name.TextValue, pm.World.GameData?.RowId ?? 0);
                bool characterExisted = Services.HrtDataManager.CharacterExists(character.HomeWorldID, character.Name);
                Services.HrtDataManager.GetManagedCharacter(ref character);
                p.MainChar = character;
                if (!characterExisted)
                {
                    bool canParseJob = Enum.TryParse(pm.ClassJob.GameData?.Abbreviation.RawString, out Job c);
                    if (Services.CharacterInfoService.TryGetChar(out var pc, p.MainChar.Name, p.MainChar.HomeWorld) && canParseJob && c != Job.ADV)
                    {
                        p.MainChar.MainJob = c;
                        p.MainChar.MainClass!.Level = pc!.Level;
                        GearSet BIS = new()
                        {
                            ManagedBy = GearSetManager.Etro,
                            EtroID = _config.Data.GetDefaultBiS(c)
                        };
                        Services.HrtDataManager.GetManagedGearSet(ref BIS);
                        p.MainChar.MainClass.BIS = BIS;
                    }
                }

            }
            Services.HrtDataManager.Save();
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
            GearRefresherOnExamine.Dispose();
            Ui.Dispose();
        }

        public void HandleMessage(HrtUiMessage message)
        {
            if (message.MessageType is HrtUiMessageType.Failure or HrtUiMessageType.Error)
                PluginLog.Warning(message.Message);
            else
                PluginLog.Information(message.Message);
            Ui.HandleMessage(message);
        }
    }
}
