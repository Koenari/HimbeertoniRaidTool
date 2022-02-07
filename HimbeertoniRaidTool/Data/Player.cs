using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class Player
    {
        public string NickName = "";
        public List<Character> Chars { get; } = new();
        public Player()
        {

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
    }
}
