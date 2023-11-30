using Client.MVVM.Model;
using Client.MVVM.Model.JsonConverters;
using Newtonsoft.Json;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using System;

namespace Client.MVVM.ViewModel.Observables
{
    public class Server : ObservableObject
    {
        #region Properties
        private string name = null!;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        private IPv4Address ipAddress = null!;
        [JsonProperty, JsonConverter(typeof(IPv4AddressJsonConverter))]
        public IPv4Address IpAddress
        {
            get => ipAddress;
            private set { ipAddress = value; OnPropertyChanged(); }
        }

        private Port port = null!;
        [JsonProperty, JsonConverter(typeof(PortJsonConverter))]
        public Port Port
        {
            get => port;
            private set { port = value; OnPropertyChanged(); }
        }

        public Guid Guid { get; set; } = Guid.Empty;

        [JsonProperty, JsonConverter(typeof(PublicKeyJsonConverter))]
        public PublicKey? PublicKey { get; set; } = null;
        #endregion

        // Do BSON-deserializacji
        public Server() { }

        public Server(ServerPrimaryKey key)
        {
            SetPrimaryKey(key);
        }

        public void CopyTo(Server server)
        {
            server.Name = Name;
            server.IpAddress = IpAddress;
            server.Port = Port;
            server.Guid = Guid;
            server.PublicKey = PublicKey;
        }

        public ServerPrimaryKey GetPrimaryKey()
        {
            return new ServerPrimaryKey(IpAddress, Port);
        }

        public void SetPrimaryKey(ServerPrimaryKey key)
        {
            IpAddress = key.IpAddress;
            Port = key.Port;
        }
    }
}
