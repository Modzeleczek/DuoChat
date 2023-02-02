using Client.MVVM.Model.JsonSerializables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using System;

namespace Client.MVVM.Model
{
    public class Server : ObservableObject
    {
        #region Properties
        private string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        private IPv4Address ipAddress;
        public IPv4Address IpAddress
        {
            get => ipAddress;
            set { ipAddress = value; OnPropertyChanged(); }
        }

        private Port port;
        public Port Port
        {
            get => port;
            set { port = value; OnPropertyChanged(); }
        }

        public Guid Guid { get; set; }

        public PublicKey PublicKey { get; set; }
        #endregion

        public bool KeyEquals(IPv4Address ipAddress, Port port) =>
            IpAddress.Equals(ipAddress) && Port.Equals(port);

        public ServerSerializable ToSerializable() =>
            new ServerSerializable
            {
                Name = Name,
                IpAddress = IpAddress.BinaryRepresentation,
                Port = Port.Value,
                Guid = Guid,
                PublicKey = PublicKey?.ToBytes(),
            };

        public void CopyTo(Server server)
        {
            server.Name = Name;
            server.IpAddress = IpAddress;
            server.Port = Port;
            server.Guid = Guid;
            server.PublicKey = PublicKey;
        }
    }
}
