using Shared.MVVM.Model;
using System;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class Disconnect : UIRequest
    {
        #region Properties
        public ServerPrimaryKey ServerKey { get; }
        public Action? Callback { get; }
        #endregion

        public Disconnect(ServerPrimaryKey serverKey, Action? callback)
        {
            ServerKey = serverKey;
            Callback = callback;
        }
    }
}
