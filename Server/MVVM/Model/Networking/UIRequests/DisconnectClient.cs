using Shared.MVVM.Model;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class DisconnectClient : UIRequest
    {
        #region Properties
        public ClientPrimaryKey ClientKey { get; }
        #endregion

        public DisconnectClient(ClientPrimaryKey clientKey)
        {
            ClientKey = clientKey;
        }
    }
}
