using System.Net;
using System.Net.Http;
using HimbeertoniRaidTool.Plugin.Connectors.Utils;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class XivGearAppConnector(TaskManager taskManager)
    : WebConnector(new RateLimit(5, TimeSpan.FromSeconds(10))), IReadOnlyGearConnector
{
    private const string WEB_BASE_URL = "https://xivgear.app/";
    private const string API_BASE_URL = "https://api.xivgear.app/";
    private const string GEAR_API_BASE_URL = API_BASE_URL + "shortlink/";

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
    public void RequestGearSetUpdate(GearSet set, Action<HrtUiMessage>? messageCallback = null,
                                     string taskName = "Gearset Update")
    {
        messageCallback ??= _ => { };
        taskManager.RegisterTask(new HrtTask(() => UpdateGearSet(set), messageCallback, taskName));
    }
    public HrtUiMessage UpdateGearSet(GearSet set)
    {
        HrtUiMessage errorMessage = new(string.Format(GeneralLoc.XivGearAppConnector_GetGearSet_Error, set.Name),
                                        HrtUiMessageType.Failure);
        if (set.ExternalId.Equals(""))
            return errorMessage;
        errorMessage.Message = $"{errorMessage.Message} ({set.ExternalId})";
        HttpResponseMessage? httpResponse = MakeWebRequest(GEAR_API_BASE_URL + set.ExternalId);
        if (httpResponse == null)
            return errorMessage;
        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
        {
            set.ManagedBy = GearSetManager.Hrt;
            set.ExternalId = string.Empty;
            return new HrtUiMessage($"Set {set.Name} is not available at {WEB_BASE_URL}. It is now managed locally",
                                    HrtUiMessageType.Warning);

        }
        var readTask = httpResponse.Content.ReadAsStringAsync();
        readTask.Wait();
        var xivGearSheet = JsonConvert.DeserializeObject<XivGearSheet>(readTask.Result, JsonSettings);
        XivGearSet? xivSet = xivGearSheet?.sets[0];
        if (xivSet == null)
            return errorMessage;
        set.Name = xivSet.name ?? "";
        set.TimeStamp = DateTime.UtcNow;
        set.LastExternalFetchDate = DateTime.UtcNow;
        HrtUiMessage successMessage = new(string.Format(GeneralLoc.EtroConnector_GetGearSet_Success, set.Name),
                                          HrtUiMessageType.Success);
        FillItem(xivSet.items["Weapon"], GearSetSlot.MainHand);
        FillItem(xivSet.items["Head"], GearSetSlot.Head);
        FillItem(xivSet.items["Body"], GearSetSlot.Body);
        FillItem(xivSet.items["Hand"], GearSetSlot.Hands);
        FillItem(xivSet.items["Legs"], GearSetSlot.Legs);
        FillItem(xivSet.items["Feet"], GearSetSlot.Feet);
        FillItem(xivSet.items["Ears"], GearSetSlot.Ear);
        FillItem(xivSet.items["Neck"], GearSetSlot.Neck);
        FillItem(xivSet.items["Wrist"], GearSetSlot.Wrist);
        FillItem(xivSet.items["RingRight"], GearSetSlot.Ring1);
        FillItem(xivSet.items["RingLeft"], GearSetSlot.Ring2);
        //FillItem(xivSet.offHand, GearSetSlot.OffHand);
        return successMessage;

        void FillItem(XivItem item, GearSetSlot slot)
        {
            set[slot] = new GearItem(item.id)
            {
                IsHq = ServiceManager.ItemInfo.CanBeCrafted(item.id),
            };
            foreach (XivItem materia in item.materia)
            {
                //set[slot].AddMateria(new HrtMateria(materia.id));
            }
        }
    }


    private class XivGearSheet
    {
        public string name;
        public List<XivGearSet> sets;
        public string job;
        public string description;
    }

    private class XivGearSet
    {
        public string name;
        public Dictionary<string, XivItem> items;
        public uint food;
    }

    private class XivItem
    {
        public uint id;
        public List<XivItem> materia;

    }
}