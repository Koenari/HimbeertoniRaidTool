using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class Character
    {
        public List<PlayableClass> Classes;
        public string name = "";
        public AvailableClasses MainClass = AvailableClasses.AST;
        public Character(string name = "")
        {
            Classes = new List<PlayableClass>();
            if(name != null)
                this.name = name;
        }
        

        public bool AddClass(AvailableClasses ClassToAdd){
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

        internal PlayableClass? getMainClass()
        {
            return this.getClass(MainClass);
        }
    }
}
