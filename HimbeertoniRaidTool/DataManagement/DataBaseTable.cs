using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public interface IDataBaseTable<T> where T : class, IHasHrtId<T>, new()
{
    internal bool Load(JsonSerializerSettings jsonSettings, string data);
    internal bool TryGet(HrtId id, [NotNullWhen(true)] out T? value);
    internal T? GetNullable(HrtId id);
    internal Reference<T> GetRef(HrtId id);
    internal bool Search(in Func<T?, bool> predicate, [NotNullWhen(true)] out T? value);
    internal bool TryAdd(in T value);
    internal bool TryRemove(T data);
    internal IEnumerable<T> GetValues();
    internal void OpenSearchWindow(IUiSystem uiSystem, Action<T> onSelect, Action? onCancel = null);
    internal HrtWindow GetSearchWindow(IUiSystem uiSystem, Action<T> onSelect, Action? onCancel = null);
    internal OldHrtIdReferenceConverter<T> GetOldRefConverter();
    internal HrtIdReferenceConverter<T> GetRefConverter();
    public HashSet<HrtId> GetReferencedIds();
    internal ulong GetNextSequence();
    internal bool Contains(HrtId hrtId);
    public void RemoveUnused(HashSet<HrtId> referencedIds);
    public void FixEntries(HrtDataManager hrtDataManager);
    internal string Serialize(JsonSerializerSettings settings);
}

public abstract class DataBaseTable<T>(IIdProvider idProvider, IEnumerable<JsonConverter> converters, ILogger logger)
    : IDataBaseTable<T>
    where T : class, IHasHrtId<T>, new()
{

    protected readonly Dictionary<HrtId, T> Data = new();
    private readonly IImmutableList<JsonConverter> _refConverters = ImmutableList.CreateRange(converters);
    private ulong _nextSequence = 0;
    protected bool LoadError = false;
    protected bool IsLoaded = false;
    protected readonly ILogger Logger = logger;

    public virtual bool Load(JsonSerializerSettings settings, string serializedData)
    {
        List<JsonConverter> savedConverters = [..settings.Converters];
        foreach (var jsonConverter in _refConverters)
        {
            settings.Converters.Add(jsonConverter);
        }
        var data = JsonConvert.DeserializeObject<List<T>>(serializedData, settings);
        settings.Converters = savedConverters;
        if (data is null)
        {
            Logger.Error($"Could not load {typeof(T)} database");
            LoadError = true;
            return IsLoaded;
        }
        foreach (var value in data)
        {
            if (value.LocalId.IsEmpty)
            {
                Logger.Error(
                    $"{typeof(T).Name} {value} was missing an ID and was removed from the database");
                continue;
            }
            if (Data.TryAdd(value.LocalId, value))
                _nextSequence = Math.Max(_nextSequence, value.LocalId.Sequence);
        }
        _nextSequence++;
        Logger.Information($"Database contains {Data.Count} entries of type {typeof(T).Name}");
        IsLoaded = true;
        return IsLoaded;
    }
    public virtual bool TryGet(HrtId id, [NotNullWhen(true)] out T? value) => Data.TryGetValue(id, out value);
    public virtual T? GetNullable(HrtId id)
    {
        TryGet(id, out var value);
        return value;
    }
    public virtual Reference<T> GetRef(HrtId id) => new(id, GetNullable);
    public virtual bool TryAdd(in T c)
    {
        if (c.LocalId.IsEmpty)
            c.LocalId = idProvider.CreateId(T.IdType);
        return Data.TryAdd(c.LocalId, c);
    }

    public virtual bool TryRemove(T data) => Data.Remove(data.LocalId);

    public virtual bool Search(in Func<T?, bool> predicate, [NotNullWhen(true)] out T? value)
    {
        value = Data.Values.FirstOrDefault(predicate, null);
        return value is not null;
    }
    public void RemoveUnused(HashSet<HrtId> referencedIds)
    {
        Logger.Debug($"Begin pruning of {typeof(T).Name} database.");
        IEnumerable<HrtId> keyList = new List<HrtId>(Data.Keys);
        foreach (var id in keyList.Where(id => !referencedIds.Contains(id)))
        {
            Data.Remove(id);
            Logger.Information($"Removed {id} from {typeof(T).Name} database");
        }
        Logger.Debug($"Finished pruning of {typeof(T).Name} database.");
    }
    public bool Contains(HrtId hrtId) => Data.ContainsKey(hrtId);
    public IEnumerable<T> GetValues() => Data.Values;
    public ulong GetNextSequence() => _nextSequence++;
    public void OpenSearchWindow(IUiSystem uiSystem, Action<T> onSelect, Action? onCancel = null) =>
        uiSystem.AddWindow(GetSearchWindow(uiSystem, onSelect, onCancel));

    public abstract HrtWindow GetSearchWindow(IUiSystem uiSystem, Action<T> onSelect, Action? onCancel = null);
    public string Serialize(JsonSerializerSettings settings)
    {
        List<JsonConverter> savedConverters = [..settings.Converters];
        foreach (var jsonConverter in _refConverters)
        {
            settings.Converters.Add(jsonConverter);
        }
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters = savedConverters;
        return result;
    }
    public abstract HashSet<HrtId> GetReferencedIds();

    public OldHrtIdReferenceConverter<T> GetOldRefConverter() => new(this);

    public HrtIdReferenceConverter<T> GetRefConverter() => new(this);

    public virtual void FixEntries(HrtDataManager hrtDataManager) { }

    internal abstract class SearchWindow<TData, TDataBaseTable>(
        IUiSystem uiSystem,
        TDataBaseTable dataBase,
        Action<TData> onSelect,
        Action? onCancel) : HrtWindow(uiSystem)
        where TDataBaseTable : IDataBaseTable<TData>
        where TData : class, IHasHrtId<TData>, new()
    {
        protected readonly TDataBaseTable Database = dataBase;

        protected TData? Selected;

        protected void Save()
        {
            if (Selected is null)
                return;
            onSelect.Invoke(Selected!);
            Hide();
        }

        public override void Draw()
        {
            if (ImGuiHelper.SaveButton(null, Selected is not null))
            {
                Save();
            }
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
            {
                onCancel?.Invoke();
                Hide();
            }
            DrawContent();
        }
        protected abstract void DrawContent();
    }
}