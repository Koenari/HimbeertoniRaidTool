using System.Diagnostics.CodeAnalysis;
using Dalamud.Logging;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDB
{
    private readonly HrtDataManager DataManager;
    private readonly Dictionary<HrtID, Character> Data = new();
    private readonly HashSet<uint> UsedWorlds = new();
    private readonly Dictionary<(uint, string), HrtID> NameLookup = new();
    private readonly Dictionary<ulong, HrtID> CharIDLookup = new();
    private ulong NextSequence = 1;

    internal CharacterDB(HrtDataManager dataManager, string serializedData, GearsetReferenceConverter conv, JsonSerializerSettings settings)
    {
        DataManager = dataManager;
        settings.Converters.Add(conv);
        var data = JsonConvert.DeserializeObject<List<Character>>(serializedData, settings);
        settings.Converters.Remove(conv);
        if (data is null)
            PluginLog.Error("Could not load CharacterDB");
        else
        {
            foreach (Character c in data)
            {
                if (c.LocalID.IsEmpty)
                {
                    PluginLog.Error($"Character {c.Name} was missing an ID and was removed from the database");
                    continue;
                }
                if (Data.TryAdd(c.LocalID, c))
                {
                    UsedWorlds.Add(c.HomeWorldID);
                    NameLookup.TryAdd((c.HomeWorldID, c.Name), c.LocalID);
                    if (c.CharID > 0)
                        CharIDLookup.TryAdd(c.CharID, c.LocalID);
                    NextSequence = Math.Max(NextSequence, c.LocalID.Sequence);
                    foreach (var job in c)
                        job.SetParent(c);
                }
            }
        }
        PluginLog.Debug($"DB conatins {Data.Count} characters");
        NextSequence++;
    }
    [Obsolete]
    internal CharacterDB(HrtDataManager dataManager, LegacyCharacterDB oldDB, LocalIDProvider idProvider)
    {
        //Migration constructor
        PluginLog.Debug("Called character DB migration constructor");
        DataManager = dataManager;
        Data = new();
        int count = 0;
        foreach (var db in oldDB.CharDB.Values)
        {
            foreach (Character c in db.Values)
            {
                count++;
                if (c.LocalID.IsEmpty)
                    c.LocalID = idProvider.CreateCharID(NextSequence++);
                Data.Add(c.LocalID, c);
            }
        }
        PluginLog.Debug($"Migrated {count} characters");
    }
    internal ulong GetNextSequence() => NextSequence++;
    internal IEnumerable<uint> GetUsedWorlds() => UsedWorlds;
    internal IReadOnlyList<string> GetKnownChracters(uint worldID)
    {
        List<string> result = new();
        foreach (var character in Data.Values.Where(c => c.HomeWorldID == worldID))
            result.Add(character.Name);
        return result;
    }
    internal bool TryAddCharacter(in Character c)
    {
        if (c.LocalID.IsEmpty)
            c.LocalID = DataManager.IDProvider.CreateID(HrtID.IDType.Character);
        if (Data.TryAdd(c.LocalID, c))
        {
            UsedWorlds.Add(c.HomeWorldID);
            NameLookup.TryAdd((c.HomeWorldID, c.Name), c.LocalID);
            if (c.CharID > 0)
                CharIDLookup.TryAdd(c.CharID, c.LocalID);
            return true;
        }
        return false;
    }
    internal bool TryGetCharacterByCharID(ulong charID, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (CharIDLookup.TryGetValue(charID, out HrtID? id))
            return TryGetCharacter(id, out c);
        id = Data.FirstOrDefault(x => x.Value.CharID == charID).Key;
        if (id is not null)
        {
            CharIDLookup.Add(charID, id);
            c = Data[id];
            return true;
        }
        return false;
    }
    internal bool SearchCharacter(uint worldID, string name, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (NameLookup.TryGetValue((worldID, name), out HrtID? id))
            return TryGetCharacter(id, out c);
        id = Data.FirstOrDefault(x => x.Value.HomeWorldID == worldID && x.Value.Name.Equals(name)).Key;
        if (id is not null)
        {
            NameLookup.Add((worldID, name), id);
            c = Data[id];
            return true;
        }
        return false;
    }
    internal bool TryGetCharacter(HrtID id, [NotNullWhen(true)] out Character? c)
        => Data.TryGetValue(id, out c);
    internal bool Contains(HrtID hrtID) => Data.ContainsKey(hrtID);
    internal string Serialize(GearsetReferenceConverter conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters.Remove(conv);
        return result;
    }
}
