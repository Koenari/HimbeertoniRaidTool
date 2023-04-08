using HimbeertoniRaidTool.Common.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HimbeertoniRaidTool.Plugin.DataManagement;


[Obsolete]
internal class LegacyGearSetReferenceConverter : JsonConverter<GearSet>
{
    private readonly LegacyGearDB _gearDB;

    internal LegacyGearSetReferenceConverter(LegacyGearDB gearDB)
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
        LegacyGearSetReference gsRef = new(value);
        serializer.Serialize(writer, gsRef, typeof(LegacyGearSetReference));
    }

    public override GearSet? ReadJson(JsonReader reader, Type objectType, GearSet? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        JObject jt = JObject.Load(reader);

        LegacyGearSetReference? gsRef = jt.ContainsKey("ID") && jt.ContainsKey("ManagedBy") ? new((string)jt["ID"]!, (GearSetManager)(int)jt["ManagedBy"]!) : null;
        if (gsRef == null)
            return null;
        GearSet result = new()
        {
            ManagedBy = gsRef.ManagedBy,
            OldHrtID = gsRef.ManagedBy == GearSetManager.HRT ? gsRef.ID : "",
            EtroID = gsRef.ManagedBy == GearSetManager.Etro ? gsRef.ID : "",
        };
        _gearDB.AddOrGetSet(ref result);
        return result;
    }
}
[Obsolete]
public class LegacyGearSetReference
{
    public string ID { get; set; }
    public GearSetManager ManagedBy { get; set; }

    public LegacyGearSetReference(GearSet set)
    {
        ManagedBy = set.ManagedBy;
        if (set.ManagedBy == GearSetManager.HRT)
            ID = set.OldHrtID;
        else
            ID = set.EtroID;
    }

    public LegacyGearSetReference(string id, GearSetManager gearSetManager)
    {
        ID = id;
        ManagedBy = gearSetManager;
    }
}
[Obsolete]
internal class LegacyCharacterReferenceConverter : JsonConverter<Character>
{
    private readonly LegacyCharacterDB _charDB;
    public LegacyCharacterReferenceConverter(LegacyCharacterDB charDB)
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
        LegacyCharacterReference cRef = new(value);
        serializer.Serialize(writer, cRef, typeof(LegacyCharacterReference));
    }

    public override Character? ReadJson(JsonReader reader, Type objectType, Character? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        if (objectType != typeof(Character))
            return null;
        JObject jt = JObject.Load(reader);

        LegacyCharacterReference? cRef = jt.ContainsKey("WorldID") && jt.ContainsKey("Name") ?
            new((uint)(jt["WorldID"] ?? 0), (string?)jt["Name"] ?? "") : null;
        if (cRef == null)
            return null;
        Character result = new(cRef.Name, cRef.HomeWorldID);
        _charDB.AddOrGetCharacter(ref result);
        return result;
    }
}
[Obsolete]
public class LegacyCharacterReference
{
    [JsonProperty("WorldID")]
    public uint HomeWorldID;
    [JsonProperty("Name")]
    public string Name;
    [JsonConstructor]
    public LegacyCharacterReference(uint homeWorldID, string name)
    {
        HomeWorldID = homeWorldID;
        Name = name;
    }
    public LegacyCharacterReference(Character c)
    {
        HomeWorldID = c.HomeWorldID;
        Name = c.Name;
    }
}
