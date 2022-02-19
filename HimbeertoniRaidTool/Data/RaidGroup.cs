using System;
using System.Collections.Generic;
using static HimbeertoniRaidTool.Data.Player;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class RaidGroup 
    {
        public DateTime TimeStamp;
        public string Name;
        private readonly List<Player> _Players = new();

        public Player Tank1 { get => _Players[0]; set => _Players[0] = value; }
        public Player Tank2 { get => _Players[1]; set => _Players[1] = value; }
        public Player Heal1 { get => _Players[2]; set => _Players[2] = value; }
        public Player Heal2 { get => _Players[3]; set => _Players[3] = value; }
        public Player Melee1 { get => _Players[4]; set => _Players[4] = value; }
        public Player Melee2 { get => _Players[5]; set => _Players[5] = value; }
        public Player Ranged { get => _Players[6]; set => _Players[6] = value; }
        public Player Caster { get => _Players[7]; set => _Players[7] = value; }

        public List<Player> Players => _Players;
        public RaidGroup(string name)
        {
            TimeStamp = DateTime.Now;
            Name = name;
            for (int i = 0; i < 8; i++)
            {
                _Players.Add(new((Position) i));
            }
        }
        public Player GetPlayer(Position pos) => _Players[(int) pos];

        internal void SetPlayer(Position pos, Player p)
        {
            _Players[(int)pos] = p;
        }

        internal Character? GetCharacter(string name)
        {
            foreach(Player p in _Players)
            {
                foreach(Character c in p.Chars)
                {
                    if (name.Equals(c.Name))
                        return c;
                }
            }
            return null;
        }
    }
}
