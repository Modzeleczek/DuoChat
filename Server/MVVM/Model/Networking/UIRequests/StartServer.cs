using Shared.MVVM.Model;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class StartServer : UIRequest
    {
        #region Properties
        public Guid Guid { get; }
        public PrivateKey PrivateKey { get; }
        public IPv4Address IpAddress { get; }
        public Port Port { get; }
        public int Capacity { get; }

        // Do callbacku przekazujemy null lub komunikat błędu.
        public Action<string?> Callback { get; }
        #endregion

        public StartServer(Guid guid, PrivateKey privateKey, IPv4Address ipAddress, Port port,
            int capacity, Action<string?> callback)
        {
            Guid = guid;
            PrivateKey = privateKey;
            IpAddress = ipAddress;
            Port = port;
            Capacity = capacity;

            Callback = callback;
        }
    }
}
