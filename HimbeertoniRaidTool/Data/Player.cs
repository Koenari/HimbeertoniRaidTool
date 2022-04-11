using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Player : IComparable<Player>
    {
        [JsonProperty("NickName")]
        public string NickName = "";
        [JsonProperty("Pos")]
        public PositionInRaidGroup Pos;
        [JsonProperty("Chars")]
        public List<Character> Chars { get; set; } = new();
        public bool Filled => NickName != "";
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
        public GearSet Gear => MainChar.MainClass.Gear;
        public GearSet BIS => MainChar.MainClass.BIS;
        public Player() { }
        [JsonConstructor]
        public Player(PositionInRaidGroup pos) => Pos = pos;
        internal void Reset()
        {
            NickName = "";
            Chars.Clear();
        }
        public int CompareTo(Player? other)
        {
            if (other == null)
                return int.MaxValue;
            return Pos - other.Pos;
        }


    }
}
