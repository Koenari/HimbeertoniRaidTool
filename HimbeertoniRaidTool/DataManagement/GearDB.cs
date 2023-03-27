using Dalamud.Logging;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDB
{
    private readonly Dictionary<string, GearSet> HrtGearDB;
    private readonly Dictionary<string, GearSet> EtroGearDB;
    private bool EtroHasUpdated = false;
    internal GearDB(string hrtGearData, string etroGearData, JsonSerializerSettings settings)
    {
        HrtGearDB = JsonConvert.DeserializeObject<Dictionary<string, GearSet>>(
            hrtGearData, settings) ?? new();
        EtroGearDB = JsonConvert.DeserializeObject<Dictionary<string, GearSet>>(
            etroGearData, settings) ?? new();
    }
    internal bool AddOrGetSet(ref GearSet gearSet)
    {
        if (gearSet.ManagedBy == GearSetManager.HRT && gearSet.HrtID.Length > 0)
        {
            if (!HrtGearDB.ContainsKey(gearSet.HrtID))
                HrtGearDB.Add(gearSet.HrtID, gearSet);
            if (HrtGearDB.TryGetValue(gearSet.HrtID, out var result))
            {
                if (result.TimeStamp < gearSet.TimeStamp)
                    result.CopyFrom(gearSet);
                gearSet = result;
            }
        }
        else if (gearSet.ManagedBy == GearSetManager.Etro && gearSet.EtroID.Length > 0)
        {
            if (!EtroGearDB.ContainsKey(gearSet.EtroID))
                EtroGearDB.Add(gearSet.EtroID, gearSet);
            if (EtroGearDB.TryGetValue(gearSet.EtroID, out var result))
                gearSet = result;
        }
        return true;
    }
    internal bool UpdateIndex(string oldID, ref GearSet gs)
    {
        if (gs.ManagedBy == GearSetManager.HRT)
        {
            if (HrtGearDB.ContainsKey(oldID))
                HrtGearDB.Remove(oldID);
            AddOrGetSet(ref gs);
        }
        else if (gs.ManagedBy == GearSetManager.Etro)
        {
            if (EtroGearDB.ContainsKey(oldID))
                EtroGearDB.Remove(oldID);
            AddOrGetSet(ref gs);
        }
        return true;
    }
    internal void UpdateEtroSets(int maxAgeInDays)
    {
        if (EtroHasUpdated)
            return;
        EtroHasUpdated = true;
        ServiceManager.TaskManager.RegisterTask(new(() => UpdateEtroSetsAsync(maxAgeInDays), LogUpdates));
    }
    private void LogUpdates(HrtUiMessage hrtUiMessage)
    {
        PluginLog.Information(hrtUiMessage.Message);
    }
    private HrtUiMessage UpdateEtroSetsAsync(int maxAgeInDays)
    {
        var OldestValid = DateTime.UtcNow - new TimeSpan(maxAgeInDays, 0, 0, 0);
        var ErrorIfOlder = DateTime.UtcNow - new TimeSpan(365, 0, 0, 0);
        int updateCount = 0;
        foreach (var gearSet in EtroGearDB.Values)
        {
            if (gearSet.EtroFetchDate >= OldestValid)
                continue;
            //Spaces out older entries (before fetrtching was tracked) over the time period equally
            //This should manage the amount of request to be minimal (per day)
            if (gearSet.EtroFetchDate < ErrorIfOlder)
            {
                gearSet.EtroFetchDate = DateTime.UtcNow - new TimeSpan(Random.Shared.Next(maxAgeInDays), 0, 0, 0);
                continue;
            }
            ServiceManager.ConnectorPool.EtroConnector.GetGearSet(gearSet);
            updateCount++;
        }
        return new()
        {
            MessageType = HrtUiMessageType.Info,
            Message = $"Finished periodic etro Updates. ({updateCount}/{EtroGearDB.Count}) updated"
        };
    }
    internal (string hrt, string etro) Serialize(JsonSerializerSettings settings)
    {
        string hrtData = JsonConvert.SerializeObject(HrtGearDB, settings);
        string etroData = JsonConvert.SerializeObject(EtroGearDB, settings);
        return (hrtData, etroData);
    }
}
