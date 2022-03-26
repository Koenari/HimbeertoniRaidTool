using Dalamud.Logging;
using Newtonsoft.Json;
using System;
using System.Net;

namespace HimbeertoniRaidTool.Data
{
    public static class EtroConnector
    {
        public static readonly string ApiBaseUrl = "https://etro.gg/api/";
        public static readonly string WebBaseUrl = "https://etro.gg/";
        public static string GearsetApiBaseUrl => ApiBaseUrl + "gearsets/";
        public static string GearsetWebBaseUrl => WebBaseUrl + "gearsets/";
        private readonly static WebHeaderCollection Headers;
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
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore

        };
        static EtroConnector()
        {
            Headers = new();
            Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");


        }

        private static string? MakeWebRequest(string URL)
        {
            WebClient client = new();
            client.Headers = Headers;
            try
            {
                while (client.IsBusy)
                {
                    PluginLog.LogDebug("WebClient Busy");
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

        public static bool GetGearSet(GearSet set)
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
            return true;
        }

    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Benennungsstile", Justification = "class is maintained by 3rd party")]
    class EtroGearSet
    {

        public string? id { get; set; }
        public string? jobAbbrev { get; set; }
        public string? name { get; set; }
        public DateTime lastUpdate { get; set; }
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
}
