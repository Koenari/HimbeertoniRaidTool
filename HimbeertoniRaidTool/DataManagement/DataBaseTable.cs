using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public interface IDataBaseTable<T> where T : IHasHrtId
{
    internal bool Load(JsonSerializerSettings jsonSettings, string data);
    internal bool TryGet(HrtId id, [NotNullWhen(true)] out T? value);
    internal bool TryAdd(in T value);
    internal IEnumerable<T> GetValues();
    internal HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    public HashSet<HrtId> GetReferencedIds();
    internal ulong GetNextSequence();
    internal bool Contains(HrtId hrtId);
    public void RemoveUnused(HashSet<HrtId> referencedIds);
    public void FixEntries();
    internal string Serialize(JsonSerializerSettings settings);
}

public abstract class DataBaseTable<T>(IIdProvider idProvider, IEnumerable<JsonConverter> converters)
    : IDataBaseTable<T>
    where T : class, IHasHrtId
{

    protected readonly Dictionary<HrtId, T> Data = new();
    private readonly IImmutableList<JsonConverter> _refConverters = ImmutableList.CreateRange(converters);
    private ulong _nextSequence = 0;
    protected bool LoadError = false;
    protected bool IsLoaded = false;
    public virtual bool Load(JsonSerializerSettings settings, string serializedData)
    {
        List<JsonConverter> savedConverters = [..settings.Converters];
        foreach (JsonConverter jsonConverter in _refConverters)
        {
            settings.Converters.Add(jsonConverter);
        }
        var data = JsonConvert.DeserializeObject<List<T>>(serializedData, settings);
        settings.Converters = savedConverters;
        if (data is null)
        {
            ServiceManager.Logger.Error($"Could not load {typeof(T)} database");
            LoadError = true;
            return IsLoaded;
        }
        foreach (T value in data)
        {
            if (value.LocalId.IsEmpty)
            {
                ServiceManager.Logger.Error(
                    $"{typeof(T).Name} {value} was missing an ID and was removed from the database");
                continue;
            }
            if (Data.TryAdd(value.LocalId, value))
                _nextSequence = Math.Max(_nextSequence, value.LocalId.Sequence);
        }
        _nextSequence++;
        ServiceManager.Logger.Information($"Database contains {Data.Count} entries of type {typeof(T).Name}");
        IsLoaded = true;
        return IsLoaded;
    }
    public virtual bool TryGet(HrtId id, [NotNullWhen(true)] out T? value) => Data.TryGetValue(id, out value);
    public virtual bool TryAdd(in T c)
    {
        if (c.LocalId.IsEmpty)
            c.LocalId = idProvider.CreateId(c.IdType);
        return Data.TryAdd(c.LocalId, c);
    }
    public void RemoveUnused(HashSet<HrtId> referencedIds)
    {
        ServiceManager.Logger.Debug($"Begin pruning of {typeof(T).Name} database.");
        IEnumerable<HrtId> keyList = new List<HrtId>(Data.Keys);
        foreach (HrtId id in keyList.Where(id => !referencedIds.Contains(id)))
        {
            Data.Remove(id);
            ServiceManager.Logger.Information($"Removed {id} from {typeof(T).Name} database");
        }
        ServiceManager.Logger.Debug($"Finished pruning of {typeof(T).Name} database.");
    }
    public bool Contains(HrtId hrtId) => Data.ContainsKey(hrtId);
    public IEnumerable<T> GetValues() => Data.Values;
    public ulong GetNextSequence() => _nextSequence++;
    public abstract HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    public string Serialize(JsonSerializerSettings settings)
    {
        List<JsonConverter> savedConverters = [..settings.Converters];
        foreach (JsonConverter jsonConverter in _refConverters)
        {
            settings.Converters.Add(jsonConverter);
        }
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters = savedConverters;
        return result;
    }
    public abstract HashSet<HrtId> GetReferencedIds();
    public virtual void FixEntries() { }

    internal abstract class SearchWindow<TData, TDataBaseTable>(
        TDataBaseTable dataBase,
        Action<TData> onSelect,
        Action? onCancel) : HrtWindow
        where TDataBaseTable : IDataBaseTable<TData>
        where TData : IHasHrtId
    {
        protected readonly TDataBaseTable Database = dataBase;

        protected TData? Selected;

        protected void Save()
        {
            if (Selected == null)
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