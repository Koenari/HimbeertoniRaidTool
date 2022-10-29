using System;
using System.Collections;
using System.Collections.Generic;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Character : IEquatable<Character>, IEnumerable<PlayableClass>
    {
        private static readonly ExcelSheet<World>? _worldSheet = Services.DataManager.GetExcelSheet<World>();
        private static readonly ExcelSheet<Tribe> _tribeSheet = Services.DataManager.GetExcelSheet<Tribe>()!;
        [JsonProperty("Classes")]
        private readonly List<PlayableClass> _classes = new();
        [JsonProperty("Name")]
        public string Name = "";
        [JsonProperty("MainJob")]
        public Job? MainJob;
        public PlayableClass? MainClass => MainJob.HasValue ? this[MainJob.Value] : null;
        [JsonProperty("WorldID")]
        public uint HomeWorldID;
        [JsonProperty("Tribe")]
        public uint TribeID = 0;
        [JsonProperty]
        public Gender Gender = Gender.Unknown;
        [JsonIgnore]
        public Tribe Tribe => _tribeSheet.GetRow(TribeID)!;
        [JsonProperty("LodestoneID")]
        public int LodestoneID = 0;
        public World? HomeWorld
        {
            get => HomeWorldID > 0 ? _worldSheet?.GetRow(HomeWorldID) : null;
            set => HomeWorldID = value?.RowId ?? 0;
        }
        public bool Filled => Name != "";
        public Character(string name = "", uint worldID = 0)
        {
            Name = name;
            HomeWorldID = worldID;
        }
        public IEnumerable<PlayableClass> Classes => _classes;
        private PlayableClass AddClass(Job ClassToAdd)
        {
            PlayableClass toAdd = new(ClassToAdd, this);
            _classes.Add(toAdd);
            return toAdd;
        }
        public PlayableClass this[Job type]
        {
            get => _classes.Find(x => x.Job == type) ?? AddClass(type);
        }
        [Obsolete("Use [type]")]
        public PlayableClass GetClass(Job type)
        {
            return this[type];
        }
        public bool RemoveClass(Job type)
        {
            return _classes.RemoveAll(job => job.Job == type) > 0;
        }
        public bool Equals(Character? other)
        {
            if (other == null)
                return false;
            return Name.Equals(other.Name) && HomeWorldID == other.HomeWorldID;
        }
        public override bool Equals(object? obj) => obj is Character objS && Equals(objS);
        public override int GetHashCode() => Name.GetHashCode();

        public IEnumerator<PlayableClass> GetEnumerator() => Classes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Classes.GetEnumerator();

    }
}
