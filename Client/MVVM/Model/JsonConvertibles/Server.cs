using Shared.MVVM.Model;
using System;
using System.Numerics;

namespace Client.MVVM.Model.JsonConvertibles
{
    public class Server
    {
        public Guid GUID { get; set; }
        public BigInteger PublicKey { get; set; }
        public int IpAddress { get; set; }
        public ushort Port { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is Server)) return base.Equals(obj);
            var ser = (Server)obj;
            // return KeyEquals(ser.IpAddress, ser.Port);
            return KeyEquals(ser.GUID);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool KeyEquals(Guid guid)
        {
            // return IpAddress.Equals(ipAddress) && Port == port;
            return GUID == guid;
        }

        public XamlObservables.Server ToObservable() =>
            new XamlObservables.Server
            {
                GUID = GUID,
                PublicKey = PublicKey,
                IpAddress = new IPv4Address(IpAddress),
                Port = Port,
                Name = Name
            };
    }
}
