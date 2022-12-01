using System.Collections.Generic;
using HimbeertoniRaidTool.Common.Data;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDB
{
    private readonly Dictionary<uint, Dictionary<string, Character>> CharDB;


    internal CharacterDB(string serializedData, GearSetReferenceConverter conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        CharDB = JsonConvert.DeserializeObject<Dictionary<uint, Dictionary<string, Character>>>(
            serializedData, settings) ?? new();
        settings.Converters.Remove(conv);
        //Potentially parallelize later
        foreach (var SingleWorldDB in CharDB.Values)
            foreach (var c in SingleWorldDB.Values)
                foreach (var job in c)
                    job.SetParent(c);
    }
    internal List<uint> GetUsedWorlds()
    {
        return new List<uint>(CharDB.Keys);
    }
    internal List<string> GetCharactersList(uint worldID)
    {
        List<string> result = new();
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
        if (CharDB[c.HomeWorldID].TryGetValue(c.Name, out var c2))
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
