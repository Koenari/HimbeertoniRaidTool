using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Connectors
{
    internal static class XIVAPIConnector
    {
        private static readonly string BaseURL = "https://xivapi.com";
        private static readonly string CharacterApiBaseURL = BaseURL + "/character";
        private static readonly string CharacterSearchApiBaseURL = CharacterApiBaseURL + "/search";
        private static JsonSerializerSettings JsonSettings => new()
        {
            StringEscapeHandling = StringEscapeHandling.Default,
            FloatParseHandling = FloatParseHandling.Double,
            FloatFormatHandling = FloatFormatHandling.DefaultValue,
            DateParseHandling = DateParseHandling.DateTime,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

        };
        public static int GetLodestoneID(string name, World w)
        {
            string? jsonResponse = BaseConnector.MakeWebRequest(CharacterSearchApiBaseURL + $"?name={name}&server={w.Name.RawString}");
            ChnaracterSearchResult? response = JsonConvert.DeserializeObject<ChnaracterSearchResponse>(jsonResponse ?? "", JsonSettings)?.Results?[0];
            return response?.ID ?? 0;
        }
        public static void UpdateGear(Character c)
        {
            if (c.LodestoneID == 0)
            {
                if (c.HomeWorld != null)
                    c.LodestoneID = GetLodestoneID(c.Name, c.HomeWorld);
                else
                    return;
            }
            string? jsonResponse = BaseConnector.MakeWebRequest(CharacterApiBaseURL + $"/{c.LodestoneID}");
            LodestoneCharacter? lsc = JsonConvert.DeserializeObject<ChnaracterResponse>(jsonResponse ?? "")?.Character;
            if (lsc is null)
                return;

        }
        private class ChnaracterSearchResult
        {
            public int ID { get; set; }
            public string? Name { get; set; }
            public string? Server { get; set; }
        }

        private class ChnaracterSearchResponse
        {
            public List<ChnaracterSearchResult>? Results { get; set; }
        }
        private class ChnaracterResponse
        {
            public LodestoneCharacter? Character { get; set; }
        }
        private class LodestoneCharacter
        {
            public ActiveClassJob ActiveClassJob { get; set; }
            public LodestoneGearSet GearSet { get; set; }
            public int ID { get; set; }
            public string Name { get; set; }
            public int Race { get; set; }
            public int Tribe { get; set; }
        }
        private class ActiveClassJob
        {
            public int JobID { get; set; }
        }
        public class LodestoneGearSet
        {
            public int ClassID { get; set; }
            public Gear? Gear { get; set; }
            public int JobID { get; set; }
            public int Level { get; set; }
        }
        public class Gear
        {
            public LodestoenGearItem? Body { get; set; }
            public LodestoenGearItem? Bracelets { get; set; }
            public LodestoenGearItem? Earrings { get; set; }
            public LodestoenGearItem? Feet { get; set; }
            public LodestoenGearItem? Hands { get; set; }
            public LodestoenGearItem? Head { get; set; }
            public LodestoenGearItem? Legs { get; set; }
            public LodestoenGearItem? MainHand { get; set; }
            public LodestoenGearItem? Necklace { get; set; }
            public LodestoenGearItem? Ring1 { get; set; }
            public LodestoenGearItem? Ring2 { get; set; }
        }
        public class LodestoenGearItem
        {
            public int ID { get; set; }
            public List<int>? Materia { get; set; }
        }

    }
