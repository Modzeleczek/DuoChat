using Newtonsoft.Json;
using Shared.MVVM.Model.Cryptography;
using System;

namespace Client.MVVM.Model.JsonConverters
{
    public class PublicKeyJsonConverter : JsonConverter<PublicKey>
    {
        public override PublicKey? ReadJson(JsonReader reader, Type objectType,
            PublicKey? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            /* Nie dopuszczamy do stworzenia PublicKey
            z parametrem modulus będacym nullem. */
            return reader.Value is byte[] bytes ? PublicKey.FromBytesNoLength(bytes) : null;
        }

        public override void WriteJson(JsonWriter writer, PublicKey? value, JsonSerializer serializer)
        {
            if (value is null)
                writer.WriteNull();
            else
                writer.WriteValue(value.ToBytesNoLength());
        }
    }
}
