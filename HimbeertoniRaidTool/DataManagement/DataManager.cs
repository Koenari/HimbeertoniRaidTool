using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Dalamud.Logging;
using Dalamud.Plugin;
using HimbeertoniRaidTool.Data;
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
        private readonly FileInfo HrtGearDBJsonFile;
        private const string EtroGearDBJsonFileName = "EtroGearDB.json";
        private readonly FileInfo EtroGearDBJsonFile;
        private const string CharDBJsonFileName = "CharacterDB.json";
        private readonly FileInfo CharDBJsonFile;
        private const string RaidGroupJsonFileName = "RaidGroups.json";
        private readonly FileInfo RaidGRoupJsonFile;
        //Converters
        private readonly GearSetReferenceConverter GearSetRefConv;
        private readonly CharacterReferenceConverter CharRefConv;
        public List<RaidGroup> Groups => _Groups ?? new();
        internal ModuleConfigurationManager ModuleConfigurationManager { get; private set; }
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
            ModuleConfigurationManager = new(pluginInterface);
            try
            {
                var hrtGearReader = HrtGearDBJsonFile.OpenText();
                var etroGearReader = EtroGearDBJsonFile.OpenText();
                GearDB = new(hrtGearReader.ReadToEnd(), etroGearReader.ReadToEnd(), JsonSettings);
                hrtGearReader.Close();
                etroGearReader.Close();
                GearSetRefConv = new GearSetReferenceConverter(GearDB);
                var charDBReader = CharDBJsonFile.OpenText();
                CharacterDB = new(charDBReader.ReadToEnd(), GearSetRefConv, JsonSettings);
                charDBReader.Close();
                CharRefConv = new CharacterReferenceConverter(CharacterDB);
                JsonSettings.Converters.Add(CharRefConv);
                var raidGroupReader = RaidGRoupJsonFile.OpenText();
                _Groups = JsonConvert.DeserializeObject<List<RaidGroup>>(
                    raidGroupReader.ReadToEnd(), JsonSettings) ?? new();
                raidGroupReader.Close();
                JsonSettings.Converters.Remove(CharRefConv);
                Initialized = true;
            }
            catch (Exception e)
            {
                PluginLog.Error("Could not load data files\n{0}", e);
                Initialized = false;
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
        private string SerializeGroupData()
        {
            JsonSettings.Converters.Add(CharRefConv);
            string result = JsonConvert.SerializeObject(_Groups, JsonSettings);
            JsonSettings.Converters.Remove(CharRefConv);
            return result;
        }
        public bool Save()
        {
            if (!Initialized || Saving || GearDB == null || CharacterDB == null)
                return false;
            Saving = true;
            var time1 = DateTime.Now;
            //Serialize all data (functions are locked while this happens)
            Serializing = true;
            string characterData = CharacterDB.Serialize(GearSetRefConv, JsonSettings);
            (string hrtGearData, string etroGearData) = GearDB.Serialize(JsonSettings);
            string groupData = SerializeGroupData();
            Serializing = false;
            //Write serialized data
            var time2 = DateTime.Now;
            bool hasError = false;
            StreamWriter? hrtWriter = null;
            StreamWriter? etroWriter = null;
            StreamWriter? characterWriter = null;
            StreamWriter? groupWriter = null;
            try
            {
                hrtWriter = HrtGearDBJsonFile.CreateText();
                hrtWriter.Write(hrtGearData);
                etroWriter = EtroGearDBJsonFile.CreateText();
                etroWriter.Write(etroGearData);
                characterWriter = CharDBJsonFile.CreateText();
                characterWriter.Write(characterData);
                groupWriter = RaidGRoupJsonFile.CreateText();
                groupWriter.Write(groupData);
            }
            catch (Exception e)
            {
                PluginLog.Error("Could not write data file\n{0}", e);
                hasError = true;
            }
            finally
            {
                hrtWriter?.Close();
                etroWriter?.Close();
                characterWriter?.Close();
                groupWriter?.Close();
            }
            Saving = false;
            var time3 = DateTime.Now;
            PluginLog.Debug($"Serializing time: {time2 - time1}");
            PluginLog.Debug($"IO time: {time3 - time2}");
            return !hasError;
        }
    }
}
