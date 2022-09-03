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
        public static bool UpdateGear(Character c)
        {
            if (c.LodestoneID == 0)
            {
                if (c.HomeWorld != null)
                    c.LodestoneID = GetLodestoneID(c.Name, c.HomeWorld);
                else
                    return false;
            }
            if (c.LodestoneID == 0)
                return false;
            string? jsonResponse = BaseConnector.MakeWebRequest(CharacterApiBaseURL + $"/{c.LodestoneID}");
            LodestoneCharacter? lsc = JsonConvert.DeserializeObject<ChnaracterResponse>(jsonResponse ?? "")?.Character;
            if (lsc is null)
                return false;
            if (lsc.Name != c.Name)
                return false;
            if (lsc.ActiveClassJob is null || lsc.GearSet?.Gear is null)
                return false;

            var job = c.GetClass((Job)lsc.GearSet.JobID);
            job.Level = lsc.GearSet.Level;
            c.TribeID = (uint)lsc.Tribe;
            var lscGear = lsc.GearSet.Gear;
            FillItem(lscGear.MainHand, GearSetSlot.MainHand);
            FillItem(lscGear.Head, GearSetSlot.Head);
            FillItem(lscGear.Body, GearSetSlot.Body);
            FillItem(lscGear.Hands, GearSetSlot.Hands);
            FillItem(lscGear.Legs, GearSetSlot.Legs);
            FillItem(lscGear.Feet, GearSetSlot.Feet);
            FillItem(lscGear.OffHand, GearSetSlot.OffHand);
            FillItem(lscGear.Earrings, GearSetSlot.Ear);
            FillItem(lscGear.Necklace, GearSetSlot.Neck);
            FillItem(lscGear.Bracelets, GearSetSlot.Wrist);
            FillItem(lscGear.Ring1, GearSetSlot.Ring1);
            FillItem(lscGear.Ring2, GearSetSlot.Ring2);

            return true;
            void FillItem(LodestoneGearItem? item, GearSetSlot slot)
            {
                if (item == null)
                {
                    job.Gear[slot] = new();
                    return;
                }
                job.Gear[slot] = new(item.ID);
                foreach (var mat in item.Materia)
                {
                    //Todo: figure out Materia
                    //job.Gear[slot].Materia.Add(new(mat.))
                }

            }
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
            public ActiveClassJob? ActiveClassJob { get; set; }
            public LodestoneGearSet? GearSet { get; set; }
            public int ID { get; set; }
            public string? Name { get; set; }
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
            public byte JobID { get; set; }
            public int Level { get; set; }
        }
        public class Gear
        {
            public LodestoneGearItem? Body { get; set; }
            public LodestoneGearItem? Bracelets { get; set; }
            public LodestoneGearItem? Earrings { get; set; }
            public LodestoneGearItem? Feet { get; set; }
            public LodestoneGearItem? Hands { get; set; }
            public LodestoneGearItem? Head { get; set; }
            public LodestoneGearItem? Legs { get; set; }
            public LodestoneGearItem? MainHand { get; set; }
            public LodestoneGearItem? OffHand { get; set; }
            public LodestoneGearItem? Necklace { get; set; }
            public LodestoneGearItem? Ring1 { get; set; }
            public LodestoneGearItem? Ring2 { get; set; }
        }
        public class LodestoneGearItem
        {
            public uint ID { get; set; }
            public List<uint> Materia { get; set; } = new();
        }

    }
}
