using Shared.MVVM.Model;
using Shared.MVVM.Model.Cryptography;
using System;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class Connect : UIRequest
    {
        #region Properties
        public ServerPrimaryKey ServerKey { get; }
        public string Login { get; }
        public PrivateKey PrivateKey { get; }
        
        // Do callbacku przekazujemy null lub komunikat błędu.
        public Action<string?> Callback { get; }
        #endregion

        public Connect(ServerPrimaryKey serverKey, string login, PrivateKey privateKey,
            Action<string?> callback)
        {
            ServerKey = serverKey;
            Login = login;
            PrivateKey = privateKey;

            Callback = callback;
        }
    }
}
