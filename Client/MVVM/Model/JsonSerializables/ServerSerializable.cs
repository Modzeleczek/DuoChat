using Shared.MVVM.Model.Networking;
using System;

namespace Client.MVVM.Model.JsonSerializables
{
    public class ServerSerializable
    {
        #region Properties
        public Guid Guid { get; set; }
        public byte[] PublicKey { get; set; }
        public int IpAddress { get; set; }
        public ushort Port { get; set; }
        public string Name { get; set; }
        #endregion

        public override bool Equals(object obj)
        {
            if (!(obj is ServerSerializable)) return false;
            var ser = (ServerSerializable)obj;
            return KeyEquals(ser.IpAddress, ser.Port);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool KeyEquals(int ipAddress, ushort port)
        {
            return IpAddress == ipAddress && Port == port;
        }

        public Server ToObservable() =>
            new Server
            {
                Guid = Guid,
                PublicKey = new Shared.MVVM.Model.Cryptography.PublicKey(PublicKey),
                IpAddress = new IPv4Address(IpAddress),
                Port = new Port(Port),
                Name = Name
            };
    }
}
