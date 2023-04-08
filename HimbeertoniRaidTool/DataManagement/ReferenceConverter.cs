using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HimbeertoniRaidTool.Plugin.DataManagement;
internal class CharacterReferenceConverter : JsonConverter<Character>
{
    private readonly CharacterDB _charDB;
    public CharacterReferenceConverter(CharacterDB charDB)
    {
        _charDB = charDB;
    }

    public override void WriteJson(JsonWriter writer, Character? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        serializer.Serialize(writer, value.LocalID, typeof(HrtID));
    }

    public override Character? ReadJson(JsonReader reader, Type objectType, Character? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null || objectType != typeof(Character))
            return null;
        HrtID? id = JObject.Load(reader).ToObject<HrtID>();
        if (id is null)
            return null;
        _charDB.TryGetCharacter(id, out Character? result);
        return result;
    }
}
internal class GearsetReferenceConverter : JsonConverter<GearSet>
{
    private readonly GearDB _gearDB;

    internal GearsetReferenceConverter(GearDB gearDB)
    {
        _gearDB = gearDB;
    }
    public override void WriteJson(JsonWriter writer, GearSet? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        serializer.Serialize(writer, value.LocalID, typeof(HrtID));
    }

    public override GearSet? ReadJson(JsonReader reader, Type objectType, GearSet? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        JObject jt = JObject.Load(reader);

        HrtID? id = jt.ToObject<HrtID>();
        if (id is null)
            return null;
        if (!_gearDB.TryGetSet(id, out GearSet? result))
        {
            result = new()
            {
                LocalID = id,
            };
            _gearDB.AddSet(result);
        }
        return result;
    }
}

