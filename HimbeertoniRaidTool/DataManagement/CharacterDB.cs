using System.Diagnostics.CodeAnalysis;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDB : IDataBaseTable<Character>
{
    private readonly Dictionary<ulong, HrtId> CharIDLookup = new();
    private readonly Dictionary<HrtId, Character> Data = new();
    private readonly HrtDataManager DataManager;
    private readonly Dictionary<HrtId, HrtId> IDReplacement = new();
    private readonly Dictionary<(uint, string), HrtId> NameLookup = new();
    private readonly HashSet<uint> UsedWorlds = new();
    private ulong NextSequence;

    internal CharacterDB(HrtDataManager dataManager, string serializedData, HrtIdReferenceConverter<GearSet> conv,
        JsonSerializerSettings settings)
    {
        DataManager = dataManager;
        settings.Converters.Add(conv);
        var data = JsonConvert.DeserializeObject<List<Character>>(serializedData, settings);
        settings.Converters.Remove(conv);
        if (data is null)
        {
            ServiceManager.PluginLog.Error("Could not load CharacterDB");
        }
        else
        {
            HashSet<HrtId> KnownGear = new();
            foreach (Character c in data)
            {
                if (c.LocalId.IsEmpty)
                {
                    ServiceManager.PluginLog.Error(
                        $"Character {c.Name} was missing an ID and was removed from the database");
                    continue;
                }

                if (Data.TryAdd(c.LocalId, c))
                {
                    UsedWorlds.Add(c.HomeWorldId);
                    if (!NameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId))
                    {
                        ServiceManager.PluginLog.Warning(
                            $"Database conatains {c.Name} @ {c.HomeWorld?.Name} twice. Characters were merged");
                        TryGet(NameLookup[(c.HomeWorldId, c.Name)], out Character? other);
                        IDReplacement.Add(c.LocalId, other!.LocalId);
                        other.MergeInfos(c);
                        Data.Remove(c.LocalId);
                        continue;
                    }

                    if (c.CharID > 0)
                        CharIDLookup.TryAdd(c.CharID, c.LocalId);
                    NextSequence = Math.Max(NextSequence, c.LocalId.Sequence);
                    foreach (PlayableClass job in c)
                    {
                        job.SetParent(c);
                        if (KnownGear.Contains(job.Gear.LocalId))
                        {
                            GearSet gearCopy = job.Gear.Clone();
                            ServiceManager.PluginLog.Debug(
                                $"Found Gear duplicate with Sequence: {gearCopy.LocalId.Sequence}");
                            gearCopy.LocalId = HrtId.Empty;
                            dataManager.GearDB.AddSet(gearCopy);
                            job.Gear = gearCopy;
                        }
                        else
                        {
                            KnownGear.Add(job.Gear.LocalId);
                        }
                    }
                }
            }
        }

        ServiceManager.PluginLog.Information($"DB contains {Data.Count} characters");
        NextSequence++;
    }

    internal ulong GetNextSequence() => NextSequence++;

    internal IEnumerable<uint> GetUsedWorlds() => UsedWorlds;

    internal IReadOnlyList<string> GetKnownCharacters(uint worldID)
    {
        List<string> result = new();
        foreach (Character character in Data.Values.Where(c => c.HomeWorldId == worldID))
            result.Add(character.Name);
        return result;
    }

    internal bool TryAddCharacter(in Character c)
    {
        if (c.LocalId.IsEmpty)
            c.LocalId = DataManager.IDProvider.CreateID(HrtId.IdType.Character);
        if (Data.TryAdd(c.LocalId, c))
        {
            UsedWorlds.Add(c.HomeWorldId);
            NameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
            if (c.CharID > 0)
                CharIDLookup.TryAdd(c.CharID, c.LocalId);
            return true;
        }

        return false;
    }

    internal bool TryGetCharacterByCharId(ulong charID, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (CharIDLookup.TryGetValue(charID, out HrtId? id))
            return TryGet(id, out c);
        id = Data.FirstOrDefault(x => x.Value.CharID == charID).Key;
        if (id is not null)
        {
            CharIDLookup.Add(charID, id);
            c = Data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character with ID: {charID} in database");
        return false;
    }

    internal bool SearchCharacter(uint worldID, string name, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (NameLookup.TryGetValue((worldID, name), out HrtId? id))
            return TryGet(id, out c);
        id = Data.FirstOrDefault(x => x.Value.HomeWorldId == worldID && x.Value.Name.Equals(name)).Key;
        if (id is not null)
        {
            NameLookup.Add((worldID, name), id);
            c = Data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character {name}@{worldID} in database");
        return false;
    }

    public bool TryGet(HrtId id, [NotNullWhen(true)] out Character? c)
    {
        if (IDReplacement.ContainsKey(id))
            id = IDReplacement[id];
        return Data.TryGetValue(id, out c);
    }

    internal bool Contains(HrtId hrtID) => Data.ContainsKey(hrtID);

    internal void ReindexCharacter(HrtId localID)
    {
        if (!TryGet(localID, out Character? c))
            return;
        UsedWorlds.Add(c.HomeWorldId);
        NameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
        if (c.CharID > 0)
            CharIDLookup.TryAdd(c.CharID, c.LocalId);
    }

    internal IEnumerable<HrtId> FindOrphanedGearSets(IEnumerable<HrtId> possibleOrphans)
    {
        HashSet<HrtId> orphanSets = new(possibleOrphans);
        foreach (PlayableClass job in Data.Values.SelectMany(character => character.Classes))
        {
            orphanSets.Remove(job.Gear.LocalId);
            orphanSets.Remove(job.BIS.LocalId);
        }

        ServiceManager.PluginLog.Information($"Found {orphanSets.Count} orphaned gear sets.");
        return orphanSets;
    }

    internal string Serialize(HrtIdReferenceConverter<GearSet> conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters.Remove(conv);
        return result;
    }

    internal void Prune(HrtDataManager hrtDataManager)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of character database.");
        foreach (HrtId toPrune in hrtDataManager.FindOrphanedCharacters(Data.Keys))
        {
            if (!Data.TryGetValue(toPrune, out Character? character)) continue;
            ServiceManager.PluginLog.Information(
                $"Removed {character.Name} @ {character.HomeWorld?.Name} ({character.LocalId}) from DB");
            Data.Remove(toPrune);
        }

        ServiceManager.PluginLog.Debug("Finished pruning of character database.");
    }
}