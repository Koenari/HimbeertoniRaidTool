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
        private readonly GearDB? GearDB;
        private readonly CharacterDB? CharacterDB;
        private List<RaidGroup>? _Groups;
        private readonly FileInfo RaidGRoupJsonFile;
        public List<RaidGroup> Groups => _Groups ?? new();
        internal ModuleConfigurationManager ModuleConfigurationManager { get; private set; }
        internal static JsonSerializerSettings JsonSerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
        };
        public HrtDataManager(DalamudPluginInterface pluginInterface)
        {
            var dir = pluginInterface.ConfigDirectory;
            RaidGRoupJsonFile = new FileInfo(dir.FullName + "\\RaidGroups.json");
            ModuleConfigurationManager = new(pluginInterface);
            try
            {
                if (!dir.Exists)
                    dir.Create();
                GearDB = new(dir);
                CharacterDB = new(dir, GearDB);

                if (!RaidGRoupJsonFile.Exists)
                    RaidGRoupJsonFile.Create().Close();
                var crc = new CharacterReferenceConverter(CharacterDB);
                JsonSerializerSettings.Converters.Add(crc);
                _Groups = JsonConvert.DeserializeObject<List<RaidGroup>>(
                    RaidGRoupJsonFile.OpenText().ReadToEnd(),
                    JsonSerializerSettings) ?? new();
                JsonSerializerSettings.Converters.Remove(crc);
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
        public List<string> GetCharacters(uint worldID)
        {
            if (!Initialized || CharacterDB is null)
                return new();
            return CharacterDB.GetCharactersList(worldID);
        }
        public bool CharacterExists(uint worldID, string name) =>
            CharacterDB?.Exists(worldID, name) ?? false;
        public bool GetManagedGearSet(ref GearSet gs)
        {
            if (!Initialized || GearDB is null)
                return false;
            while (Saving)
                Thread.Sleep(100);
            return GearDB.AddOrGetSet(ref gs);
        }
        public bool GetManagedCharacter(ref Character c)
        {
            if (!Initialized || CharacterDB is null)
                return false;
            while (Saving)
                Thread.Sleep(100);
            return CharacterDB.AddOrGetCharacter(ref c);
        }
        public bool RearrangeCharacter(uint oldWorld, string oldName, ref Character c)
        {
            bool hasError = false;
            if (!Initialized || CharacterDB is null || GearDB is null)
                return false;
            while (Saving)
                Thread.Sleep(100);
            hasError |= CharacterDB.UpdateIndex(oldWorld, oldName, ref c);
            for (int i = 0; i < c.Classes.Count; i++)
            {
                string oldID = c.Classes[i].Gear.HrtID;
                c.Classes[i].Gear.UpdateID(c, c.Classes[i].Job);
                hasError |= GearDB.UpdateIndex(oldID, ref c.Classes[i].Gear);
            }
            return !hasError;
        }
        public bool RearrangeGearSet(string oldID, ref GearSet gs)
        {
            if (!Initialized || GearDB is null)
                return false;
            while (Saving)
                Thread.Sleep(100);
            return GearDB.UpdateIndex(oldID, ref gs);
        }
        [Obsolete("Only used to convert from legacy config")]
        public void Fill(List<RaidGroup> rg)
        {
            _Groups = rg;
            for (int i = 0; i < _Groups.Count; i++)
            {
                for (int j = 0; j < _Groups[i].Players.Length; j++)
                {
                    for (int k = 0; k < _Groups[i].Players[j].Chars.Count; k++)
                    {
                        Character c = _Groups[i].Players[j].Chars[k];
                        GetManagedCharacter(ref c);
                        _Groups[i].Players[j].Chars[k] = c;
                        for (int l = 0; l < c.Classes.Count; l++)
                        {
                            PlayableClass pc = c.Classes[l];
                            pc.Gear.Name = "HrtCurrent";
                            pc.Gear.HrtID = GearSet.GenerateID(c, pc.Job, pc.Gear);
                            pc.BIS.ManagedBy = GearSetManager.Etro;
                            pc.ManageGear();
                        }
                    }
                }
            }
        }
        public void UpdateEtroSets(int maxAgeDays) => GearDB?.UpdateEtroSets(maxAgeDays);
        public bool Save()
        {
            if (!Initialized || Saving || GearDB == null || CharacterDB == null)
                return false;
            Saving = true;
            bool hasError = false;
            hasError |= GearDB.Save();
            if (hasError)
                return !hasError;
            hasError |= CharacterDB.Save(GearDB!);
            if (hasError)
                return !hasError;
            var crc = new CharacterReferenceConverter(CharacterDB!);
            JsonSerializerSettings.Converters.Add(crc);
            StreamWriter? writer = null;
            try
            {
                writer = RaidGRoupJsonFile.CreateText();
                var serializer = JsonSerializer.Create(JsonSerializerSettings);
                serializer.Serialize(writer, _Groups);
            }
            catch (Exception e)
            {
                PluginLog.Error("Could not write gear data\n{0}", e);
                hasError = true;
            }
            finally
            {
                writer?.Dispose();
            }
            JsonSerializerSettings.Converters.Remove(crc);
            Saving = false;
            return !hasError;
        }
    }
}
