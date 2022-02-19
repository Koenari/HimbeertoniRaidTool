using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [Serializable]
    public class Character : IEquatable<Character>
    {
        public List<PlayableClass> Classes = new();
        public string Name = "";
        public AvailableClasses MainClassType = AvailableClasses.AST;
        [JsonIgnore]
        public PlayableClass MainClass => GetClass(MainClassType);
        [JsonIgnore]
        public bool Filled => Name != "";
        public Character(string name = "")
        {
                this.Name = name;
        }

        private PlayableClass AddClass(AvailableClasses ClassToAdd) 
        {
            PlayableClass toAdd = new(ClassToAdd);
                Classes.Add(toAdd);
            return toAdd;
        }

        public PlayableClass GetClass(AvailableClasses type)
        {
            return Classes.Find( x => x.ClassType == type) ?? AddClass(type);
        }

        public bool Equals(Character? other)
        {
            if (other == null)
                return false;
            return Name.Equals(other.Name);
        }
        public override bool Equals(object? obj)=> obj is Character objS && Equals(objS);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
