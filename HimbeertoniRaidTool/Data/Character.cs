using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    public class Character
    {
        public List<PlayableClass> Classes;
        public string name = "";
        public Character(string? name)
        {
            Classes = new List<PlayableClass>();
            if(name != null)
                this.name = name;
        }
        

        public bool AddClass(AvailableClasses ClassToAdd){
            //Todo: Look if Calss is already present
            if (Classes.Find(x => x.ClassName == ClassToAdd) != null)
            {
                return false;
            } else
            {
                Classes.Add(new PlayableClass(ClassToAdd));
                return true;
            }
        }

        internal PlayableClass? getClass(AvailableClasses type)
        {
            return Classes.Find( x => x.ClassName == type);
        }
    }
}
