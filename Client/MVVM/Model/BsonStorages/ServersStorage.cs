using System;
using System.Collections.Generic;

namespace Client.MVVM.Model.BsonStorages
{
    public class ServersStorage : BsonStorage
    {
        public ServersStorage(string path) : base(path) { }

        private class BsonStructure
        {
            public List<Server> Servers { get; set; } = new List<Server>();
        }

        private BsonStructure Load() => Load<BsonStructure>();

        private void Save(BsonStructure servers) => Save<BsonStructure>(servers);

        public Status Add(Server server)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            if (Exists(server.GUID, servers))
                return new Status(1, d["Server with GUID"] + $" '{server.GUID}' " + d["already exists."]);
            servers.Add(server);
            Save(fileStr);
            return new Status(0);
        }

        public List<Server> GetAll() => Load().Servers;

        private bool Exists(Guid guid, List<Server> servers)
        {
            for (int i = 0; i < servers.Count; ++i)
                if (servers[i].GUID == guid)
                    return true;
            return false;
        }

        public bool Exists(Guid guid) => Exists(guid, Load().Servers);

        public Status Get(Guid guid)
        {
            var servers = Load().Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                var s = servers[i];
                if (s.GUID == guid)
                    return new Status(0, null, s);
            }
            return new Status(1,
                d["Server with GUID"] + $" '{guid}' " + d["does not exist."], null);
        }

        public Status Update(Guid guid, Server server)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].GUID == guid)
                {
                    string error = d["Server with GUID"] + $" '{server.GUID}' " + d["already exists."];
                    int j;
                    for (j = 0; j < i; ++j)
                        if (servers[j].GUID == server.GUID)
                            return new Status(2, error);
                    for (j = i + 1; j < servers.Count; ++j)
                        if (servers[j].GUID == server.GUID)
                            return new Status(2, error);
                    servers[i] = server;
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return new Status(1, d["Server with GUID"] + $" '{guid}' " + d["does not exist."]);
        }

        public Status Delete(Guid guid)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                var s = servers[i];
                if (s.GUID == guid)
                {
                    servers.RemoveAt(i);
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return new Status(1, d["Server with GUID"] + $" '{guid}' " + d["does not exist."]);
        }
    }
}
