using System;
using HimbeertoniRaidTool.Common.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HimbeertoniRaidTool.Plugin.DataManagement
{
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
            CharacterReference cRef = new(value);
            serializer.Serialize(writer, cRef, typeof(CharacterReference));
        }

        public override Character? ReadJson(JsonReader reader, Type objectType, Character? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            if (objectType != typeof(Character))
                return null;
            JObject jt = JObject.Load(reader);

            CharacterReference? cRef = jt.ContainsKey("WorldID") && jt.ContainsKey("Name") ?
                new((uint)(jt["WorldID"] ?? 0), (string?)jt["Name"] ?? "") : null;
            if (cRef == null)
                return null;
            Character result = new(cRef.Name, cRef.HomeWorldID);
            _charDB.AddOrGetCharacter(ref result);
            return result;
        }
    }
    public class CharacterReference
    {
        [JsonProperty("WorldID")]
        public uint HomeWorldID;
        [JsonProperty("Name")]
        public string Name;
        [JsonConstructor]
        public CharacterReference(uint homeWorldID, string name)
        {
            HomeWorldID = homeWorldID;
            Name = name;
        }
        public CharacterReference(Character c)
        {
            HomeWorldID = c.HomeWorldID;
            Name = c.Name;
        }
    }
}
