using Client.MVVM.Model.JsonSerializables;
using Shared.MVVM.Model;
using System;
using System.Collections.Generic;

namespace Client.MVVM.Model.BsonStorages
{
    public class ServersStorage : BsonStorage
    {
        public ServersStorage(string path) : base(path) { }

        private class BsonStructure
        {
            public List<ServerSerializable> Servers { get; set; } = new List<ServerSerializable>();
        }

        private BsonStructure Load() => Load<BsonStructure>();

        private void Save(BsonStructure servers) => Save<BsonStructure>(servers);

        public Status Add(ServerSerializable server)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            if (Exists(server.Guid, servers))
                return AlreadyExistsStatus(server.Guid);
            servers.Add(server);
            Save(fileStr);
            return new Status(0);
        }

        private Status AlreadyExistsStatus(Guid guid) =>
            new Status(2, d["Server with GUID"] + $" {guid} " + d["already exists."]);

        public List<ServerSerializable> GetAll() => Load().Servers;

        private bool Exists(Guid guid, List<ServerSerializable> servers)
        {
            for (int i = 0; i < servers.Count; ++i)
                if (servers[i].KeyEquals(guid))
                    return true;
            return false;
        }

        public bool Exists(Guid guid) =>
            Exists(guid, Load().Servers);

        public Status Get(Guid guid)
        {
            var servers = Load().Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                var s = servers[i];
                if (s.KeyEquals(guid))
                    return new Status(0, null, s);
            }
            return DoesNotExistStatus(guid);
        }

        private Status DoesNotExistStatus(Guid guid) =>
            new Status(1, d["Server with GUID"] + $" {guid} " + d["does not exist."]);

        public Status Update(Guid guid, ServerSerializable server)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].KeyEquals(guid))
                {
                    int j;
                    for (j = 0; j < i; ++j)
                        if (servers[j].Equals(server))
                            return AlreadyExistsStatus(guid);
                    for (j = i + 1; j < servers.Count; ++j)
                        if (servers[j].Equals(server))
                            return AlreadyExistsStatus(guid);
                    servers[i] = server;
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return DoesNotExistStatus(guid);
        }

        public Status Delete(Guid guid)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].KeyEquals(guid))
                {
                    servers.RemoveAt(i);
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return DoesNotExistStatus(guid);
        }
    }
}
