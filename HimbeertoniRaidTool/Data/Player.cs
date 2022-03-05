using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HimbeertoniRaidTool.Data
{
    public class Player : IComparable<Player>
    {
        public string NickName = "";
        [JsonIgnore]
        public bool Filled => NickName != "";

        public PositionInRaidGroup Pos;
        public List<Character> Chars { get; set; } = new();
        [JsonIgnore]
        public Character MainChar
        {
            get
            {
                if (Chars.Count == 0)
                    Chars.Insert(0, new());
                return Chars[0];
            }
            set
            {
                Chars.Remove(value);
                Chars.Insert(0, value);
            }
        }
        [JsonIgnore]
        public GearSet Gear => MainChar.MainClass.Gear;
        [JsonIgnore]
        public GearSet BIS => MainChar.MainClass.BIS;
        public Player() { }
        public Player(PositionInRaidGroup pos) => Pos = pos;
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


    }
}
