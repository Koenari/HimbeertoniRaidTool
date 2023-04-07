using System.Diagnostics.CodeAnalysis;
using Dalamud.Logging;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDB
{
    private readonly HrtDataManager DataManager;
    private readonly GearDBData Data;
    private readonly Dictionary<string, HrtID> EtroLookup = new();
    private bool EtroHasUpdated = false;
    internal GearDB(HrtDataManager dataManager, string gearData, JsonSerializerSettings settings)
    {
        DataManager = dataManager;
        Data = JsonConvert.DeserializeObject<GearDBData>(gearData, settings) ?? new(1);
    }
    [Obsolete]
    internal GearDB(HrtDataManager dataManager, LegacyGearDB oldDB, LocalIDProvider localIDProvider)
    {
        DataManager = dataManager;
        Data = new(1);
        Data.Migrate(oldDB, localIDProvider);
    }
    internal ulong GetNextSequence() => Data.NextSequence++;
    internal bool AddSet(GearSet gearSet)
    {
        if (gearSet.LocalID.IsEmpty)
            gearSet.LocalID = DataManager.IDProvider.CreateID(HrtID.IDType.Gear);
        return Data.TryAdd(gearSet.LocalID, gearSet);
    }
    internal bool TryGetSet(HrtID id, [NotNullWhen(true)] out GearSet? gearSet)
    {
        gearSet = null;
        if (id.IsEmpty)
            return false;
        return Data.TryGetValue(id, out gearSet);
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
    [Obsolete]
    internal bool TryGetByOldId(string oldID, [NotNullWhen(true)] out GearSet? gearSet)
    {
        gearSet = null;
        var entry = Data.FirstOrDefault(e => e.Value.OldHrtID == oldID, new KeyValuePair<HrtID, GearSet>(HrtID.Empty, null!));
        if (entry.Key.IsEmpty)
            return false;
        gearSet = entry.Value;
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
        int totalCount = 0;
        int updateCount = 0;
        foreach (var gearSet in Data.Values.Where(set => set.ManagedBy == GearSetManager.Etro))
        {
            totalCount++;
            if (gearSet.EtroFetchDate >= OldestValid)
                continue;
            ServiceManager.ConnectorPool.EtroConnector.GetGearSet(gearSet);
            updateCount++;
        }
        return new()
        {
            MessageType = HrtUiMessageType.Info,
            Message = $"Finished periodic etro Updates. ({updateCount}/{totalCount}) updated"
        };
    }
    internal string Serialize(JsonSerializerSettings settings)
    {
        return JsonConvert.SerializeObject(Data, settings);
    }
    private class GearDBData : Dictionary<HrtID, GearSet>
    {
        [JsonProperty] public int Version = 0;
        [JsonProperty] public ulong NextSequence = 1;
        public GearDBData() : base() { }
        public GearDBData(int ver)
        {
            Version = ver;
        }
        [Obsolete]
        public void Migrate(LegacyGearDB oldDb, LocalIDProvider idProvider)
        {
            foreach (var gearSet in oldDb.HrtGearDB.Values)
            {
                if (gearSet.LocalID.IsEmpty)
                {
                    gearSet.LocalID = idProvider.CreateGearID(NextSequence++);
                }
                Add(gearSet.LocalID, gearSet);
            }
            foreach (var gearSet in oldDb.EtroGearDB.Values)
            {
                if (gearSet.LocalID.IsEmpty)
                {
                    gearSet.LocalID = idProvider.CreateGearID(NextSequence++);
                }
                Add(gearSet.LocalID, gearSet);
            }
        }
    }
}
