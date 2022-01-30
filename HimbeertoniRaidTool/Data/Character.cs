using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    class Character
    {
        public Character()
        {
            Classes = new List<PlayableClass>();
        }
        public List<PlayableClass> Classes;

        public bool AddClass(AvailabbleClasses ClassToAdd){
            //Todo: Look if Calss already present
            if (false)
            {
                return false;
            } else
            {
                Classes.Add(new PlayableClass(ClassToAdd));
                return true;
            }
        }
    }
}
