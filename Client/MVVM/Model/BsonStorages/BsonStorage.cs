using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using System;
using System.IO;
using Shared.MVVM.View.Localization;
using Shared.MVVM.Model;

namespace Client.MVVM.Model.BsonStorages
{
    public class BsonStorage
    {
        protected static readonly Translator d = Translator.Instance;

        protected Status Load<BsonStructure>(string bsonPath) where BsonStructure : new()
        {
            if (File.Exists(bsonPath))
            {
                try
                {
                    using (var fs = File.OpenRead(bsonPath))
                    using (var br = new BinaryReader(fs))
                    using (var bdr = new BsonDataReader(br, false, DateTimeKind.Utc))
                    {
                        var ser = new JsonSerializer();
                        var structure = ser.Deserialize<BsonStructure>(bdr);
                        return new Status(0, structure); // 0
                    }
                }
                catch (Exception)
                {
                    return new Status(-1, null, d["Error occured while opening file"],
                        bsonPath, d["for reading."]); // -1
                }
            }
            else return new Status(-2, null, d["File"], bsonPath, d["does not exist."]); // -2
        }

        protected Status Save<BsonStructure>(string bsonPath, BsonStructure data)
        {
            // jeżeli plik nie istnieje, to zostanie stworzony
            try
            {
                using (var fs = File.OpenWrite(bsonPath))
                using (var bw = new BinaryWriter(fs))
                using (var bdw = new BsonDataWriter(bw))
                {
                    var ser = new JsonSerializer();
                    ser.Serialize(bdw, data);
                    return new Status(0); // 0
                }
            }
            catch (Exception)
            {
                return new Status(-1, null, d["Error occured while opening file"],
                    bsonPath, d["for writing."]); // -1
            }
        }
    }
}
