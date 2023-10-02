
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;
[Obsolete]
internal class LegacyGearDB
{
    internal readonly Dictionary<string, GearSet> HrtGearDB;
    internal readonly Dictionary<string, GearSet> EtroGearDB;
    private bool EtroHasUpdated = false;
    internal LegacyGearDB(string hrtGearData, string etroGearData, JsonSerializerSettings settings)
    {
        HrtGearDB = JsonConvert.DeserializeObject<Dictionary<string, GearSet>>(
            hrtGearData, settings) ?? new();
        EtroGearDB = JsonConvert.DeserializeObject<Dictionary<string, GearSet>>(
            etroGearData, settings) ?? new();
    }
    internal bool AddOrGetSet(ref GearSet gearSet)
    {
        if (gearSet.ManagedBy == GearSetManager.HRT && gearSet.OldHrtID.Length > 0)
        {
            if (!HrtGearDB.ContainsKey(gearSet.OldHrtID))
                HrtGearDB.Add(gearSet.OldHrtID, gearSet);
            if (HrtGearDB.TryGetValue(gearSet.OldHrtID, out var result))
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
        ServiceManager.PluginLog.Information(hrtUiMessage.Message);
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
[Obsolete]
internal class LegacyCharacterDB
{
    internal readonly Dictionary<uint, Dictionary<string, Character>> CharDB;


    internal LegacyCharacterDB(string serializedData, LegacyGearSetReferenceConverter conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        CharDB = JsonConvert.DeserializeObject<Dictionary<uint, Dictionary<string, Character>>>(
            serializedData, settings) ?? new();
        settings.Converters.Remove(conv);
        //Potentially parallelize later
        foreach (var SingleWorldDB in CharDB.Values)
            foreach (var c in SingleWorldDB.Values)
                foreach (var job in c)
                    job.SetParent(c);
    }
    internal List<uint> GetUsedWorlds()
    {
        return new List<uint>(CharDB.Keys);
    }
    internal List<string> GetCharactersList(uint worldID)
    {
        List<string> result = new();
        foreach (var character in CharDB[worldID].Values)
            result.Add(character.Name);
        return result;
    }
    internal bool Exists(uint worldID, string name) =>
        CharDB.ContainsKey(worldID) && CharDB[worldID].ContainsKey(name);

    internal bool AddOrGetCharacter(ref Character c)
    {
        if (!CharDB.ContainsKey(c.HomeWorldID))
            CharDB.Add(c.HomeWorldID, new Dictionary<string, Character>());
        if (!CharDB[c.HomeWorldID].ContainsKey(c.Name))
            CharDB[c.HomeWorldID].Add(c.Name, c);
        if (CharDB[c.HomeWorldID].TryGetValue(c.Name, out var c2))
            c = c2;
        return true;
    }
    internal bool UpdateIndex(uint oldWorld, string oldName, ref Character c)
    {
        if (CharDB.ContainsKey(oldWorld))
            CharDB[oldWorld].Remove(oldName);
        AddOrGetCharacter(ref c);
        return true;
    }
    internal string Serialize(LegacyGearSetReferenceConverter conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        string result = JsonConvert.SerializeObject(CharDB, settings);
        settings.Converters.Remove(conv);
        return result;
    }

}
