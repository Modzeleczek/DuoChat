using Client.MVVM.Core;
using Shared.Cryptography;
using System;
using System.Net;

namespace Client.MVVM.Model
{
    public class Server : ObservableObject
    {
        public Guid GUID { get; set; }

        public RSA.Key<int> PublicKey { get; set; }

        private IPAddress ipAddress;
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
                PublicKey = new RSA.Key<int>(rng.Next(), rng.Next()),
                IpAddress = new IPAddress(rng.Next()),
                Port = (ushort)rng.Next(),
                Name = rng.Next().ToString()
            };
    }
}
