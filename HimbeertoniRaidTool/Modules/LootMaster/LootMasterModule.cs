using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.DataManagement;
using HimbeertoniRaidTool.UI;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    internal sealed class LootMasterModule : IHrtModule<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
    {
        //Singleton
        private static readonly Lazy<LootMasterModule> _Instance = new(() => new LootMasterModule());
        internal static LootMasterModule Instance { get { return _Instance.Value; } }
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
                Description = Localize("/lootmaster", "Opens LootMaster Window"),
                ShowInHelp = false,
                OnCommand = OnCommand,
            },
            new()
            {
                Command = "/lm",
                Description = Localize("/lootmaster", "Opens LootMaster Window"),
                ShowInHelp = true,
                OnCommand = OnCommand,
            }
        };
        //Properties
        private static List<RaidGroup> RaidGroups => Services.HrtDataManager.Groups;
        private readonly LootmasterUI Ui;
        private readonly LootMasterConfiguration _config;
        private LootMasterModule()
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
                FillSoloChar(RaidGroups[0].Tank1, true);
            GearRefresherOnExamine.Enable();
            _config = new(this);
            Ui = new(this);
        }
        public void AfterFullyLoaded()
        {
            if (_config.Data.OpenOnStartup)
                Ui.Show();
        }

        public void Update(Framework fw)
        {

        }
        private static void FillSoloChar(Player p, bool useSelf = false)
        {
            PlayerCharacter? character = null;
            if (useSelf)
                character = Helper.Self;
            if (character == null)
                character = Helper.TryGetChar(p.MainChar.Name, p.MainChar.HomeWorld);
            if (character == null)
                return;
            var c = new Character(character.Name.TextValue, character.HomeWorld.Id);
            Services.HrtDataManager.GetManagedCharacter(ref c);
            p.MainChar = c;
            c.MainJob ??= character.GetJob();
            if (c.MainClass != null)
                c.MainClass.Level = character.Level;
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
                var target = Helper.TargetChar;
                if (target is not null)
                {
                    group.Tank1.NickName = target.Name.TextValue;
                    group.Tank1.MainChar.Name = target.Name.TextValue;
                    group.Tank1.MainChar.HomeWorld = target.HomeWorld.GameData;
                    FillSoloChar(group.Tank1);
                }
                else
                    FillSoloChar(group.Tank1, true);
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
                        if (!group[PositionInRaidGroup.Tank1].Filled)
                            FillPosition(PositionInRaidGroup.Tank1, p);
                        else if (!group[PositionInRaidGroup.Tank2].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Tank2, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Healer:
                        if (!group[PositionInRaidGroup.Heal1].Filled)
                            FillPosition(PositionInRaidGroup.Heal1, p);
                        else if (!group[PositionInRaidGroup.Heal2].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Heal2, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Melee:
                        if (!group[PositionInRaidGroup.Melee1].Filled)
                            FillPosition(PositionInRaidGroup.Melee1, p);
                        else if (!group[PositionInRaidGroup.Melee2].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Melee2, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Caster:
                        if (!group[PositionInRaidGroup.Caster].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Caster, p);
                        else
                            fill.Add(p);
                        break;
                    case Role.Ranged:
                        if (!group[PositionInRaidGroup.Ranged].Filled)
                            FillPosition(PositionInRaidGroup.Ranged, p);
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
                Dalamud.Logging.PluginLog.Debug($"To fill: {pm.Name}");
                int pos = 0;
                while (group[(PositionInRaidGroup)pos].Filled) { pos++; }
                if (pos > 7) break;
                FillPosition((PositionInRaidGroup)pos, pm);
            }
            void FillPosition(PositionInRaidGroup pos, PartyMember pm)
            {
                Dalamud.Logging.PluginLog.Debug($"In fill: {pm.Name}");
                var p = group[pos];
                p.Pos = pos;
                p.NickName = pm.Name.TextValue.Split(' ')[0];
                var character = new Character(pm.Name.TextValue, pm.World.GameData?.RowId ?? 0);
                bool characterExisted = Services.HrtDataManager.CharacterExists(character.HomeWorldID, character.Name);
                Services.HrtDataManager.GetManagedCharacter(ref character);
                p.MainChar = character;
                if (!characterExisted)
                {
                    p.MainChar.Classes.Clear();
                    bool canParseJob = Enum.TryParse(pm.ClassJob.GameData?.Abbreviation.RawString, out Job c);

                    var pc = Helper.TryGetChar(p.MainChar.Name, p.MainChar.HomeWorld);
                    if (pc != null && canParseJob && c != Job.ADV)
                    {
                        p.MainChar.MainJob = c;
                        p.MainChar.MainClass!.Level = pc.Level;
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
            throw new NotImplementedException();
        }
    }
}
