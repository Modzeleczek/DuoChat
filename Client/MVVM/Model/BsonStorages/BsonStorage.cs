using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using System;
using System.IO;
using Shared.MVVM.Core;

namespace Client.MVVM.Model.BsonStorages
{
    public class BsonStorage
    {
        protected BsonStructure Load<BsonStructure>(string bsonPath) where BsonStructure : new()
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
                        return structure;
                    }
                }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while opening file| " +
                        $"{bsonPath} |for reading.|");
                }
            }
            else throw new Error($"|File| {bsonPath} |does not exist.|");
        }

        protected void Save<BsonStructure>(string bsonPath, BsonStructure data)
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
                    return;
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while opening file| " +
                    $"{bsonPath} |for writing.|");
            }
        }
    }
}
