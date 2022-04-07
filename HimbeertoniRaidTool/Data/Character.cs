using System;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;
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
        [JsonProperty("WorldID")]
        public uint HomeWorldID { get; private set; }
        [JsonIgnore]
        public World? HomeWorld
        {
            get => HomeWorldID > 0 ? Services.DataManager.GetExcelSheet<World>()?.GetRow(HomeWorldID) : null;
            set => HomeWorldID = value?.RowId ?? 0;
        }

        [JsonIgnore]
        public bool Filled => Name != "";
        public Character(string name = "", uint worldID = 0)
        {
            Name = name;
            HomeWorldID = worldID;
        }

        private PlayableClass AddClass(AvailableClasses ClassToAdd)
        {
            PlayableClass toAdd = new(ClassToAdd, this);
            Classes.Add(toAdd);
            return toAdd;
        }
        internal void CleanUpClasses() => Classes.RemoveAll(x => x.IsEmpty);

        public PlayableClass GetClass(AvailableClasses type)
        {
            return Classes.Find(x => x.ClassType == type) ?? AddClass(type);
        }

        public bool Equals(Character? other)
        {
            if (other == null)
                return false;
            return Name.Equals(other.Name) && HomeWorldID == other.HomeWorldID;
        }
        public override bool Equals(object? obj) => obj is Character objS && Equals(objS);
        public override int GetHashCode() => Name.GetHashCode();
    }
}
