using Client.MVVM.Model.JsonSerializables;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using System.Linq;

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

        public Status Add(Server server)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            if (Exists(server.IpAddress, server.Port, servers))
                return AlreadyExistsStatus(server.IpAddress, server.Port);
            servers.Add(server.ToSerializable());
            Save(fileStr);
            return new Status(0);
        }

        private Status AlreadyExistsStatus(IPv4Address ipAddress, Port port) =>
            new Status(2, d["Server with IP address"] + $" {ipAddress} " +
                d["and port"] + $" {port} " + d["already exists."]);

        public List<Server> GetAll() =>
            Load().Servers.Select(e => e.ToObservable()).ToList();

        private bool Exists(IPv4Address ipAddress, Port port, List<ServerSerializable> servers)
        {
            for (int i = 0; i < servers.Count; ++i)
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    return true;
            return false;
        }

        public bool Exists(IPv4Address ipAddress, Port port) =>
            Exists(ipAddress, port, Load().Servers);

        public Status Get(IPv4Address ipAddress, Port port)
        {
            var servers = Load().Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                var s = servers[i];
                if (s.KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                    return new Status(0, null, s);
            }
            return DoesNotExistStatus(ipAddress, port);
        }

        private Status DoesNotExistStatus(IPv4Address ipAddress, Port port) =>
            new Status(1, d["Server with IP address"] + $" {ipAddress} " +
                d["and port"] + $" {port} " + d["does not exist."]);

        public Status Update(IPv4Address ipAddress, Port port, ServerSerializable server)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                {
                    int j;
                    for (j = 0; j < i; ++j)
                        if (servers[j].Equals(server))
                            return AlreadyExistsStatus(ipAddress, port);
                    for (j = i + 1; j < servers.Count; ++j)
                        if (servers[j].Equals(server))
                            return AlreadyExistsStatus(ipAddress, port);
                    servers[i] = server;
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return DoesNotExistStatus(ipAddress, port);
        }

        public Status Delete(IPv4Address ipAddress, Port port)
        {
            var fileStr = Load();
            var servers = fileStr.Servers;
            for (int i = 0; i < servers.Count; ++i)
            {
                if (servers[i].KeyEquals(ipAddress.BinaryRepresentation, port.Value))
                {
                    servers.RemoveAt(i);
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return DoesNotExistStatus(ipAddress, port);
        }
    }
}
