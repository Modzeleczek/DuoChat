using Client.MVVM.Core;
using Shared.Cryptography;
using System;

namespace Client.MVVM.Model
{
    public class Server : ObservableObject
    {
        public Guid GUID { get; set; }

        public Rsa.Key<int> PublicKey { get; set; }

        public string ipAddress;
        public string IpAddress
        {
            get => ipAddress;
            set { ipAddress = value; OnPropertyChanged(); }
        }

        public ushort port;
        public ushort Port
        {
            get => port;
            set { port = value; OnPropertyChanged(); }
        }

        public string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }
    }
}
