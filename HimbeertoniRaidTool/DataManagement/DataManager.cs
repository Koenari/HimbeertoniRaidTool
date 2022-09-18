using System.Collections.Generic;
using System.IO;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.DataManagement
{
    public static class DataManager
    {
        private static bool Initialized;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal static GearDB GearDB;
        internal static CharacterDB CharacterDB;
        private static List<RaidGroup> _Groups;
        private static FileInfo RaidGRoupJsonFile;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static List<RaidGroup> Groups => _Groups;
        internal static JsonSerializerSettings JsonSerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static void Init(bool reset = false)
        {
            var dir = Services.PluginInterface.ConfigDirectory;
            if (!dir.Exists)
                dir.Create();

            GearDB = new(dir, reset);
            CharacterDB = new(dir, reset);
            RaidGRoupJsonFile = new FileInfo(dir.FullName + "\\RaidGroups.json");
            if (!RaidGRoupJsonFile.Exists)
                RaidGRoupJsonFile.Create().Close();
            var crc = new CharacterReferenceConverter();
            JsonSerializerSettings.Converters.Add(crc);
            _Groups = JsonConvert.DeserializeObject<List<RaidGroup>>(
                reset ? "" : RaidGRoupJsonFile.OpenText().ReadToEnd(),
                JsonSerializerSettings) ?? new();
            JsonSerializerSettings.Converters.Remove(crc);
            Initialized = true;
        }
        public static List<uint> GetWorldsWithCharacters()
        {
            if (Initialized)
                return CharacterDB.GetUsedWorlds();
            return new();
        }
        public static List<string> GetCharacters(uint worldID)
        {
            if (Initialized)
                return CharacterDB.GetCharactersList(worldID);
            return new();
        }
        public static bool CharacterExists(uint worldID, string name) =>
            CharacterDB.Exists(worldID, name);
        public static void GetManagedGearSet(ref GearSet gs)
        {
            if (Initialized)
                GearDB.AddOrGetSet(ref gs);
        }
        public static void GetManagedCharacter(ref Character c)
        {
            if (Initialized)
                CharacterDB.AddOrGetCharacter(ref c);
        }
        public static void RearrangeCharacter(uint oldWorld, string oldName, ref Character c)
        {
            if (!Initialized)
                return;
            CharacterDB.UpdateIndex(oldWorld, oldName, ref c);
            for (int i = 0; i < c.Classes.Count; i++)
            {
                string oldID = c.Classes[i].Gear.HrtID;
                c.Classes[i].Gear.UpdateID(c, c.Classes[i].Job);
                RearrangeGearSet(oldID, ref c.Classes[i].Gear);
            }
        }
        public static void RearrangeGearSet(string oldID, ref GearSet gs)
        {
            GearDB.UpdateIndex(oldID, ref gs);
        }
        public static void Fill(List<RaidGroup> rg)
        {
            Init(true);
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
        public static void Save()
        {
            if (!Initialized)
                return;
            GearDB.Save();
            CharacterDB.Save();
            //TODO: RaidGroups
            var crc = new CharacterReferenceConverter();
            JsonSerializerSettings.Converters.Add(crc);
            File.WriteAllText(RaidGRoupJsonFile.FullName,
                JsonConvert.SerializeObject(_Groups, JsonSerializerSettings));
            JsonSerializerSettings.Converters.Remove(crc);

        }
    }
}
