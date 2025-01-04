﻿using System.Diagnostics.CodeAnalysis;
using System.Net;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.Connectors.Utils;
using HimbeertoniRaidTool.Plugin.DataManagement;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Connectors;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
internal sealed class EtroConnector : WebConnector, IReadOnlyGearConnector
{
    private const string WEB_BASE_URL = "https://etro.gg/";
    private const string API_BASE_URL = WEB_BASE_URL + "api/";
    private const string GEARSET_API_BASE_URL = API_BASE_URL + "gearsets/";
    private const string GEARSET_WEB_BASE_URL = WEB_BASE_URL + "gearset/";
    private const string RELIC_API_BASE_URL = API_BASE_URL + "relic/";
    private const string BIS_API_BASE_URL = GEARSET_API_BASE_URL + "bis/";
    private readonly Dictionary<Job, List<ExternalBiSDefinition>> _bisCache = [];
    private readonly Dictionary<uint, FoodItem> _foodLookup = [];
    private readonly TaskManager _taskManager;
    private readonly HrtDataManager _hrtDataManager;
    internal EtroConnector(HrtDataManager hrtDataManager, TaskManager tm, ILogger log, IDataManager dataManager) : base(
        log, new RateLimit(10, new TimeSpan(0, 0, 30)))
    {
        _hrtDataManager = hrtDataManager;
        _taskManager = tm;
        foreach (var job in Enum.GetValues<Job>())
        {
            _bisCache.Add(job, []);
        }
        _taskManager.RegisterTask(new HrtTask<HrtUiMessage>(FillBisList,
                                                            msg =>
                                                            {
                                                                if (msg.MessageType == HrtUiMessageType.Failure)
                                                                    Logger.Error(msg.Message);
                                                                else
                                                                    Logger.Info(msg.Message);
                                                            }, "Load BiS list from etro"));
        foreach (var food in dataManager.Excel.GetSheet<LuminaItem>()
                                        .Where(ItemExtensions.IsFood))
        {
            _foodLookup[food.ItemAction.Value.Data[1]] = new FoodItem(food.RowId);

        }
    }
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
    public IList<ExternalBiSDefinition> GetBiSList(Job job) => _bisCache[job];
    public ExternalBiSDefinition GetDefaultBiS(Job job) => _bisCache[job].FirstOrDefault(new ExternalBiSDefinition());
    public IList<ExternalBiSDefinition> GetPossibilities(string id)
    {
        if (id.Equals("")) return [];
        var httpResponse = MakeWebRequest(GEARSET_API_BASE_URL + id);
        if (httpResponse is not { IsSuccessStatusCode: true }) return [];
        var readTask = httpResponse.Content.ReadAsStringAsync();
        readTask.Wait();
        var etroSet = JsonConvert.DeserializeObject<EtroGearSet>(readTask.Result, JsonSettings);
        return etroSet?.name is null ? [] : [new ExternalBiSDefinition(GearSetManager.Etro, id, 0, etroSet.name)];
    }
    private HrtUiMessage FillBisList()
    {
        HrtUiMessage failureMessage = new(GeneralLoc.EtroConnector_FillBisList_ErrorMessaeg, HrtUiMessageType.Failure);
        string? jsonResponse = GetContent(MakeWebRequest(BIS_API_BASE_URL));
        if (jsonResponse == null)
            return failureMessage;
        var sets = JsonConvert.DeserializeObject<EtroGearSet[]>(jsonResponse, JsonSettings);
        if (sets == null) return failureMessage;
        foreach (var set in sets)
        {
            if (set.id != null)
                _bisCache[set.job].Add(new ExternalBiSDefinition(GearSetManager.Etro, set.id, 0, set.name ?? set.id));
        }
        return new HrtUiMessage(GeneralLoc.EtroConnector_FillBisList_Success, HrtUiMessageType.Success);
    }

    public bool BelongsToThisService(string url) => url.StartsWith(WEB_BASE_URL);
    public string GetId(string url) => BelongsToThisService(url) ? url[GEARSET_WEB_BASE_URL.Length..] : url;
    public string GetWebUrl(string id) => GEARSET_WEB_BASE_URL + id;
    public void RequestGearSetUpdate(GearSet set, Action<HrtUiMessage>? messageCallback = null,
                                     string taskName = "Etro Update")
    {
        messageCallback ??= _ => { };
        _taskManager.RegisterTask(new HrtTask<HrtUiMessage>(() => UpdateGearSet(set), messageCallback, taskName));
    }

    private EtroRelic? GetRelicItem(string id)
    {
        string? relicJson = GetContent(MakeWebRequest(RELIC_API_BASE_URL + id));
        return relicJson == null ? null : JsonConvert.DeserializeObject<EtroRelic>(relicJson, JsonSettings);
    }

    public HrtUiMessage UpdateGearSet(GearSet set)
    {
        HrtUiMessage failureMessage = new(string.Format(GeneralLoc.EtroConnector_GetGearSet_Error, set.Name),
                                          HrtUiMessageType.Failure);
        if (set.ExternalId.Equals(""))
            return failureMessage;
        failureMessage = new HrtUiMessage($"{failureMessage.Message} ({set.ExternalId})", HrtUiMessageType.Failure);

        var httpResponse = MakeWebRequest(GEARSET_API_BASE_URL + set.ExternalId);
        if (httpResponse == null)
            return failureMessage;
        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
        {
            set.ManagedBy = GearSetManager.Hrt;
            set.ExternalId = string.Empty;
            return new HrtUiMessage($"Set {set.Name} is not available at etro.gg. It is now managed locally",
                                    HrtUiMessageType.Warning);
        }
        var readTask = httpResponse.Content.ReadAsStringAsync();
        readTask.Wait();
        var etroSet = JsonConvert.DeserializeObject<EtroGearSet>(readTask.Result, JsonSettings);
        if (etroSet == null)
            return failureMessage;
        set.Name = etroSet.name ?? "";
        set.TimeStamp = etroSet.lastUpdate;
        set.LastExternalFetchDate = DateTime.UtcNow;
        if (!_foodLookup.TryGetValue(etroSet.food, out var newFood))
            Logger.Warning($"Did not find food {etroSet.food} for set {set.Name} ({set.ExternalId})");
        set.Food = newFood;
        HrtUiMessage successMessage = new(string.Format(GeneralLoc.EtroConnector_GetGearSet_Success, set.Name),
                                          HrtUiMessageType.Success);
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
        if (etroSet.relics is null)
            return successMessage;
        foreach ((string slot, string relicId) in etroSet.relics)
        {
            var relic = GetRelicItem(relicId);
            if (relic is null) continue;
            switch (slot)
            {
                case "weapon":
                    FillRelicItem(relic, GearSetSlot.MainHand);
                    break;
                default:
                    Logger.Error(string.Format(GeneralLoc.EtroConnector_GetGearSet_RelicError, slot));
                    break;
            }
        }
        return successMessage;
        void FillRelicItem(EtroRelic relic, GearSetSlot slot)
        {
            FillItem(relic.baseItem.id, slot);
            Dictionary<StatType, int> stats = new();
            foreach ((uint statType, int statValue) in relic.Stats)
            {
                stats.Add((StatType)statType, statValue);
            }
            set[slot].RelicStats = stats;
        }
        void FillItem(uint id, GearSetSlot slot)
        {
            set[slot] = new GearItem(id)
            {
                IsHq = new Item(id).CanBeHq,
            };
            string idString = id + slot switch
            {
                GearSetSlot.Ring1 => "L",
                GearSetSlot.Ring2 => "R",
                _                 => "",
            };
            if (!(etroSet.materia?.TryGetValue(idString, out var materia) ?? false)) return;
            foreach (uint? matId in materia.Values.Where(matId => matId.HasValue))
            {
                set[slot].AddMateria(new MateriaItem(matId!.Value));
            }
        }
    }
    public HrtUiMessage UpdateAllSets(bool updateAll, int maxAgeInDays)
    {
        var oldestValid = DateTime.UtcNow - new TimeSpan(maxAgeInDays, 0, 0, 0);
        int totalCount = 0;
        int updateCount = 0;
        foreach (var gearSet in _hrtDataManager.GearDb.GetValues()
                                               .Where(set => set.ManagedBy == GearSetManager.Etro))
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
            string.Format(GeneralLoc.Connector_UpdateAllSets_Finished, updateCount, totalCount, "etro.gg"));

    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("Style", "IDE1006:Naming Styles")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private class EtroRelic
    {
        public BaseItem baseItem { get; set; }

        public uint? param0 { get; set; }
        public uint? param1 { get; set; }
        public uint? param2 { get; set; }
        public uint? param3 { get; set; }
        public uint? param4 { get; set; }
        public uint? param5 { get; set; }

        public int? param0Value { get; set; }
        public int? param1Value { get; set; }
        public int? param2Value { get; set; }
        public int? param3Value { get; set; }
        public int? param4Value { get; set; }
        public int? param5Value { get; set; }
        [JsonIgnore]
        public IEnumerable<(uint, int)> Stats
        {
            get
            {
                if (param0 is null) yield break;
                yield return (param0.Value, param0Value ?? 0);
                if (param1 is null) yield break;
                yield return (param1.Value, param1Value ?? 0);
                if (param2 is null) yield break;
                yield return (param2.Value, param2Value ?? 0);
                if (param3 is null) yield break;
                yield return (param3.Value, param3Value ?? 0);
                if (param4 is null) yield break;
                yield return (param4.Value, param4Value ?? 0);
                if (param5 is null) yield break;
                yield return (param5.Value, param5Value ?? 0);
            }
        }

        internal struct BaseItem
        {
            public uint id { get; set; }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("Style", "IDE1006:Naming Styles")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
    private class EtroGearSet
    {

        public string? id { get; set; }

        public string? jobAbbrev { get; set; }

        public Job job
        {
            get
            {
                try
                {
                    return Enum.Parse<Job>(jobAbbrev ?? "ADV");
                }
                catch (Exception e) when (e is ArgumentException or ArgumentNullException)
                {
                    return Job.ADV;
                }

            }
        }

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
        public uint food { get; set; }
        public Dictionary<string, string>? relics { get; set; }
    }
}