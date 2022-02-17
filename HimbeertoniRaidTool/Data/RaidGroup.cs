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
        private List<Player> Players;

        public Player Tank1 { get => Players[0]; set => Players[0] = value; }
        public Player Tank2 { get => Players[1]; set => Players[1] = value; }
        public Player Heal1 { get => Players[2]; set => Players[2] = value; }
        public Player Heal2 { get => Players[3]; set => Players[3] = value; }
        public Player Melee1 { get => Players[4]; set => Players[4] = value; }
        public Player Melee2 { get => Players[5]; set => Players[5] = value; }
        public Player Ranged { get => Players[6]; set => Players[6] = value; }
        public Player Caster { get => Players[7]; set => Players[7] = value; }
        public RaidGroup(string name)
        {
            this.TimeStamp = DateTime.Now;
            this.Name = name;
            this.Players = new();
            for (int i = 0; i < 8; i++)
            {
                Players.Add(new((Position) i));
            }
        }
        public Player GetPlayer(Position pos) => Players[(int) pos];
        public List<Player> GetPlayers() => Players;

        internal void SetPlayer(Position pos, Player p)
        {
            this.Players[(int)pos] = p;
        }

        internal Character? GetCharacter(string name)
        {
            foreach(Player p in Players)
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
