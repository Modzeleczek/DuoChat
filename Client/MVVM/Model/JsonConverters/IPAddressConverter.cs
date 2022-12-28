using Newtonsoft.Json;
using System;
using System.Net;

namespace Client.MVVM.Model.JsonConverters
{
    public class IPAddressConverter : JsonConverter
    {
        public override bool CanWrite => true;

        public override bool CanRead => true;

        public IPAddressConverter() : base() { }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPAddress);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IPAddress ipAddress = (IPAddress)value;
            writer.WriteValue((int)ipAddress.Address);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            reader.Read();
            int? int32 = reader.ReadAsInt32();
            if (!int32.HasValue) return null;
            return new IPAddress((long)int32.Value);
        }
    }
}
