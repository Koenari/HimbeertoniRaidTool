using System;
using System.Collections.Generic;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Connectors
{
    internal class EtroConnector : WebConnector
    {
        public static string ApiBaseUrl => "https://etro.gg/api/";
        public static string WebBaseUrl => "https://etro.gg/";
        public static string GearsetApiBaseUrl => ApiBaseUrl + "gearsets/";
        public static string GearsetWebBaseUrl => WebBaseUrl + "gearset/";
        public static string MateriaApiBaseUrl => ApiBaseUrl + "materia/";
        private static readonly Dictionary<uint, (MateriaCategory, byte)> MateriaCache = new();
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
        internal EtroConnector() : base(new(4, new(0, 0, 30)), new(0, 5, 0))
        {
            if (MateriaCache.Count == 0)
            {
                string? jsonResponse = MakeWebRequest(MateriaApiBaseUrl);
                EtroMateria[]? matList = JsonConvert.DeserializeObject<EtroMateria[]>(jsonResponse ?? "", JsonSettings);
                if (matList != null)
                    foreach (var mat in matList)
                        for (byte i = 0; i < mat.tiers.Length; i++)
                            MateriaCache.Add(mat.tiers[i].id, ((MateriaCategory)mat.id, i));
            }
        }


        public bool GetGearSet(GearSet set)
        {
            if (set.EtroID.Equals(""))
                return false;
            EtroGearSet? etroSet;
            string? jsonResponse = MakeWebRequest(GearsetApiBaseUrl + set.EtroID);
            if (jsonResponse == null)
                return false;
            etroSet = JsonConvert.DeserializeObject<EtroGearSet>(jsonResponse, JsonSettings);
            if (etroSet == null)
                return false;
            set.Name = etroSet.name ?? "";
            set.TimeStamp = etroSet.lastUpdate;
            set.EtroFetchDate = DateTime.UtcNow;
            FillItem(etroSet.weapon, GearSetSlot.MainHand);
            FillItem(etroSet.head, GearSetSlot.Head);
            FillItem(etroSet.body, GearSetSlot.Body);
            FillItem(etroSet.hands, GearSetSlot.Hands);
            FillItem(etroSet.legs, GearSetSlot.Legs);
            FillItem(etroSet.feet, GearSetSlot.Feet);
            FillItem(etroSet.ears, GearSetSlot.Ear);
            FillItem(etroSet.neck, GearSetSlot.Neck);
            FillItem(etroSet.wrists, GearSetSlot.Wrist);
            FillItem(etroSet.fingerL, GearSetSlot.Ring1);
            FillItem(etroSet.fingerR, GearSetSlot.Ring2);
            FillItem(etroSet.offHand, GearSetSlot.OffHand);
            return true;

            void FillItem(uint id, GearSetSlot slot)
            {
                set[slot] = new(id);
                if (set[slot].Source == GearSource.Crafted)
                    set[slot].IsHq = true;
                string idString = id.ToString() + (slot == GearSetSlot.Ring1 ? "L" : slot == GearSetSlot.Ring2 ? "R" : "");
                if (etroSet!.materia?.TryGetValue(idString, out Dictionary<uint, uint?>? materia) ?? false)
                    foreach (uint? matId in materia.Values)
                        if (matId.HasValue)
                            set[slot].Materia.Add(new(MateriaCache.GetValueOrDefault<uint, (MateriaCategory, byte)>(matId.Value, (0, 0))));
            }
        }
        private class EtroGearSet
        {

            public string? id { get; set; }
            public string? jobAbbrev { get; set; }
            public string? name { get; set; }
            public DateTime lastUpdate { get; set; }
            public Dictionary<string, Dictionary<uint, uint?>>? materia { get; set; }
            public uint weapon { get; set; }
            public uint head { get; set; }
            public uint body { get; set; }
            public uint hands { get; set; }
            public uint legs { get; set; }
            public uint feet { get; set; }
            public uint offHand { get; set; }
            public uint ears { get; set; }
            public uint neck { get; set; }
            public uint wrists { get; set; }
            public uint fingerL { get; set; }
            public uint fingerR { get; set; }
        }
        private class EtroMateriaTier
        {
            public ushort id;
        }



        private class EtroMateria
        {
            public uint id;
            public EtroMateriaTier tier1 { get => tiers[0]; set => tiers[0] = value; }
            public EtroMateriaTier tier2 { get => tiers[1]; set => tiers[1] = value; }
            public EtroMateriaTier tier3 { get => tiers[2]; set => tiers[2] = value; }
            public EtroMateriaTier tier4 { get => tiers[3]; set => tiers[3] = value; }
            public EtroMateriaTier tier5 { get => tiers[4]; set => tiers[4] = value; }
            public EtroMateriaTier tier6 { get => tiers[5]; set => tiers[5] = value; }
            public EtroMateriaTier tier7 { get => tiers[6]; set => tiers[6] = value; }
            public EtroMateriaTier tier8 { get => tiers[7]; set => tiers[7] = value; }
            public EtroMateriaTier tier9 { get => tiers[8]; set => tiers[8] = value; }
            public EtroMateriaTier tier10 { get => tiers[9]; set => tiers[9] = value; }
            [JsonIgnore]
            public EtroMateriaTier[] tiers = new EtroMateriaTier[10];
        }
    }
}
