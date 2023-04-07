using System.Diagnostics.CodeAnalysis;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDB
{
    private readonly HrtDataManager DataManager;
    private readonly CharacterDBData Data;
    private readonly HashSet<uint> UsedWorlds = new();
    private readonly Dictionary<(uint, string), HrtID> NameLookup = new();
    private const int CurrentDataVersion = 1;
    public bool DataReady => Data.Version == CurrentDataVersion;

    internal CharacterDB(HrtDataManager dataManager, string serializedData, GearsetReferenceConverter conv, JsonSerializerSettings settings)
    {
        DataManager = dataManager;
        settings.Converters.Add(conv);
        Data = JsonConvert.DeserializeObject<CharacterDBData>(serializedData, settings) ?? new(1);
        settings.Converters.Remove(conv);
        foreach (var c in Data.Values)
        {
            UsedWorlds.Add(c.HomeWorldID);
            foreach (var job in c)
                job.SetParent(c);
        }
    }
    [Obsolete]
    internal CharacterDB(HrtDataManager dataManager, LegacyCharacterDB oldDB, LocalIDProvider idProvider)
    {
        DataManager = dataManager;
        Data = new(1);
        Data.Migrate(oldDB, idProvider);
    }
    internal ulong GetNextSequence() => Data.NextSequence++;
    internal IEnumerable<uint> GetUsedWorlds() => UsedWorlds;
    internal IReadOnlyList<string> GetCharactersList(uint worldID)
    {
        List<string> result = new();
        foreach (var character in Data.Values.Where(c => c.HomeWorldID == worldID))
            result.Add(character.Name);
        return result;
    }
    internal bool TryAddCharacter(Character c)
    {
        if (c.LocalID.IsEmpty)
            c.LocalID = DataManager.IDProvider.CreateID(HrtID.IDType.Character);
        return Data.TryAdd(c.LocalID, c);
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
        string result = JsonConvert.SerializeObject(Data, settings);
        settings.Converters.Remove(conv);
        return result;
    }
    private class CharacterDBData : Dictionary<HrtID, Character>
    {
        public int Version = 0;
        public ulong NextSequence = 1;
        public CharacterDBData() : base() { }
        public CharacterDBData(int ver)
        {
            Version = ver;
        }
        [Obsolete]
        internal void Migrate(LegacyCharacterDB oldDB, LocalIDProvider idProvider)
        {
            foreach (var db in oldDB.CharDB.Values)
            {
                foreach (Character c in db.Values)
                {
                    if (c.LocalID.IsEmpty)
                        c.LocalID = idProvider.CreateCharID(NextSequence++);
                    Add(c.LocalID, c);
                }

            }
        }
    }
}
