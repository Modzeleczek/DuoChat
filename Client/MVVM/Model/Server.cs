using Client.MVVM.Model.JsonConverters;
using Newtonsoft.Json;
using Shared.MVVM.Core;
using System;
using System.Net;
using System.Numerics;

namespace Client.MVVM.Model
{
    public class Server : ObservableObject
    {
        public Guid GUID { get; set; }

        public BigInteger PublicKey { get; set; }

        private IPAddress ipAddress;
        [JsonConverter(typeof(IPAddressConverter))]
        public IPAddress IpAddress
        {
            get => ipAddress;
            set { ipAddress = value; OnPropertyChanged(); }
        }

        private ushort port;
        public ushort Port
        {
            get => port;
            set { port = value; OnPropertyChanged(); }
        }

        private string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        public static Server Random(Random rng) =>
            new Server
            {
                GUID = Guid.NewGuid(),
                PublicKey = new BigInteger(rng.Next()),
                IpAddress = new IPAddress(rng.Next()),
                Port = (ushort)rng.Next(),
                Name = rng.Next().ToString()
            };

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
    }
}
