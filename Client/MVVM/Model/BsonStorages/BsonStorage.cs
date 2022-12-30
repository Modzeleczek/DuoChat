using Newtonsoft.Json.Bson;
using Newtonsoft.Json;
using System;
using System.IO;
using Client.MVVM.View.Localization;

namespace Client.MVVM.Model.BsonStorages
{
    public class BsonStorage
    {
        private readonly string path;
        protected static readonly ClientTranslator d = ClientTranslator.Instance;

        protected BsonStorage(string path)
        {
            this.path = path;
        }

        protected BsonStructure Load<BsonStructure>() where BsonStructure : new()
        {
            BsonStructure ret;
            if (File.Exists(path))
            {
                using (var br = new BinaryReader(File.OpenRead(path)))
                using (var bdr = new BsonDataReader(br, false, DateTimeKind.Utc))
                {
                    var ser = new JsonSerializer();
                    ret = ser.Deserialize<BsonStructure>(bdr);
                }
            }
            else ret = new BsonStructure();
            return ret;
        }

        protected void Save<BsonStructure>(BsonStructure data)
        {
            // jeżeli plik nie istnieje, to zostanie stworzony
            using (var bw = new BinaryWriter(File.OpenWrite(path)))
            using (var bdw = new BsonDataWriter(bw))
            {
                var ser = new JsonSerializer();
                ser.Serialize(bdw, data);
            }
        }
    }
}
