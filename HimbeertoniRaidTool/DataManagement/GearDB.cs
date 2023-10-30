using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDb : IDataBaseTable<GearSet>
{
    private readonly HrtDataManager _dataManager;
    private readonly Dictionary<HrtId, GearSet> _data = new();
    private readonly Dictionary<string, HrtId> _etroLookup = new();
    private bool _etroHasUpdated = false;
    private ulong _nextSequence = 0;

    internal GearDb(HrtDataManager dataManager, string gearData, JsonSerializerSettings settings)
    {
        _dataManager = dataManager;
        var data = JsonConvert.DeserializeObject<List<GearSet>>(gearData, settings);
        if (data is null)
            ServiceManager.PluginLog.Error("Could not load GearDB");
        else
        {
            foreach (GearSet set in data)
            {
                _data.TryAdd(set.LocalId, set);
                if (set.ManagedBy == GearSetManager.Etro)
                    _etroLookup.TryAdd(set.EtroId, set.LocalId);
                _nextSequence = Math.Max(_nextSequence, set.LocalId.Sequence);
            }
        }
        ServiceManager.PluginLog.Information($"DB contains {_data.Count} gear sets");
        _nextSequence++;
    }
    internal ulong GetNextSequence() => _nextSequence++;
    internal bool AddSet(GearSet gearSet)
    {
        if (gearSet.LocalId.IsEmpty)
            gearSet.LocalId = _dataManager.IdProvider.CreateId(HrtId.IdType.Gear);
        return _data.TryAdd(gearSet.LocalId, gearSet);
    }
    public bool TryGet(HrtId id, [NotNullWhen(true)] out GearSet? gearSet)
    {
        gearSet = null;
        return !id.IsEmpty && _data.TryGetValue(id, out gearSet);
    }
    internal bool TryGetSetByEtroId(string etroId, [NotNullWhen(true)] out GearSet? set)
    {
        if (_etroLookup.TryGetValue(etroId, out HrtId? id))
            return TryGet(id, out set);
        id = _data.FirstOrDefault(s => s.Value.EtroId == etroId).Key;
        if (id is not null)
        {
            _etroLookup.Add(etroId, id);
            set = _data[id];
            return true;
        }
        set = null;
        return false;
    }
    internal void UpdateEtroSets(bool updateAll, int maxAgeInDays)
    {
        if (_etroHasUpdated)
            return;
        _etroHasUpdated = true;
        ServiceManager.TaskManager.RegisterTask(new HrtTask(() => UpdateEtroSetsAsync(updateAll, maxAgeInDays), LogUpdates, "Update etro sets"));
    }

    internal void Prune(CharacterDb charDb)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of gear database.");
        foreach (HrtId toPrune in charDb.FindOrphanedGearSets(_data.Keys))
        {
            if (!_data.TryGetValue(toPrune, out GearSet? set)) continue;
            ServiceManager.PluginLog.Information($"Removed {set.Name} ({set.LocalId}) from DB");
            _data.Remove(toPrune);
        }
        ServiceManager.PluginLog.Debug("Finished pruning of gear database.");
    }
    private static void LogUpdates(HrtUiMessage hrtUiMessage)
    {
        ServiceManager.PluginLog.Information(hrtUiMessage.Message);
    }
    private HrtUiMessage UpdateEtroSetsAsync(bool updateAll, int maxAgeInDays)
    {
        DateTime oldestValid = DateTime.UtcNow - new TimeSpan(maxAgeInDays, 0, 0, 0);
        int totalCount = 0;
        int updateCount = 0;
        foreach (GearSet gearSet in _data.Values.Where(set => set.ManagedBy == GearSetManager.Etro))
        {
            totalCount++;
            if (gearSet.IsEmpty || gearSet.EtroFetchDate < oldestValid && updateAll)
            {
                HrtUiMessage message = ServiceManager.ConnectorPool.EtroConnector.GetGearSet(gearSet);
                if (message.MessageType is HrtUiMessageType.Error or HrtUiMessageType.Failure)
                    ServiceManager.PluginLog.Error(message.Message);
                updateCount++;
            }
        }

        return new HrtUiMessage($"Finished periodic etro Updates. ({updateCount}/{totalCount}) updated");

    }
    internal string Serialize(JsonSerializerSettings settings) => JsonConvert.SerializeObject(_data.Values, settings);
}