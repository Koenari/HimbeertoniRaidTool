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
        [JsonProperty("Members")]
        private readonly Player[] _Players;
        [JsonProperty("Type")]
        public GroupType Type;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RolePriority? RolePriority = null;
        //These are needed for deserialization of RaidGRoups.json from versions < 1.1.0
        [Obsolete("Use []",true)]
        [JsonProperty]
        private Player Tank1 { set => _Players[0] = value; }
        [Obsolete("Use []", true)]
        [JsonProperty]
        private Player Tank2 { set => _Players[1] = value; }
        [Obsolete("Use []", true)]
        [JsonProperty]
        private Player Heal1 { set => _Players[2] = value; }
        [Obsolete("Use []", true)]
        [JsonProperty]
        private Player Heal2 { set => _Players[3] = value; }
        [Obsolete("Use []", true)]
        [JsonProperty]
        private Player Melee1 { set => _Players[4] = value; }
        [Obsolete("Use []", true)]
        [JsonProperty]
        private Player Melee2 { set => _Players[5] = value; }
        [Obsolete("Use []", true)]
        [JsonProperty]
        private Player Ranged {set => _Players[6] = value; }
        [Obsolete("Use []", true)]
        [JsonProperty]
        private Player Caster { set => _Players[7] = value; }
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
            for (int i = 0; i < _Players.Length; i++)
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
