using System;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HimbeertoniRaidTool.DataManagement
{
    internal class GearSetReferenceConverter : JsonConverter<GearSet>

    {
        public override void WriteJson(JsonWriter writer, GearSet? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            GearSetReference gsRef = new(value);
            serializer.Serialize(writer, gsRef, typeof(GearSetReference));
        }

        public override GearSet? ReadJson(JsonReader reader, Type objectType, GearSet? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            JObject jt = JObject.Load(reader);

            GearSetReference? gsRef = jt.ContainsKey("ID") && jt.ContainsKey("ManagedBy") ? new((string)jt["ID"]!, (GearSetManager)(int)jt["ManagedBy"]!) : null;
            if (gsRef == null)
                return null;
            GearSet result = new()
            {
                ManagedBy = gsRef.ManagedBy,
                HrtID = gsRef.ManagedBy == GearSetManager.HRT ? gsRef.ID : "",
                EtroID = gsRef.ManagedBy == GearSetManager.Etro ? gsRef.ID : "",
            };
            DataManager.GearDB.AddOrGetSet(ref result);
            return result;
        }
    }
    public class GearSetReference
    {
        public string ID { get; set; }
        public GearSetManager ManagedBy { get; set; }

        public GearSetReference(GearSet set)
        {
            ManagedBy = set.ManagedBy;
            if (set.ManagedBy == GearSetManager.HRT)
                ID = set.HrtID;
            else
                ID = set.EtroID;
        }

        public GearSetReference(string id, GearSetManager gearSetManager)
        {
            ID = id;
            ManagedBy = gearSetManager;
        }
    }
}
