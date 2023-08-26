using Newtonsoft.Json;
using Shared.MVVM.Model.Networking;
using System;

namespace Client.MVVM.Model.JsonConverters
{
    public class PortJsonConverter : JsonConverter<Port>
    {
        public override Port ReadJson(JsonReader reader, Type objectType,
            Port existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // return new Port((ushort)reader.Value);
            return Port.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, Port value, JsonSerializer serializer)
        {
            // writer.WriteValue(value.Value);
            writer.WriteValue(value.ToString());
        }
    }
}
