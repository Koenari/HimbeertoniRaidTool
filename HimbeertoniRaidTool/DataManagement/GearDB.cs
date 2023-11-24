using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDb : DataBaseTable<GearSet, GearSet>
{
    private readonly Dictionary<string, HrtId> _etroLookup = new();

    internal GearDb(IIdProvider idProvider, string gearData, JsonSerializerSettings settings) : base(idProvider, gearData, null, settings)
    {
        if (LoadError)
            return;
        foreach ((HrtId id,GearSet set) in Data)
        {
            if (set.ManagedBy == GearSetManager.Etro)
                _etroLookup.TryAdd(set.EtroId, id);
        }


    }
    internal bool TryGetSetByEtroId(string etroId, [NotNullWhen(true)] out GearSet? set)
    {
        if (_etroLookup.TryGetValue(etroId, out HrtId? id))
            return TryGet(id, out set);
        id = Data.FirstOrDefault(s => s.Value.EtroId == etroId).Key;
        if (id is not null)
        {
            _etroLookup.Add(etroId, id);
            set = Data[id];
            return true;
        }
        set = null;
        return false;
    }

    internal void Prune(CharacterDb charDb)
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
}