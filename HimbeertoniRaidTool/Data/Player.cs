using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class Player : IComparable<Player>
    {
        public string NickName = "";

        public Position Pos;
        public List<Character> Chars { get; } = new();
        public Player() { }
        public Player(Position pos)
        {
            this.Pos = pos;
        }
        public Player(string name, Character Character)
        {
            this.NickName = name;
            this.Chars.Add(Character);
        }
        public Character GetMainChar()
        {
            return Chars.First();
        }
        public void SetMainChar(Character main)
        {
            this.Chars.Remove(main);
            this.Chars.Insert(0, main);
        }
        public Character GetChar(int id)
        {
            return Chars[id];
        }

        public int CompareTo(Player? other)
        {
            if (other == null)
                return Int32.MaxValue;
            return this.Pos - other.Pos;
        }

        public enum Position
        {
            Tank1 = 0,
            Tank2 = 1,
            Heal1 = 2,
            Heal2 = 3,
            Melee1 = 4,
            Melee2 = 5,
            Ranged = 6,
            Caster = 7
        }
    }
}
