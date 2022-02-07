using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class RaidGroup
    {
        public Player Tank1 = new();
        public Player Tank2 = new();
        public Player Heal1 = new();
        public Player Heal2 = new();
        public Player Melee1 = new();
        public Player Melee2 = new();
        public Player Ranged = new();
        public Player Caster = new();
    }
}
