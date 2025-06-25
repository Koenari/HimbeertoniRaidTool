using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public class HrtIdReferenceConverter<TData> : JsonConverter<Reference<TData>>
    where TData : class, IHasHrtId<TData>, new()
{
    private readonly IDataBaseTable<TData> _db;
    private static readonly Reference<TData> EmptyRef = new(HrtId.Empty, _ => new TData());
    internal HrtIdReferenceConverter(IDataBaseTable<TData> db)
    {
        _db = db;
    }

    public override void WriteJson(JsonWriter writer, Reference<TData>? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }
        serializer.Serialize(writer, value.Id, typeof(HrtId));
    }

    public override Reference<TData> ReadJson(JsonReader reader, Type objectType, Reference<TData>? existingValue,
                                              bool hasExistingValue,
                                              JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null || objectType != typeof(Reference<TData>))
            return EmptyRef;

        var id = JObject.Load(reader).ToObject<HrtId>();
        if (id is null)
            return EmptyRef;
        return _db.GetRef(id);
    }
}

public class OldHrtIdReferenceConverter<T> : JsonConverter<T> where T : class, IHasHrtId<T>, new()
{
    private readonly IDataBaseTable<T> _db;

    internal OldHrtIdReferenceConverter(IDataBaseTable<T> db)
    {
        _db = db;
    }
    public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }
        serializer.Serialize(writer, value.LocalId, typeof(HrtId));
    }

    public override T ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue,
                               JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null || objectType != typeof(T))
            return new T();

        var id = JObject.Load(reader).ToObject<HrtId>();
        if (id is null)
            return new T();
        _db.TryGet(id, out var result);
        result ??= new T();
        return result;
    }
}