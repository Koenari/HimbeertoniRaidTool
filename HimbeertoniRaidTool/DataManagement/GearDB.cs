using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    internal class GearDB
    {
        private readonly Dictionary<string, GearSet> HrtGearDB;
        private readonly Dictionary<string, GearSet> EtroGearDB;
        private readonly FileInfo HrtDBJsonFile;
        private readonly FileInfo EtroDBJsonFile;
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
        internal void AddOrGetSet(ref GearSet gearSet)
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
        }
        internal void UpdateIndex(string oldID, ref GearSet gs)
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
        }
        internal void Save()
        {
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
            }
            finally
            {
                hrtWriter?.Dispose();
                etroWriter?.Dispose();
            }
        }
    }
}
