using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDB : IDataBaseTable<GearSet>
{
    private readonly HrtDataManager DataManager;
    private readonly Dictionary<HrtId, GearSet> Data = new();
    private readonly Dictionary<string, HrtId> EtroLookup = new();
    private bool EtroHasUpdated = false;
    private ulong NextSequence = 0;

    internal GearDB(HrtDataManager dataManager, string gearData, JsonSerializerSettings settings)
    {
        DataManager = dataManager;
        var data = JsonConvert.DeserializeObject<List<GearSet>>(gearData, settings);
        if (data is null)
            ServiceManager.PluginLog.Error("Could not load GearDB");
        else
        {
            foreach (GearSet set in data)
            {
                Data.TryAdd(set.LocalId, set);
                if (set.ManagedBy == GearSetManager.Etro)
                    EtroLookup.TryAdd(set.EtroID, set.LocalId);
                NextSequence = Math.Max(NextSequence, set.LocalId.Sequence);
            }
        }
        ServiceManager.PluginLog.Information($"DB contains {Data.Count} gear sets");
        NextSequence++;
    }
    internal ulong GetNextSequence() => NextSequence++;
    internal bool AddSet(GearSet gearSet)
    {
        if (gearSet.LocalId.IsEmpty)
            gearSet.LocalId = DataManager.IDProvider.CreateID(HrtId.IdType.Gear);
        return Data.TryAdd(gearSet.LocalId, gearSet);
    }
    public bool TryGet(HrtId id, [NotNullWhen(true)] out GearSet? gearSet)
    {
        gearSet = null;
        return !id.IsEmpty && Data.TryGetValue(id, out gearSet);
    }
    internal bool TryGetSetByEtroID(string etroID, [NotNullWhen(true)] out GearSet? set)
    {
        if (EtroLookup.TryGetValue(etroID, out HrtId? id))
            return TryGet(id, out set);
        id = Data.FirstOrDefault(s => s.Value.EtroID == etroID).Key;
        if (id is not null)
        {
            EtroLookup.Add(etroID, id);
            set = Data[id];
            return true;
        }
        set = null;
        return false;
    }
    internal void UpdateEtroSets(bool updateAll, int maxAgeInDays)
    {
        if (EtroHasUpdated)
            return;
        EtroHasUpdated = true;
        ServiceManager.TaskManager.RegisterTask(new HrtTask(() => UpdateEtroSetsAsync(updateAll, maxAgeInDays), LogUpdates, "Update etro sets"));
    }

    internal void Prune(CharacterDB charDb)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of gear database.");
        foreach (HrtId toPrune in charDb.FindOrphanedGearSets(Data.Keys))
        {
            if (!Data.TryGetValue(toPrune, out GearSet? set)) continue;
            ServiceManager.PluginLog.Information($"Removed {set.Name} ({set.LocalId}) from DB");
            Data.Remove(toPrune);
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
        foreach (GearSet gearSet in Data.Values.Where(set => set.ManagedBy == GearSetManager.Etro))
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
    internal string Serialize(JsonSerializerSettings settings) => JsonConvert.SerializeObject(Data.Values, settings);
}