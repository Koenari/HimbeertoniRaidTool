using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

public class HrtIdReferenceConverter<T> : JsonConverter<T> where T : IHasHrtId, new()
{
    private readonly IDataBaseTable<T> _db;

    internal HrtIdReferenceConverter(IDataBaseTable<T> db)
    {
        _db = db;
    }
    public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
    {
        if (value == null)
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
        _db.TryGet(id, out T? result);
        result ??= new T();
        return result;
    }
}