using System;
using System.Collections.Generic;
using System.IO;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    internal class CharacterDB
    {
        private readonly Dictionary<uint, Dictionary<string, Character>> CharDB;
        private readonly FileInfo CharDBJsonFile;

        internal CharacterDB(DirectoryInfo loadDir, bool reset)
        {
            var conv = new GearSetReferenceConverter();
            DataManager.JsonSerializerSettings.Converters.Add(conv);

            CharDBJsonFile = new FileInfo(loadDir.FullName + "\\CharacterDB.json");
            if (!CharDBJsonFile.Exists)
                CharDBJsonFile.Create().Close();
            CharDB = JsonConvert.DeserializeObject<Dictionary<uint, Dictionary<string, Character>>>(
                reset ? "" : CharDBJsonFile.OpenText().ReadToEnd(),
                DataManager.JsonSerializerSettings) ?? new();
            DataManager.JsonSerializerSettings.Converters.Remove(conv);
        }
        internal void AddOrGetCharacter(ref Character c)
        {
            if (!CharDB.ContainsKey(c.HomeWorldID))
                CharDB.Add(c.HomeWorldID, new Dictionary<string, Character>());
            if (!CharDB[c.HomeWorldID].ContainsKey(c.Name))
                CharDB[c.HomeWorldID].Add(c.Name, c);
            if (CharDB[c.HomeWorldID].TryGetValue(c.Name, out Character? c2))
                c = c2;
        }
        internal void UpdateIndex(uint oldWorld, string oldName, ref Character c)
        {
            if (CharDB.ContainsKey(oldWorld))
                CharDB[oldWorld].Remove(oldName);
            AddOrGetCharacter(ref c);
        }
        internal void Save()
        {
            var conv = new GearSetReferenceConverter();

            DataManager.JsonSerializerSettings.Converters.Add(conv);
            File.WriteAllText(CharDBJsonFile.FullName,
                JsonConvert.SerializeObject(CharDB, DataManager.JsonSerializerSettings));
            DataManager.JsonSerializerSettings.Converters.Remove(conv);
        }

    }
    public struct CharacterDBIndex
    {
        [JsonConstructor]
        public CharacterDBIndex(uint worldID, string name)
        {
            WorldID = worldID;
            Name = name;
        }
        [JsonProperty]
        public uint WorldID;
        [JsonProperty]
        public string Name;
    }
}
