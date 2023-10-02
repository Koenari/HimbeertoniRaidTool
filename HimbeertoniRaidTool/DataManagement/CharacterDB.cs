using System.Diagnostics.CodeAnalysis;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDB
{
    private readonly Dictionary<ulong, HrtID> CharIDLookup = new();
    private readonly Dictionary<HrtID, Character> Data = new();
    private readonly HrtDataManager DataManager;
    private readonly Dictionary<HrtID, HrtID> IDReplacement = new();
    private readonly Dictionary<(uint, string), HrtID> NameLookup = new();
    private readonly HashSet<uint> UsedWorlds = new();
    private ulong NextSequence;

    internal CharacterDB(HrtDataManager dataManager, string serializedData, GearsetReferenceConverter conv,
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
            HashSet<HrtID> KnownGear = new();
            foreach (Character c in data)
            {
                if (c.LocalID.IsEmpty)
                {
                    ServiceManager.PluginLog.Error(
                        $"Character {c.Name} was missing an ID and was removed from the database");
                    continue;
                }

                if (Data.TryAdd(c.LocalID, c))
                {
                    UsedWorlds.Add(c.HomeWorldID);
                    if (!NameLookup.TryAdd((c.HomeWorldID, c.Name), c.LocalID))
                    {
                        ServiceManager.PluginLog.Warning(
                            $"Database conatains {c.Name} @ {c.HomeWorld?.Name} twice. Characters were merged");
                        TryGetCharacter(NameLookup[(c.HomeWorldID, c.Name)], out Character? other);
                        IDReplacement.Add(c.LocalID, other!.LocalID);
                        other.MergeInfos(c);
                        Data.Remove(c.LocalID);
                        continue;
                    }

                    if (c.CharID > 0)
                        CharIDLookup.TryAdd(c.CharID, c.LocalID);
                    NextSequence = Math.Max(NextSequence, c.LocalID.Sequence);
                    foreach (PlayableClass job in c)
                    {
                        job.SetParent(c);
                        if (KnownGear.Contains(job.Gear.LocalID))
                        {
                            GearSet gearCopy = job.Gear.Clone();
                            ServiceManager.PluginLog.Debug(
                                $"Found Gear duplicate with Sequence: {gearCopy.LocalID.Sequence}");
                            gearCopy.LocalID = HrtID.Empty;
                            dataManager.GearDB.AddSet(gearCopy);
                            job.Gear = gearCopy;
                        }
                        else
                        {
                            KnownGear.Add(job.Gear.LocalID);
                        }
                    }
                }
            }
        }

        ServiceManager.PluginLog.Information($"DB conatins {Data.Count} characters");
        NextSequence++;
    }

    [Obsolete]
    internal CharacterDB(HrtDataManager dataManager, LegacyCharacterDB oldDB, LocalIDProvider idProvider)
    {
        //Migration constructor
        DataManager = dataManager;
        Data = new Dictionary<HrtID, Character>();
        int count = 0;
        foreach (var db in oldDB.CharDB.Values)
        foreach (Character c in db.Values)
        {
            count++;
            if (c.LocalID.IsEmpty)
                c.LocalID = idProvider.CreateCharID(NextSequence++);
            Data.Add(c.LocalID, c);
        }

        ServiceManager.PluginLog.Information($"Migrated {count} characters");
    }

    internal ulong GetNextSequence()
    {
        return NextSequence++;
    }

    internal IEnumerable<uint> GetUsedWorlds()
    {
        return UsedWorlds;
    }

    internal IReadOnlyList<string> GetKnownChracters(uint worldID)
    {
        List<string> result = new();
        foreach (Character character in Data.Values.Where(c => c.HomeWorldID == worldID))
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

        ServiceManager.PluginLog.Debug($"Did not find character with ID: {charID} in database");
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

        ServiceManager.PluginLog.Debug($"Did not find character {name}@{worldID} in database");
        return false;
    }

    internal bool TryGetCharacter(HrtID id, [NotNullWhen(true)] out Character? c)
    {
        if (IDReplacement.ContainsKey(id))
            id = IDReplacement[id];
        return Data.TryGetValue(id, out c);
    }

    internal bool Contains(HrtID hrtID)
    {
        return Data.ContainsKey(hrtID);
    }

    internal void ReindexCharacter(HrtID localID)
    {
        if (!TryGetCharacter(localID, out Character? c))
            return;
        UsedWorlds.Add(c.HomeWorldID);
        NameLookup.TryAdd((c.HomeWorldID, c.Name), c.LocalID);
        if (c.CharID > 0)
            CharIDLookup.TryAdd(c.CharID, c.LocalID);
    }

    internal IEnumerable<HrtID> FindOrphanedGearSets(IEnumerable<HrtID> possibleOrphans)
    {
        HashSet<HrtID> orphanSets = new(possibleOrphans);
        foreach (PlayableClass job in Data.Values.SelectMany(character => character.Classes))
        {
            orphanSets.Remove(job.Gear.LocalID);
            orphanSets.Remove(job.BIS.LocalID);
        }

        ServiceManager.PluginLog.Information($"Found {orphanSets.Count} orphaned gear sets.");
        return orphanSets;
    }

    internal string Serialize(GearsetReferenceConverter conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters.Remove(conv);
        return result;
    }

    internal void Prune(HrtDataManager hrtDataManager)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of character database.");
        foreach (HrtID toPrune in hrtDataManager.FindOrphanedCharacters(Data.Keys))
        {
            if (!Data.TryGetValue(toPrune, out Character? character)) continue;
            ServiceManager.PluginLog.Information(
                $"Removed {character.Name} @ {character.HomeWorld?.Name} ({character.LocalID}) from DB");
            Data.Remove(toPrune);
        }

        ServiceManager.PluginLog.Debug("Finished pruning of character database.");
    }
}