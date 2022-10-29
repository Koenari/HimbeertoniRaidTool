using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RaidGroup : IEnumerable<Player>
    {
        [JsonProperty("TimeStamp")]
        public DateTime TimeStamp;
        [JsonProperty("Name")]
        public string Name;
        private readonly Player[] _Players;
        [JsonProperty("Type")]
        public GroupType Type;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RolePriority? RolePriority = null;
        [JsonProperty]
        private Player Tank1 { get => _Players[0]; set => _Players[0] = value; }
        [JsonProperty]
        private Player Tank2 { get => _Players[1]; set => _Players[1] = value; }
        [JsonProperty]
        private Player Heal1 { get => _Players[2]; set => _Players[2] = value; }
        [JsonProperty]
        private Player Heal2 { get => _Players[3]; set => _Players[3] = value; }
        [JsonProperty]
        private Player Melee1 { get => _Players[4]; set => _Players[4] = value; }
        [JsonProperty]
        private Player Melee2 { get => _Players[5]; set => _Players[5] = value; }
        [JsonProperty]
        private Player Ranged { get => _Players[6]; set => _Players[6] = value; }
        [JsonProperty]
        private Player Caster { get => _Players[7]; set => _Players[7] = value; }
        private IEnumerable<Player> Players
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    int idx = i;
                    if (Type == GroupType.Group)
                        idx *= 2;
                    yield return _Players[idx];
                }
            }

        }
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
                _Players[i] = new();
            }
        }
        public Player this[int idx]
        {
            get
            {
                if (idx >= Count)
                    throw new IndexOutOfRangeException($"Raidgroup of type {Type} has no member at index {idx}");
                if (Type == GroupType.Group)
                    idx *= 2;
                return _Players[idx];
            }
            set
            {
                if (idx >= Count)
                    throw new IndexOutOfRangeException($"Raidgroup of type {Type} has no member at index {idx}");
                if (Type == GroupType.Group)
                    idx *= 2;
                _Players[idx] = value;
            }
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

        public IEnumerator<Player> GetEnumerator() => Players.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Players.GetEnumerator();
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
