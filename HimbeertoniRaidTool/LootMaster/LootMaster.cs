using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.DataManagement;

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
                FillSoloChar(RaidGroups[0].Tank1, true);
            GearRefresherOnExamine.Enable();
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
            Character c = new Character(character.Name.TextValue, character.HomeWorld.Id);
            DataManager.GetManagedCharacter(ref c);
            p.MainChar = c;
            c.MainJob ??= Helper.GetJob(character);
            if (c.MainClass != null)
                c.MainClass.Level = character.Level;
        }
        internal static void AddGroup(RaidGroup group, bool getGroupInfos)
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
                PartyMember? p = Services.PartyList[i];
                if (p != null)
                    players.Add(p);
            }
            foreach (PartyMember p in players)
            {
                if (!Enum.TryParse(p.ClassJob.GameData!.Abbreviation.RawString, out Job c))
                    continue;
                Role r = c.GetRole();
                switch (r)
                {
                    case Role.Tank:
                        if (!group[PositionInRaidGroup.Tank1].Filled)
                            FillPosition(PositionInRaidGroup.Tank1, p, c);
                        else if (!group[PositionInRaidGroup.Tank2].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Tank2, p, c);
                        else
                            fill.Add(p);
                        break;
                    case Role.Healer:
                        if (!group[PositionInRaidGroup.Heal1].Filled)
                            FillPosition(PositionInRaidGroup.Heal1, p, c);
                        else if (!group[PositionInRaidGroup.Heal2].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Heal2, p, c);
                        else
                            fill.Add(p);
                        break;
                    case Role.Melee:
                        if (!group[PositionInRaidGroup.Melee1].Filled)
                            FillPosition(PositionInRaidGroup.Melee1, p, c);
                        else if (!group[PositionInRaidGroup.Melee2].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Melee2, p, c);
                        else
                            fill.Add(p);
                        break;
                    case Role.Caster:
                        if (!group[PositionInRaidGroup.Caster].Filled && group.Type == GroupType.Raid)
                            FillPosition(PositionInRaidGroup.Caster, p, c);
                        else
                            fill.Add(p);
                        break;
                    case Role.Ranged:
                        if (!group[PositionInRaidGroup.Ranged].Filled)
                            FillPosition(PositionInRaidGroup.Ranged, p, c);
                        else
                            fill.Add(p);
                        break;
                }
            }
            foreach (PartyMember pm in fill)
            {
                int pos = 0;
                while (group[(PositionInRaidGroup)pos].Filled) { pos++; }
                if (pos > 7) break;
                FillPosition((PositionInRaidGroup)pos, pm, Enum.Parse<Job>(pm.ClassJob.GameData!.Abbreviation.RawString));
            }
            void FillPosition(PositionInRaidGroup pos, PartyMember pm, Job c)
            {

                Player p = group[pos];
                p.Pos = pos;
                p.NickName = pm.Name.TextValue.Split(' ')[0];
                Character character = new Character(pm.Name.TextValue, pm.World.GameData!.RowId);
                bool characterExisted = DataManager.CharacterExists(character.HomeWorldID, character.Name);
                DataManager.GetManagedCharacter(ref character);
                p.MainChar = character;
                if (!characterExisted)
                {
                    p.MainChar.Classes.Clear();
                    p.MainChar.MainJob = c;
                    PlayerCharacter? pc = Helper.TryGetChar(p.MainChar.Name, p.MainChar.HomeWorld);
                    if (pc != null)
                    {
                        p.MainChar.MainClass!.Level = pc.Level;
                        GearSet BIS = new()
                        {
                            ManagedBy = GearSetManager.Etro,
                            EtroID = HRTPlugin.Configuration.GetDefaultBiS(c)
                        };
                        DataManager.GetManagedGearSet(ref BIS);
                        p.MainChar.MainClass.BIS = BIS;
                    }
                }

            }
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
