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
        public Guid Guid { get; set; }

        public PublicKey PublicKey { get; set; }

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

        private string name;
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }
        #endregion

        public ServerSerializable ToSerializable() =>
            new ServerSerializable
            {
                Guid = Guid,
                PublicKey = PublicKey?.ToBytes(),
                IpAddress = IpAddress.BinaryRepresentation,
                Port = Port.Value,
                Name = Name
            };
    }
}
