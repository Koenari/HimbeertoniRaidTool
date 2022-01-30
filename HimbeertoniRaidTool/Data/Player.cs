using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    class Player
    {
        private List<Character> Chars;
        public Player()
        {
            Chars = new List<Character>();
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
    }
}
