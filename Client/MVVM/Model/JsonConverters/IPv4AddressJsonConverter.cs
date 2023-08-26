using Newtonsoft.Json;
using Shared.MVVM.Model.Networking;
using System;

namespace Client.MVVM.Model.JsonConverters
{
    public class IPv4AddressJsonConverter : JsonConverter<IPv4Address>
    {
        public override IPv4Address ReadJson(JsonReader reader, Type objectType,
            IPv4Address existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // return new IPv4Address((uint)reader.Value);
            return IPv4Address.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, IPv4Address value, JsonSerializer serializer)
        {
            // writer.WriteValue(value.BinaryRepresentation);
            writer.WriteValue(value.ToString());
        }
    }
}
