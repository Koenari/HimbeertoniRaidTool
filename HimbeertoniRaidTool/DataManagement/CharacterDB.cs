using System.Diagnostics.CodeAnalysis;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDb : IDataBaseTable<Character>
{
    private readonly Dictionary<ulong, HrtId> _charIdLookup = new();
    private readonly Dictionary<HrtId, Character> _data = new();
    private readonly HrtDataManager _dataManager;
    private readonly Dictionary<HrtId, HrtId> _idReplacement = new();
    private readonly Dictionary<(uint, string), HrtId> _nameLookup = new();
    private readonly HashSet<uint> _usedWorlds = new();
    private ulong _nextSequence;

    internal CharacterDb(HrtDataManager dataManager, string serializedData, HrtIdReferenceConverter<GearSet> conv,
        JsonSerializerSettings settings)
    {
        _dataManager = dataManager;
        settings.Converters.Add(conv);
        var data = JsonConvert.DeserializeObject<List<Character>>(serializedData, settings);
        settings.Converters.Remove(conv);
        if (data is null)
        {
            ServiceManager.PluginLog.Error("Could not load CharacterDB");
        }
        else
        {
            HashSet<HrtId> knownGear = new();
            foreach (Character c in data)
            {
                if (c.LocalId.IsEmpty)
                {
                    ServiceManager.PluginLog.Error(
                        $"Character {c.Name} was missing an ID and was removed from the database");
                    continue;
                }

                if (_data.TryAdd(c.LocalId, c))
                {
                    _usedWorlds.Add(c.HomeWorldId);
                    if (!_nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId))
                    {
                        ServiceManager.PluginLog.Warning(
                            $"Database conatains {c.Name} @ {c.HomeWorld?.Name} twice. Characters were merged");
                        TryGet(_nameLookup[(c.HomeWorldId, c.Name)], out Character? other);
                        _idReplacement.Add(c.LocalId, other!.LocalId);
                        other.MergeInfos(c);
                        _data.Remove(c.LocalId);
                        continue;
                    }

                    if (c.CharId > 0)
                        _charIdLookup.TryAdd(c.CharId, c.LocalId);
                    _nextSequence = Math.Max(_nextSequence, c.LocalId.Sequence);
                    foreach (PlayableClass job in c)
                    {
                        job.SetParent(c);
                        if (knownGear.Contains(job.Gear.LocalId))
                        {
                            GearSet gearCopy = job.Gear.Clone();
                            ServiceManager.PluginLog.Debug(
                                $"Found Gear duplicate with Sequence: {gearCopy.LocalId.Sequence}");
                            gearCopy.LocalId = HrtId.Empty;
                            dataManager.GearDb.AddSet(gearCopy);
                            job.Gear = gearCopy;
                        }
                        else
                        {
                            knownGear.Add(job.Gear.LocalId);
                        }
                    }
                }
            }
        }

        ServiceManager.PluginLog.Information($"DB contains {_data.Count} characters");
        _nextSequence++;
    }

    internal ulong GetNextSequence() => _nextSequence++;

    internal IEnumerable<uint> GetUsedWorlds() => _usedWorlds;

    internal IReadOnlyList<string> GetKnownCharacters(uint worldId)
    {
        List<string> result = new();
        foreach (Character character in _data.Values.Where(c => c.HomeWorldId == worldId))
            result.Add(character.Name);
        return result;
    }

    internal bool TryAddCharacter(in Character c)
    {
        if (c.LocalId.IsEmpty)
            c.LocalId = _dataManager.IdProvider.CreateId(HrtId.IdType.Character);
        if (_data.TryAdd(c.LocalId, c))
        {
            _usedWorlds.Add(c.HomeWorldId);
            _nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
            if (c.CharId > 0)
                _charIdLookup.TryAdd(c.CharId, c.LocalId);
            return true;
        }

        return false;
    }

    internal bool TryGetCharacterByCharId(ulong charId, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (_charIdLookup.TryGetValue(charId, out HrtId? id))
            return TryGet(id, out c);
        id = _data.FirstOrDefault(x => x.Value.CharId == charId).Key;
        if (id is not null)
        {
            _charIdLookup.Add(charId, id);
            c = _data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character with ID: {charId} in database");
        return false;
    }

    internal bool SearchCharacter(uint worldId, string name, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (_nameLookup.TryGetValue((worldId, name), out HrtId? id))
            return TryGet(id, out c);
        id = _data.FirstOrDefault(x => x.Value.HomeWorldId == worldId && x.Value.Name.Equals(name)).Key;
        if (id is not null)
        {
            _nameLookup.Add((worldId, name), id);
            c = _data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character {name}@{worldId} in database");
        return false;
    }

    public bool TryGet(HrtId id, [NotNullWhen(true)] out Character? c)
    {
        if (_idReplacement.ContainsKey(id))
            id = _idReplacement[id];
        return _data.TryGetValue(id, out c);
    }

    internal bool Contains(HrtId hrtId) => _data.ContainsKey(hrtId);

    internal void ReindexCharacter(HrtId localId)
    {
        if (!TryGet(localId, out Character? c))
            return;
        _usedWorlds.Add(c.HomeWorldId);
        _nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
        if (c.CharId > 0)
            _charIdLookup.TryAdd(c.CharId, c.LocalId);
    }

    internal IEnumerable<HrtId> FindOrphanedGearSets(IEnumerable<HrtId> possibleOrphans)
    {
        HashSet<HrtId> orphanSets = new(possibleOrphans);
        foreach (PlayableClass job in _data.Values.SelectMany(character => character.Classes))
        {
            orphanSets.Remove(job.Gear.LocalId);
            orphanSets.Remove(job.Bis.LocalId);
        }

        ServiceManager.PluginLog.Information($"Found {orphanSets.Count} orphaned gear sets.");
        return orphanSets;
    }

    internal string Serialize(HrtIdReferenceConverter<GearSet> conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        string result = JsonConvert.SerializeObject(_data.Values, settings);
        settings.Converters.Remove(conv);
        return result;
    }

    internal void Prune(HrtDataManager hrtDataManager)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of character database.");
        foreach (HrtId toPrune in hrtDataManager.FindOrphanedCharacters(_data.Keys))
        {
            if (!_data.TryGetValue(toPrune, out Character? character)) continue;
            ServiceManager.PluginLog.Information(
                $"Removed {character.Name} @ {character.HomeWorld?.Name} ({character.LocalId}) from DB");
            _data.Remove(toPrune);
        }

        ServiceManager.PluginLog.Debug("Finished pruning of character database.");
    }
}