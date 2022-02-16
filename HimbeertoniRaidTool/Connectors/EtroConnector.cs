using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace HimbeertoniRaidTool.Connectors
{
    public static class EtroConnector
    {
        private const string BaseUri = "https://etro.gg/api/";
        private static string EquipmentBaseUri { get => (BaseUri + "equipment/"); }
        private static string GearsetBaseUri { get => (BaseUri + "gearsets/"); }
        //private static WebClient Client;
        private static WebHeaderCollection Headers;
        private static JsonSerializerSettings jsonSettings => new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.Default,
            FloatParseHandling = FloatParseHandling.Double,
            FloatFormatHandling = FloatFormatHandling.DefaultValue,
            DateParseHandling = DateParseHandling.DateTime,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore

        };
        static EtroConnector()
        {
            Headers = new WebHeaderCollection();
            Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");


        }

        private static string? MakeWebRequest(string URL)
        {
            WebClient client = new WebClient();
            client.Headers = Headers;
            try
            {
                PluginLog.LogDebug(client.IsBusy.ToString());
                while (client.IsBusy)
                {
                    PluginLog.LogDebug("WEbClient Busy");
                    System.Threading.Thread.Sleep(1000);
                }
                return client.DownloadString(URL);
                
            }
            catch (Exception e)
            {
                PluginLog.LogError(e.Message);
                return null;
            }
        }
        public static bool GetEtroGearStats(GearItem item)
        {
            if (item.ID < 1)
                return false;
            EtroGearItem? etroResponse;

            string? jsonResponse = MakeWebRequest(EquipmentBaseUri + item.ID);
            if (jsonResponse == null)
                return false;
            etroResponse = JsonConvert.DeserializeObject<EtroGearItem>(jsonResponse, jsonSettings);

            
            if (etroResponse == null)
                return false;
            item.Name = etroResponse.name == null ? "" : etroResponse.name;
            item.Description = etroResponse.description == null ? "" : etroResponse.description;
            item.ItemLevel = etroResponse.itemLevel;
            return true;
        }

        public static bool GetGearSet(GearSet set)
        {
            if (set.EtroID.Equals(""))
                return false;
            EtroGearSet? etroSet;
            string? jsonResponse = MakeWebRequest(GearsetBaseUri + set.EtroID);
            if (jsonResponse == null)
                return false;
            etroSet = JsonConvert.DeserializeObject<EtroGearSet>(jsonResponse, jsonSettings);
            if (etroSet == null)
                return false;
            set.MainHand = new(etroSet.weapon);
            set.Head = new(etroSet.head);
            set.Head = new(etroSet.head);
            set.Body = new(etroSet.body);
            set.Hands = new(etroSet.hands);
            set.Legs = new(etroSet.legs);
            set.Feet = new(etroSet.feet);
            set.Ear = new(etroSet.ears);
            set.Neck = new(etroSet.neck);
            set.Wrist = new(etroSet.wrists);
            set.Ring1 = new(etroSet.fingerL);
            set.Ring2 = new(etroSet.fingerR);
            set.OffHand = new(etroSet.offHand);
            set.FillStats();
            return true;
        }

    }
    class EtroGearSet
    {
        public string? id { get; set; }
        public string? jobAbbrev { get; set; }
        public string? jobIconPath { get; set; }
        public string? clanName { get; set; }
        public bool isOwner { get; set; }
        public string? name { get; set; }
        public DateTime lastUpdate { get; set; }
        public uint? minItemLevel { get; set; }
        public uint maxItemLevel { get; set; }
        public uint minMateriaTier { get; set; }
        public uint maxMateriaTier { get; set; }
        //public Materia materia { get; set; }
        public List<TotalParam>? totalParams { get; set; }
        //public object buffs { get; set; }
        //public object relics { get; set; }
        public string? patch { get; set; }
        public uint job { get; set; }
        public uint clan { get; set; }
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
        public uint food { get; set; }
        public uint medicine { get; set; }
    }
    class EtroGearItem
    {
        public uint id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public uint param0 { get; set; }
        public uint param1 { get; set; }
        public uint param2 { get; set; }
        public uint param3 { get; set; }
        public object? param4 { get; set; }
        public object? param5 { get; set; }
        public uint param0Value { get; set; }
        public uint param1Value { get; set; }
        public uint param2Value { get; set; }
        public uint param3Value { get; set; }
        public uint param4Value { get; set; }
        public uint param5Value { get; set; }
        public MaxParams? maxParams { get; set; }
        public bool advancedMelding { get; set; }
        public uint block { get; set; }
        public uint blockRate { get; set; }
        public bool canBeHq { get; set; }
        public uint damageMag { get; set; }
        public uint damagePhys { get; set; }
        public uint defenseMag { get; set; }
        public uint defensePhys { get; set; }
        public uint delay { get; set; }
        public uint iconId { get; set; }
        public string? iconPath { get; set; }
        public int itemLevel { get; set; }
        public object? itemSpecialBonus { get; set; }
        public uint itemSpecialBonusParam { get; set; }
        public uint level { get; set; }
        public uint materiaSlotCount { get; set; }
        public uint materializeType { get; set; }
        public bool PVP { get; set; }
        public uint rarity { get; set; }
        public uint slotCategory { get; set; }
        public bool unique { get; set; }
        public bool untradable { get; set; }
        public bool weapon { get; set; }
        public bool canCustomize { get; set; }
        public string? slotName { get; set; }
        public string? jobName { get; set; }
        public uint itemUICategory { get; set; }
        public uint jobCategory { get; set; }
    }
    class MaxParams
    {
        public uint _6 { get; set; }
        public uint _10 { get; set; }
        public uint _11 { get; set; }
        public uint _19 { get; set; }
        public uint _22 { get; set; }
        public uint _27 { get; set; }
        public uint _44 { get; set; }
        public uint _45 { get; set; }
        public uint _46 { get; set; }
        public uint _70 { get; set; }
        public uint _71 { get; set; }
        public uint _72 { get; set; }
        public uint _73 { get; set; }
    }
    class TotalParam
    {
        public uint? id { get; set; }
        public string? name { get; set; }
        public decimal value { get; set; }
        public string? units { get; set; }
    }
}
