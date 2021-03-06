using System;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RaidGroup
    {
        [JsonProperty("TimeStamp")]
        public DateTime TimeStamp;
        [JsonProperty("Name")]
        public string Name;
        private readonly Player[] _Players;
        [JsonProperty("Type")]
        public GroupType Type;
        [JsonProperty]
        public Player Tank1 { get => _Players[0]; set => _Players[0] = value; }
        [JsonProperty]
        public Player Tank2 { get => _Players[1]; set => _Players[1] = value; }
        [JsonProperty]
        public Player Heal1 { get => _Players[2]; set => _Players[2] = value; }
        [JsonProperty]
        public Player Heal2 { get => _Players[3]; set => _Players[3] = value; }
        [JsonProperty]
        public Player Melee1 { get => _Players[4]; set => _Players[4] = value; }
        [JsonProperty]
        public Player Melee2 { get => _Players[5]; set => _Players[5] = value; }
        [JsonProperty]
        public Player Ranged { get => _Players[6]; set => _Players[6] = value; }
        [JsonProperty]
        public Player Caster { get => _Players[7]; set => _Players[7] = value; }
        public Player[] Players => Type switch
        {
            GroupType.Solo => new[] { _Players[0] },
            GroupType.Group => new[] { _Players[0], _Players[2], _Players[4], _Players[6], },
            GroupType.Raid => _Players,
            _ => throw new NotImplementedException(),
        };
        public int Count => Type switch
        {
            GroupType.Solo => 1,
            GroupType.Group => 4,
            GroupType.Raid => 8,
            _ => throw new NotImplementedException(),
        };
        [JsonConstructor]
        public RaidGroup(string name = "", GroupType type = GroupType.Raid)
        {
            Type = type;
            TimeStamp = DateTime.Now;
            Name = name;
            _Players = new Player[8];
            for (int i = 0; i < 8; i++)
            {
                _Players[i] = new((PositionInRaidGroup)i);
            }
        }
        public Player this[PositionInRaidGroup pos]
        {
            get => _Players[(int)pos];
            set => _Players[(int)pos] = value;
        }

        internal Character? GetCharacter(string name)
        {
            foreach (Player p in _Players)
            {
                foreach (Character c in p.Chars)
                {
                    if (name.Equals(c.Name))
                        return c;
                }
            }
            return null;
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class Alliance
    {
        [JsonProperty("Name")]
        public string Name { get; set; }
        public DateTime TimeStamp { get; set; }
        [JsonProperty("Groups")]
        public RaidGroup[] RaidGroups { get; set; }
        [JsonConstructor]
        public Alliance(string name = "")
        {
            Name = name;
            TimeStamp = DateTime.Now;
            RaidGroups = new RaidGroup[3];
        }
    }
}
