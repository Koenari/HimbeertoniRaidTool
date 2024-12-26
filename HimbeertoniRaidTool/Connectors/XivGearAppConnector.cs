using System.Net;
using System.Net.Http;
using HimbeertoniRaidTool.Plugin.Connectors.Utils;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class XivGearAppConnector(HrtDataManager hrtDataManager, TaskManager taskManager, ILogger logger)
    : WebConnector(logger, new RateLimit(5, TimeSpan.FromSeconds(10))), IReadOnlyGearConnector
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


    public bool BelongsToThisService(string url) => url.StartsWith(WEB_BASE_URL) || url.StartsWith(API_BASE_URL);
    public string GetId(string url) => url.Split("%7C")[^1];


    public bool IsSheet(string id)
    {
        var httpResponse = MakeWebRequest(GEAR_API_BASE_URL + id);
        if (httpResponse is null || !httpResponse.IsSuccessStatusCode) return false;
        var readTask = httpResponse.Content.ReadAsStringAsync();
        readTask.Wait();
        return IsSheetInternal(readTask.Result);
    }
    private static bool IsSheetInternal(string content)
    {
        var sheet = JsonConvert.DeserializeObject<XivGearSheet>(content);
        return sheet?.sets?.Count > 0;
    }

    public List<string> GetSetNames(string id)
    {
        var httpResponse = MakeWebRequest(GEAR_API_BASE_URL + id);
        if (httpResponse is null || !httpResponse.IsSuccessStatusCode) return [];
        var readTask = httpResponse.Content.ReadAsStringAsync();
        readTask.Wait();
        var sheet = JsonConvert.DeserializeObject<XivGearSheet>(readTask.Result);
        if (sheet?.sets != null)
            return sheet.sets?.ConvertAll(set => set.name ?? "") ?? [];
        var set = JsonConvert.DeserializeObject<XivGearSheet>(readTask.Result);
        return set is null ? [] : [set.name ?? ""];
    }

    public void RequestGearSetUpdate(GearSet set, Action<HrtUiMessage>? messageCallback = null,
                                     string taskName = "Gearset Update")
    {
        messageCallback ??= _ => { };
        taskManager.RegisterTask(new HrtTask<HrtUiMessage>(() => UpdateGearSet(set), messageCallback, taskName));
    }
    public HrtUiMessage UpdateGearSet(GearSet set)
    {
        HrtUiMessage errorMessage = new(string.Format(GeneralLoc.XivGearAppConnector_GetGearSet_Error, set.Name),
                                        HrtUiMessageType.Failure);
        if (set.ExternalId.Equals(""))
            return errorMessage;
        errorMessage.Message = $"{errorMessage.Message} ({set.ExternalId})";
        var httpResponse = MakeWebRequest(GEAR_API_BASE_URL + set.ExternalId);
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
        XivGearSet? xivSet;
        if (IsSheetInternal(readTask.Result))
        {
            var xivGearSheet = JsonConvert.DeserializeObject<XivGearSheet>(readTask.Result, JsonSettings);
            xivSet = xivGearSheet?.sets?[set.ExternalIdx];
        }
        else
        {
            xivSet = JsonConvert.DeserializeObject<XivGearSet>(readTask.Result, JsonSettings);
        }
        if (xivSet == null)
            return errorMessage;
        set.Name = xivSet.name ?? "";
        set.TimeStamp = DateTime.UtcNow;
        set.LastExternalFetchDate = DateTime.UtcNow;
        HrtUiMessage successMessage = new(string.Format(GeneralLoc.XivGearAppConnector_GetGearSet_Success, set.Name),
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
            if (item.id < 0) return;
            set[slot] = new GearItem((uint)item.id)
            {
                IsHq = new Item((uint)item.id).CanBeHq,
            };
            foreach (var materia in item.materia)
            {
                if (materia.id < 0) continue;
                set[slot].AddMateria(new MateriaItem((uint)materia.id));
            }
        }
    }

    internal HrtUiMessage UpdateAllSets(bool updateAll, int maxAgeInDays)
    {
        var oldestValid = DateTime.UtcNow - new TimeSpan(maxAgeInDays, 0, 0, 0);
        int totalCount = 0;
        int updateCount = 0;
        foreach (var gearSet in hrtDataManager.GearDb.GetValues()
                                              .Where(set => set.ManagedBy == GearSetManager.XivGear))
        {
            totalCount++;
            if (gearSet.IsEmpty || gearSet.LastExternalFetchDate < oldestValid && updateAll)
            {
                var message = UpdateGearSet(gearSet);
                if (message.MessageType is HrtUiMessageType.Error or HrtUiMessageType.Failure)
                    Logger.Error(message.Message);
                if (message.MessageType is HrtUiMessageType.Warning)
                    Logger.Warning(message.Message);
                updateCount++;
            }
        }

        return new HrtUiMessage(
            string.Format(GeneralLoc.Connector_UpdateAllSets_Finished, updateCount, totalCount, "XivGear"));

    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    // ReSharper disable InconsistentNaming
    // ReSharper disable CollectionNeverUpdated.Local
    // ReSharper disable ClassNeverInstantiated.Local
    private class XivGearSheet
    {
        public string? name;
        public List<XivGearSet>? sets;
        public string job;
        public string description;
    }

    private class XivGearSet
    {
        public string? name;
        public string job;
        public Dictionary<string, XivItem> items;
        public uint food;
    }

    private class XivItem
    {
        public int id;
        public List<XivItem> materia;
    }
    // ReSharper restore ClassNeverInstantiated.Local
    // ReSharper restore InconsistentNaming
    // ReSharper restore CollectionNeverUpdated.Local
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}