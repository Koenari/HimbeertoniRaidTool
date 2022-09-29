using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    internal class GearDB
    {
        private readonly Dictionary<string, GearSet> HrtGearDB;
        private readonly Dictionary<string, GearSet> EtroGearDB;
        private readonly FileInfo HrtDBJsonFile;
        private readonly FileInfo EtroDBJsonFile;
        private bool EtroHasUpdated = false;
        internal GearDB(DirectoryInfo loadDir)
        {
            HrtDBJsonFile = new FileInfo(loadDir.FullName + "\\HrtGearDB.json");
            if (!HrtDBJsonFile.Exists)
                HrtDBJsonFile.Create().Close();
            EtroDBJsonFile = new FileInfo(loadDir.FullName + "\\EtroGearDB.json");

            if (!EtroDBJsonFile.Exists)
                EtroDBJsonFile.Create().Close();
            HrtGearDB = JsonConvert.DeserializeObject<Dictionary<string, GearSet>>(
                HrtDBJsonFile.OpenText().ReadToEnd(),
                HrtDataManager.JsonSerializerSettings) ?? new();
            EtroGearDB = JsonConvert.DeserializeObject<Dictionary<string, GearSet>>(
                EtroDBJsonFile.OpenText().ReadToEnd(),
                HrtDataManager.JsonSerializerSettings) ?? new();
        }
        internal bool AddOrGetSet(ref GearSet gearSet)
        {
            if (gearSet.ManagedBy == GearSetManager.HRT && gearSet.HrtID.Length > 0)
            {
                if (!HrtGearDB.ContainsKey(gearSet.HrtID))
                    HrtGearDB.Add(gearSet.HrtID, gearSet);
                if (HrtGearDB.TryGetValue(gearSet.HrtID, out GearSet? result))
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
                if (EtroGearDB.TryGetValue(gearSet.EtroID, out GearSet? result))
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
            Services.TaskManager.RegisterTask(LogUpdates, () => UpdateEtroSetsAsync(maxAgeInDays));
        }
        private void LogUpdates(HrtUiMessage hrtUiMessage)
        {
            PluginLog.Information(hrtUiMessage.Message);
        }
        private HrtUiMessage UpdateEtroSetsAsync(int maxAgeInDays)
        {
            DateTime OldestValid = DateTime.UtcNow - new TimeSpan(maxAgeInDays, 0, 0, 0);
            DateTime ErrorIfOlder = DateTime.UtcNow - new TimeSpan(365, 0, 0, 0);
            int updateCount = 0;
            foreach (GearSet gearSet in EtroGearDB.Values)
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
                Services.ConnectorPool.EtroConnector.GetGearSet(gearSet);
                updateCount++;
            }
            return new()
            {
                MessageType = HrtUiMessageType.Info,
                Message = $"Finished periodic etro Updates. ({updateCount}/{EtroGearDB.Count}) updated"
            };
        }
        internal bool Save()
        {
            bool hasError = false;
            StreamWriter? hrtWriter = null;
            StreamWriter? etroWriter = null;
            try
            {
                hrtWriter = HrtDBJsonFile.CreateText();
                etroWriter = EtroDBJsonFile.CreateText();
                var serializer = JsonSerializer.Create(HrtDataManager.JsonSerializerSettings);
                serializer.Serialize(hrtWriter, HrtGearDB);
                serializer.Serialize(etroWriter, EtroGearDB);
            }
            catch (Exception e)
            {
                PluginLog.Error("Could not write gear data\n{0}", e);
                hasError = true;
            }
            finally
            {
                hrtWriter?.Dispose();
                etroWriter?.Dispose();
            }
            return !hasError;
        }
    }
}
