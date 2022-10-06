using System.Collections.Generic;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    internal class CharacterDB
    {
        private readonly Dictionary<uint, Dictionary<string, Character>> CharDB;


        internal CharacterDB(string serializedData, GearSetReferenceConverter conv)
        {
            HrtDataManager.JsonSettings.Converters.Add(conv);
            CharDB = JsonConvert.DeserializeObject<Dictionary<uint, Dictionary<string, Character>>>(
                serializedData, HrtDataManager.JsonSettings) ?? new();
            HrtDataManager.JsonSettings.Converters.Remove(conv);
            foreach (var entry1 in CharDB)
                foreach (var entry2 in entry1.Value)
                    foreach (var entry3 in entry2.Value.Classes)
                        entry3.SetParent(entry2.Value);
        }
        internal List<uint> GetUsedWorlds()
        {
            return new List<uint>(CharDB.Keys);
        }
        internal List<string> GetCharactersList(uint worldID)
        {
            List<string> result = new List<string>();
            foreach (var character in CharDB[worldID].Values)
                result.Add(character.Name);
            return result;
        }
        internal bool Exists(uint worldID, string name) =>
            CharDB.ContainsKey(worldID) && CharDB[worldID].ContainsKey(name);

        internal bool AddOrGetCharacter(ref Character c)
        {
            if (!CharDB.ContainsKey(c.HomeWorldID))
                CharDB.Add(c.HomeWorldID, new Dictionary<string, Character>());
            if (!CharDB[c.HomeWorldID].ContainsKey(c.Name))
                CharDB[c.HomeWorldID].Add(c.Name, c);
            if (CharDB[c.HomeWorldID].TryGetValue(c.Name, out Character? c2))
                c = c2;
            return true;
        }
        internal bool UpdateIndex(uint oldWorld, string oldName, ref Character c)
        {
            if (CharDB.ContainsKey(oldWorld))
                CharDB[oldWorld].Remove(oldName);
            AddOrGetCharacter(ref c);
            return true;
        }
        internal string Serialize(GearSetReferenceConverter conv, JsonSerializerSettings settings)
        {
            settings.Converters.Add(conv);
            string result = JsonConvert.SerializeObject(CharDB, settings);
            settings.Converters.Remove(conv);
            return result;
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
