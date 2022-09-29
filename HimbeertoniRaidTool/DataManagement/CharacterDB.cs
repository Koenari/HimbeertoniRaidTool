using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    internal class CharacterDB
    {
        private readonly Dictionary<uint, Dictionary<string, Character>> CharDB;
        private readonly FileInfo CharDBJsonFile;

        internal CharacterDB(DirectoryInfo loadDir, GearDB gearDB)
        {
            var conv = new GearSetReferenceConverter(gearDB);
            HrtDataManager.JsonSerializerSettings.Converters.Add(conv);

            CharDBJsonFile = new FileInfo(loadDir.FullName + "\\CharacterDB.json");
            if (!CharDBJsonFile.Exists)
                CharDBJsonFile.Create().Close();
            CharDB = JsonConvert.DeserializeObject<Dictionary<uint, Dictionary<string, Character>>>(
                CharDBJsonFile.OpenText().ReadToEnd(),
                HrtDataManager.JsonSerializerSettings) ?? new();
            HrtDataManager.JsonSerializerSettings.Converters.Remove(conv);
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
        internal bool Save(GearDB gearDB)
        {
            bool hasError = false;
            var conv = new GearSetReferenceConverter(gearDB);

            HrtDataManager.JsonSerializerSettings.Converters.Add(conv);
            StreamWriter? writer = null;
            try
            {
                writer = CharDBJsonFile.CreateText();
                var serializer = JsonSerializer.Create(HrtDataManager.JsonSerializerSettings);
                serializer.Serialize(writer, CharDB);
            }
            catch (Exception e)
            {

                PluginLog.Error("Could not write character data\n{0}", e);
                hasError = true;
            }
            finally
            {
                writer?.Dispose();
            }
            HrtDataManager.JsonSerializerSettings.Converters.Remove(conv);
            return !hasError;
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
