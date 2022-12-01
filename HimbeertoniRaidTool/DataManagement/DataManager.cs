using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Dalamud.Logging;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Common.Data;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    public class HrtDataManager
    {
        public bool Initialized { get; private set; }
        private volatile bool Saving = false;
        private volatile bool Serializing = false;
        //Data
        private readonly GearDB? GearDB;
        private readonly CharacterDB? CharacterDB;
        private readonly List<RaidGroup>? _Groups;
        //Files
        private const string HrtGearDBJsonFileName = "HrtGearDB.json";
        private readonly FileInfo? HrtGearDBJsonFile;
        private const string EtroGearDBJsonFileName = "EtroGearDB.json";
        private readonly FileInfo? EtroGearDBJsonFile;
        private const string CharDBJsonFileName = "CharacterDB.json";
        private readonly FileInfo? CharDBJsonFile;
        private const string RaidGroupJsonFileName = "RaidGroups.json";
        private readonly FileInfo? RaidGRoupJsonFile;
        //Converters
        private readonly GearSetReferenceConverter? GearSetRefConv;
        private readonly CharacterReferenceConverter? CharRefConv;
        public List<RaidGroup> Groups => _Groups ?? new();
        internal ModuleConfigurationManager? ModuleConfigurationManager { get; private set; }
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };
        public HrtDataManager(DalamudPluginInterface pluginInterface)
        {
            string configDirName = pluginInterface.ConfigDirectory.FullName;
            //Set up files &folders
            try
            {
                if (!pluginInterface.ConfigDirectory.Exists)
                    pluginInterface.ConfigDirectory.Create();
            }
            catch (IOException ioe)
            {
                PluginLog.Error($"Could not create data directory\n{ioe}");
                Initialized = false;
                return;
            }
            RaidGRoupJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{RaidGroupJsonFileName}");
            if (!RaidGRoupJsonFile.Exists)
                RaidGRoupJsonFile.Create().Close();
            CharDBJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{CharDBJsonFileName}");
            if (!CharDBJsonFile.Exists)
                CharDBJsonFile.Create().Close();
            HrtGearDBJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{HrtGearDBJsonFileName}");
            if (!HrtGearDBJsonFile.Exists)
                HrtGearDBJsonFile.Create().Close();
            EtroGearDBJsonFile = new FileInfo($"{configDirName}{Path.DirectorySeparatorChar}{EtroGearDBJsonFileName}");
            if (!EtroGearDBJsonFile.Exists)
                EtroGearDBJsonFile.Create().Close();
            //Read files
            bool loadError = false;
            ModuleConfigurationManager = new(pluginInterface);
            loadError |= !TryRead(HrtGearDBJsonFile, out string hrtGearJson);
            loadError |= !TryRead(EtroGearDBJsonFile, out string etroGearJson);
            loadError |= !TryRead(CharDBJsonFile, out string charDBJson);
            loadError |= !TryRead(RaidGRoupJsonFile, out string RaidGRoupJson);
            if (!loadError)
            {
                GearDB = new(hrtGearJson, etroGearJson, JsonSettings);
                GearSetRefConv = new GearSetReferenceConverter(GearDB);
                CharacterDB = new(charDBJson, GearSetRefConv, JsonSettings);
                CharRefConv = new CharacterReferenceConverter(CharacterDB);
                JsonSettings.Converters.Add(CharRefConv);
                _Groups = JsonConvert.DeserializeObject<List<RaidGroup>>(RaidGRoupJson, JsonSettings) ?? new();
                JsonSettings.Converters.Remove(CharRefConv);
            }
            Initialized = !loadError;
        }
        internal static bool TryRead(FileInfo file, out string data)
        {
            data = "";
            using var reader = file.OpenText();
            try
            {
                data = reader.ReadToEnd();
                return true;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Could not load data file");
                return false;
            }
        }
        internal static bool TryWrite(FileInfo file, in string data)
        {
            using var writer = file.CreateText();
            try
            {
                writer.Write(data);
                return true;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Could not write data file");
                return false;
            }
        }
        public List<uint> GetWorldsWithCharacters()
        {
            if (!Initialized || CharacterDB is null)
                return new();
            return CharacterDB.GetUsedWorlds();
        }
        public List<string> GetCharacterNames(uint worldID)
        {
            if (!Initialized || CharacterDB is null)
                return new();
            return CharacterDB.GetCharactersList(worldID);
        }
        public bool CharacterExists(uint worldID, string name) =>
            CharacterDB?.Exists(worldID, name) ?? false;
        public bool GetManagedGearSet(ref GearSet gs, bool doNotWaitOnSaving = false)
        {
            if (!Initialized || GearDB is null)
                return false;
            while (Serializing)
            {
                if (doNotWaitOnSaving)
                    return false;
                Thread.Sleep(1);
            }
            return GearDB.AddOrGetSet(ref gs);
        }
        public bool GetManagedCharacter(ref Character c, bool doNotWaitOnSaving = false)
        {
            if (!Initialized || CharacterDB is null)
                return false;
            while (Serializing)
            {
                if (doNotWaitOnSaving)
                    return false;
                Thread.Sleep(1);
            }
            return CharacterDB.AddOrGetCharacter(ref c);
        }
        public bool RearrangeCharacter(uint oldWorld, string oldName, ref Character c, bool doNotWaitOnSaving = false)
        {
            bool hasError = false;
            if (!Initialized || CharacterDB is null || GearDB is null)
                return false;
            while (Serializing)
            {
                if (doNotWaitOnSaving)
                    return false;
                Thread.Sleep(1);
            }
            hasError |= CharacterDB.UpdateIndex(oldWorld, oldName, ref c);
            foreach (PlayableClass job in c)
            {
                string oldID = job.Gear.HrtID;
                job.Gear.UpdateID(c, job.Job);
                hasError |= GearDB.UpdateIndex(oldID, ref job.Gear);
            }
            return !hasError;
        }
        public bool RearrangeGearSet(string oldID, ref GearSet gs, bool doNotWaitOnSaving = false)
        {
            if (!Initialized || GearDB is null)
                return false;
            while (Serializing)
            {
                if (doNotWaitOnSaving)
                    return false;
                Thread.Sleep(1);
            }
            return GearDB.UpdateIndex(oldID, ref gs);
        }
        public void UpdateEtroSets(int maxAgeDays) => GearDB?.UpdateEtroSets(maxAgeDays);
        private string SerializeGroupData(CharacterReferenceConverter charRefCon)
        {
            JsonSettings.Converters.Add(charRefCon);
            string result = JsonConvert.SerializeObject(_Groups, JsonSettings);
            JsonSettings.Converters.Remove(charRefCon);
            return result;
        }
        public bool Save()
        {
            if (!Initialized || Saving || GearDB == null || CharacterDB == null
                || EtroGearDBJsonFile == null || HrtGearDBJsonFile == null
                || CharDBJsonFile == null || RaidGRoupJsonFile == null
                || GearSetRefConv == null || CharRefConv == null)
                return false;
            Saving = true;
            var time1 = DateTime.Now;
            //Serialize all data (functions are locked while this happens)
            Serializing = true;
            string characterData = CharacterDB.Serialize(GearSetRefConv, JsonSettings);
            (string hrtGearData, string etroGearData) = GearDB.Serialize(JsonSettings);
            string groupData = SerializeGroupData(CharRefConv);
            Serializing = false;
            //Write serialized data
            var time2 = DateTime.Now;
            bool hasError = !TryWrite(HrtGearDBJsonFile, hrtGearData);
            if (!hasError)
                hasError |= !TryWrite(EtroGearDBJsonFile, etroGearData);
            if (!hasError)
                hasError |= !TryWrite(CharDBJsonFile, characterData);
            if (!hasError)
                hasError |= !TryWrite(RaidGRoupJsonFile, groupData);
            Saving = false;
            var time3 = DateTime.Now;
            PluginLog.Debug($"Serializing time: {time2 - time1}");
            PluginLog.Debug($"IO time: {time3 - time2}");
            return !hasError;
        }
    }
}
