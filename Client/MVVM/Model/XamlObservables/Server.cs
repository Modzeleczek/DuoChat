using Shared.MVVM.Core;
using System;
using System.Numerics;

namespace Client.MVVM.Model.XamlObservables
{
    public class Server : ObservableObject
    {
        public Guid GUID { get; set; }

        public BigInteger PublicKey { get; set; }

        private IPv4Address ipAddress;
        public IPv4Address IpAddress
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
                IpAddress = new IPv4Address(rng.Next()),
                Port = (ushort)rng.Next(),
                Name = rng.Next().ToString()
            };

        public JsonConvertibles.Server ToSerializable() =>
            new JsonConvertibles.Server
            {
                GUID = GUID,
                PublicKey = PublicKey,
                IpAddress = IpAddress.BinaryRepresentation,
                Port = Port,
                Name = Name
            };
    }
}
