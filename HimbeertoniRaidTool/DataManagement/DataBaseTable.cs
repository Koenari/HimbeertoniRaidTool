using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public interface IDataBaseTable<T> where T : IHasHrtId
{
    internal bool TryGet(HrtId id, [NotNullWhen(true)] out T? value);
    internal bool TryAdd(in T value);
    internal IEnumerable<T> GetValues();
    internal HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    public HashSet<HrtId> GetReferencedIds();
    internal ulong GetNextSequence();
    internal bool Contains(HrtId hrtId);
    public void RemoveUnused(HashSet<HrtId> referencedIds);
    public void FixEntries();
}

public abstract class DataBaseTable<T, S> : IDataBaseTable<T> where T : class, IHasHrtId where S : IHasHrtId, new()
{

    protected readonly Dictionary<HrtId, T> Data = new();
    protected readonly HrtIdReferenceConverter<S>? RefConv;
    protected readonly IIdProvider IdProvider;
    protected ulong NextSequence = 0;
    protected bool LoadError = false;
    protected DataBaseTable(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<S>? conv,
        JsonSerializerSettings settings)
    {
        IdProvider = idProvider;
        RefConv = conv;
        if (RefConv is not null) settings.Converters.Add(RefConv);
        var data = JsonConvert.DeserializeObject<List<T>>(serializedData, settings);
        if (RefConv is not null) settings.Converters.Remove(RefConv);
        if (data is null)
        {
            ServiceManager.PluginLog.Error($"Could not load {typeof(T)} database");
            LoadError = true;
            return;
        }
        foreach (T value in data)
        {
            if (value.LocalId.IsEmpty)
            {
                ServiceManager.PluginLog.Error(
                    $"{typeof(T).Name} {value} was missing an ID and was removed from the database");
                continue;
            }
            if (Data.TryAdd(value.LocalId, value))
                NextSequence = Math.Max(NextSequence, value.LocalId.Sequence);
        }
        NextSequence++;
        ServiceManager.PluginLog.Information($"Database contains {Data.Count} entries of type {typeof(T).Name}");
    }
    public virtual bool TryGet(HrtId id, [NotNullWhen(true)] out T? value) => Data.TryGetValue(id, out value);
    public virtual bool TryAdd(in T c)
    {
        if (c.LocalId.IsEmpty)
            c.LocalId = IdProvider.CreateId(c.IdType);
        return Data.TryAdd(c.LocalId, c);
    }
    public void RemoveUnused(HashSet<HrtId> referencedIds)
    {
        ServiceManager.PluginLog.Debug($"Begin pruning of {typeof(T).Name} database.");
        IEnumerable<HrtId> keyList = new List<HrtId>(Data.Keys);
        foreach (HrtId id in keyList.Where(id => !referencedIds.Contains(id)))
        {
            Data.Remove(id);
            ServiceManager.PluginLog.Information($"Removed {id} from {typeof(T).Name} database");
        }
        ServiceManager.PluginLog.Debug($"Finished pruning of {typeof(T).Name} database.");
    }
    public bool Contains(HrtId hrtId) => Data.ContainsKey(hrtId);
    public IEnumerable<T> GetValues() => Data.Values;
    public ulong GetNextSequence() => NextSequence++;
    public abstract HrtWindow OpenSearchWindow(Action<T> onSelect, Action? onCancel = null);
    internal string Serialize(JsonSerializerSettings settings)
    {
        if (RefConv is not null) settings.Converters.Add(RefConv);
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        if (RefConv is not null) settings.Converters.Remove(RefConv);
        return result;
    }
    public abstract HashSet<HrtId> GetReferencedIds();
    public virtual void FixEntries() { }

    internal abstract class SearchWindow<Q, R> : HrtWindow where R : IDataBaseTable<Q> where Q : IHasHrtId
    {
        protected readonly R Database;
        private readonly Action<Q> _onSelect;
        private readonly Action? _onCancel;

        protected Q? Selected;
        protected SearchWindow(R dataBase, Action<Q> onSelect, Action? onCancel)
        {
            _onSelect = onSelect;
            _onCancel = onCancel;
            Database = dataBase;
        }

        protected void Save()
        {
            if (Selected == null)
                return;
            _onSelect.Invoke(Selected!);
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
                _onCancel?.Invoke();
                Hide();
            }
            DrawContent();
        }
        protected abstract void DrawContent();
    }
}