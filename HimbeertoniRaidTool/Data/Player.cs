using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class Player : IComparable<Player>
    {
        public string NickName = "";
        [JsonIgnore]
        public bool Filled => NickName != "";

        public Position Pos;
        public List<Character> Chars { get; set; } = new();
        [JsonIgnore]
        public Character MainChar {
            get 
            {
                if (Chars.Count == 0)
                    Chars.Insert(0,new());
                return Chars[0]; 
            }
            set
            {
                this.Chars.Remove(value);
                this.Chars.Insert(0, value);
            } }
        [JsonIgnore]
        public GearSet Gear => MainChar.MainClass.Gear;
        [JsonIgnore]
        public GearSet BIS => MainChar.MainClass.BIS;
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
        internal void Reset()
        {
            NickName = "";
            Chars.Clear();
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
