
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDB
{
    private readonly HrtDataManager DataManager;
    private readonly Dictionary<HrtID, GearSet> Data = new();
    private readonly Dictionary<string, HrtID> EtroLookup = new();
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
            foreach (var set in data)
            {
                Data.TryAdd(set.LocalID, set);
                if (set.ManagedBy == GearSetManager.Etro)
                    EtroLookup.TryAdd(set.EtroID, set.LocalID);
                NextSequence = Math.Max(NextSequence, set.LocalID.Sequence);
            }
        }
        ServiceManager.PluginLog.Information($"DB contains {Data.Count} gear sets");
        NextSequence++;
    }
    [Obsolete]
    internal GearDB(HrtDataManager dataManager, LegacyGearDB oldDB, LocalIDProvider localIDProvider)
    {
        DataManager = dataManager;
        Data = new();
        int count = 0;
        foreach (var gearSet in oldDB.HrtGearDB.Values)
        {
            count++;
            if (gearSet.LocalID.IsEmpty)
            {
                gearSet.LocalID = localIDProvider.CreateGearID(NextSequence++);
            }
            Data.Add(gearSet.LocalID, gearSet);
        }
        foreach (var gearSet in oldDB.EtroGearDB.Values)
        {
            count++;
            if (gearSet.LocalID.IsEmpty)
            {
                gearSet.LocalID = localIDProvider.CreateGearID(NextSequence++);
            }
            Data.Add(gearSet.LocalID, gearSet);
        }
        ServiceManager.PluginLog.Information($"Migrated {count} gear sets");
    }
    internal ulong GetNextSequence() => NextSequence++;
    internal bool AddSet(GearSet gearSet)
    {
        if (gearSet.LocalID.IsEmpty)
            gearSet.LocalID = DataManager.IDProvider.CreateID(HrtID.IDType.Gear);
        return Data.TryAdd(gearSet.LocalID, gearSet);
    }
    internal bool TryGetSet(HrtID id, [NotNullWhen(true)] out GearSet? gearSet)
    {
        gearSet = null;
        return !id.IsEmpty && Data.TryGetValue(id, out gearSet);
    }
    internal bool TryGetSetByEtroID(string etroID, [NotNullWhen(true)] out GearSet? set)
    {
        if (EtroLookup.TryGetValue(etroID, out HrtID? id))
            return TryGetSet(id, out set);
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
        ServiceManager.TaskManager.RegisterTask(new(() => UpdateEtroSetsAsync(updateAll, maxAgeInDays), LogUpdates,"Update etro sets"));
    }

    internal void Prune(CharacterDB charDb)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of gear database.");
        foreach (HrtID toPrune in charDb.FindOrphanedGearSets(Data.Keys))
        { 
            if(!Data.TryGetValue(toPrune, out GearSet? set)) continue;
            ServiceManager.PluginLog.Information($"Removed {set.Name} ({set.LocalID}) from DB");
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
        var oldestValid = DateTime.UtcNow - new TimeSpan(maxAgeInDays, 0, 0, 0);
        int totalCount = 0;
        int updateCount = 0;
        foreach (var gearSet in Data.Values.Where(set => set.ManagedBy == GearSetManager.Etro))
        {
            totalCount++;
            if (gearSet.IsEmpty || (gearSet.EtroFetchDate < oldestValid && updateAll))
            {
                var message = ServiceManager.ConnectorPool.EtroConnector.GetGearSet(gearSet);
                if(message.MessageType is HrtUiMessageType.Error or HrtUiMessageType.Failure)
                    ServiceManager.PluginLog.Error(message.Message);
                updateCount++;
            }
        }

        return new HrtUiMessage($"Finished periodic etro Updates. ({updateCount}/{totalCount}) updated");
    
    }
    internal string Serialize(JsonSerializerSettings settings)
    {
        return JsonConvert.SerializeObject(Data.Values, settings);
    }
}
