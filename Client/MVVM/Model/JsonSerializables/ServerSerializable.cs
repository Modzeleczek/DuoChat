using Shared.MVVM.Model.Networking;
using System;

namespace Client.MVVM.Model.JsonSerializables
{
    public class ServerSerializable
    {
        #region Properties
        public string Name { get; set; }
        public int IpAddress { get; set; }
        public ushort Port { get; set; }
        public Guid Guid { get; set; }
        public byte[] PublicKey { get; set; }
        #endregion

        public override bool Equals(object obj)
        {
            if (!(obj is ServerSerializable)) return false;
            var server = (ServerSerializable)obj;
            return KeyEquals(server.IpAddress, server.Port);
        }

        public override int GetHashCode() => base.GetHashCode();

        public bool KeyEquals(int ipAddress, ushort port) =>
            IpAddress == ipAddress && Port == port;

        public Server ToObservable() =>
            new Server
            {
                Name = Name,
                IpAddress = new IPv4Address(IpAddress),
                Port = new Port(Port),
                Guid = Guid,
                PublicKey = PublicKey != null ?
                    Shared.MVVM.Model.Cryptography.PublicKey.FromBytes(PublicKey) : null,
            };
    }
}
