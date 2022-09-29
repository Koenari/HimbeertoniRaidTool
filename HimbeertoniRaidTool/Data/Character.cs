using System;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Character : IEquatable<Character>
    {
        [JsonProperty("Classes")]
        public List<PlayableClass> Classes = new();
        [JsonProperty("Name")]
        public string Name = "";
        [JsonProperty("MainJob")]
        public Job? MainJob;
        public PlayableClass? MainClass => MainJob.HasValue ? GetClass(MainJob.Value) : null;
        [JsonProperty("WorldID")]
        public uint HomeWorldID;
        [JsonProperty("Tribe")]
        public uint TribeID = 0;
        [JsonProperty]
        public Gender Gender = Gender.Unknown;
        [JsonIgnore]
        public Tribe Tribe => Services.DataManager.GetExcelSheet<Tribe>()!.GetRow(TribeID)!;
        [JsonProperty("LodestoneID")]
        public int LodestoneID = 0;
        public World? HomeWorld
        {
            get => HomeWorldID > 0 ? Services.DataManager.GetExcelSheet<World>()?.GetRow(HomeWorldID) : null;
            set => HomeWorldID = value?.RowId ?? 0;
        }
        public bool Filled => Name != "";
        public Character(string name = "", uint worldID = 0)
        {
            Name = name;
            HomeWorldID = worldID;
        }
        private PlayableClass AddClass(Job ClassToAdd)
        {
            PlayableClass toAdd = new(ClassToAdd, this);
            Classes.Add(toAdd);
            return toAdd;
        }
        internal void CleanUpClasses() => Classes.RemoveAll(x => x.IsEmpty);

        public PlayableClass GetClass(Job type)
        {
            return Classes.Find(x => x.Job == type) ?? AddClass(type);
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
