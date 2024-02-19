using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.Connectors.Utils;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Connectors;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
internal class EtroConnector : WebConnector
{
    public const string WEB_BASE_URL = "https://etro.gg/";
    public const string API_BASE_URL = WEB_BASE_URL + "api/";
    public const string GEARSET_API_BASE_URL = API_BASE_URL + "gearsets/";
    public const string GEARSET_WEB_BASE_URL = WEB_BASE_URL + "gearset/";
    public const string MATERIA_API_BASE_URL = API_BASE_URL + "materia/";
    public const string RELIC_API_BASE_URL = API_BASE_URL + "relic/";
    public const string BIS_API_BASE_URL = GEARSET_API_BASE_URL + "bis/";
    private readonly Dictionary<Job, Dictionary<string, string>> _bisCache;
    private readonly ConcurrentDictionary<uint, (MateriaCategory, MateriaLevel)> _materiaCache = new();
    private bool _materiaCacheReady;
    internal EtroConnector(TaskManager tm, IPluginLog log) : base(new RateLimit(4, new TimeSpan(0, 0, 30)))
    {
        tm.RegisterTask(new HrtTask(() => FillMateriaCache(_materiaCache),
                                    msg =>
                                    {
                                        if (msg.MessageType == HrtUiMessageType.Failure)
                                            log.Error(msg.Message);
                                        else
                                            log.Info(msg.Message);
                                    }
                                  , "Get Etro Materia definitions"));
        _bisCache = new Dictionary<Job, Dictionary<string, string>>();
        foreach (Job job in Enum.GetValues<Job>())
        {
            _bisCache.Add(job, new Dictionary<string, string>());
        }
        tm.RegisterTask(new HrtTask(FillBisList,
                                    msg =>
                                    {
                                        if (msg.MessageType == HrtUiMessageType.Failure)
                                            log.Error(msg.Message);
                                        else
                                            log.Info(msg.Message);
                                    }, "Load BiS list from etro"));
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
    public IReadOnlyDictionary<string, string> GetBiS(Job job) => _bisCache[job];
    public string GetDefaultBiS(Job job) => _bisCache[job].Keys.FirstOrDefault("");

    private HrtUiMessage FillBisList()
    {
        HrtUiMessage failureMessage = new(GeneralLoc.EtroConnector_FillBisList_ErrorMessaeg, HrtUiMessageType.Failure);
        string? jsonResponse = MakeWebRequest(BIS_API_BASE_URL);
        if (jsonResponse == null)
            return failureMessage;
        var sets = JsonConvert.DeserializeObject<EtroGearSet[]>(jsonResponse, JsonSettings);
        if (sets == null) return failureMessage;
        foreach (EtroGearSet set in sets)
        {
            if (set.id != null)
                _bisCache[set.job][set.id] = set.name ?? set.id;
        }
        return new HrtUiMessage(GeneralLoc.EtroConnector_FillBisList_Success, HrtUiMessageType.Success);
    }

    private HrtUiMessage FillMateriaCache(ConcurrentDictionary<uint, (MateriaCategory, MateriaLevel)> materiaCache)
    {
        var errorMessage = new HrtUiMessage(GeneralLoc.EtroConnector_FillMateriaCache_Error, HrtUiMessageType.Failure);
        string? jsonResponse = MakeWebRequest(MATERIA_API_BASE_URL);
        if (jsonResponse == null) return errorMessage;
        var matList = JsonConvert.DeserializeObject<EtroMateria[]>(jsonResponse, JsonSettings);
        if (matList == null) return errorMessage;
        foreach (EtroMateria? mat in matList)
        {
            for (byte i = 0; i < mat.Tiers.Length; i++)
            {
                materiaCache.TryAdd(mat.Tiers[i].Id, ((MateriaCategory)mat.Id, (MateriaLevel)i));
            }
        }
        _materiaCacheReady = true;
        return new HrtUiMessage(GeneralLoc.EtroConnector_FillMateriaCache_Success, HrtUiMessageType.Success);
    }

    public void GetGearSetAsync(GearSet set, Action<HrtUiMessage>? messageCallback = null,
                                string taskName = "Etro Update")
    {
        messageCallback ??= _ => { };
        ServiceManager.TaskManager.RegisterTask(new HrtTask(() => GetGearSet(set), messageCallback, taskName));
    }

    private EtroRelic? GetRelicItem(string id)
    {
        string? relicJson = MakeWebRequest(RELIC_API_BASE_URL + id);
        return relicJson == null ? null : JsonConvert.DeserializeObject<EtroRelic>(relicJson, JsonSettings);
    }

    private HrtUiMessage GetGearSet(GearSet set)
    {
        HrtUiMessage errorMessage = new(string.Format(GeneralLoc.EtroConnector_GetGearSet_Error, set.Name),
                                        HrtUiMessageType.Failure);
        if (set.EtroId.Equals(""))
            return errorMessage;
        errorMessage.Message = $"{errorMessage.Message} ({set.EtroId})";
        string? jsonResponse = MakeWebRequest(GEARSET_API_BASE_URL + set.EtroId);
        if (jsonResponse == null)
            return errorMessage;
        var etroSet = JsonConvert.DeserializeObject<EtroGearSet>(jsonResponse, JsonSettings);
        if (etroSet == null)
            return errorMessage;
        set.Name = etroSet.name ?? "";
        set.TimeStamp = etroSet.lastUpdate;
        set.EtroFetchDate = DateTime.UtcNow;
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
            EtroRelic? relic = GetRelicItem(relicId);
            if (relic is null) continue;
            switch (slot)
            {
                case "weapon":
                    FillRelicItem(relic, GearSetSlot.MainHand);
                    break;
                default:
                    ServiceManager.PluginLog.Error(string.Format(GeneralLoc.EtroConnector_GetGearSet_RelicError, slot));
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
                IsHq = ServiceManager.ItemInfo.CanBeCrafted(id),
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
                set[slot].AddMateria(new HrtMateria(
                                         _materiaCache.GetValueOrDefault<uint, (MateriaCategory, MateriaLevel)>(
                                             matId!.Value, (0, 0))));
            }
        }
    }
    internal HrtUiMessage UpdateEtroSets(bool updateAll, int maxAgeInDays)
    {
        while (!_materiaCacheReady)
        {
            Thread.Sleep(1);
        }
        DateTime oldestValid = DateTime.UtcNow - new TimeSpan(maxAgeInDays, 0, 0, 0);
        int totalCount = 0;
        int updateCount = 0;
        foreach (GearSet gearSet in ServiceManager.HrtDataManager.GearDb.GetValues()
                                                  .Where(set => set.ManagedBy == GearSetManager.Etro))
        {
            totalCount++;
            if (gearSet.IsEmpty || gearSet.EtroFetchDate < oldestValid && updateAll)
            {
                HrtUiMessage message = GetGearSet(gearSet);
                if (message.MessageType is HrtUiMessageType.Error or HrtUiMessageType.Failure)
                    ServiceManager.PluginLog.Error(message.Message);
                updateCount++;
            }
        }

        return new HrtUiMessage(
            string.Format(GeneralLoc.EtroConnector_UpdateEtroSets_Finished, updateCount, totalCount));

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
        public Dictionary<string, string>? relics { get; set; }
    }

    private class EtroMateriaTier
    {
        public ushort Id { get; set; }
    }



    private class EtroMateria
    {
        [JsonIgnore]
        public readonly EtroMateriaTier[] Tiers = new EtroMateriaTier[10];
        public uint Id { get; set; }
        public EtroMateriaTier Tier1 { get => Tiers[0]; set => Tiers[0] = value; }
        public EtroMateriaTier Tier2 { get => Tiers[1]; set => Tiers[1] = value; }
        public EtroMateriaTier Tier3 { get => Tiers[2]; set => Tiers[2] = value; }
        public EtroMateriaTier Tier4 { get => Tiers[3]; set => Tiers[3] = value; }
        public EtroMateriaTier Tier5 { get => Tiers[4]; set => Tiers[4] = value; }
        public EtroMateriaTier Tier6 { get => Tiers[5]; set => Tiers[5] = value; }
        public EtroMateriaTier Tier7 { get => Tiers[6]; set => Tiers[6] = value; }
        public EtroMateriaTier Tier8 { get => Tiers[7]; set => Tiers[7] = value; }
        public EtroMateriaTier Tier9 { get => Tiers[8]; set => Tiers[8] = value; }
        public EtroMateriaTier Tier10 { get => Tiers[9]; set => Tiers[9] = value; }
    }
}