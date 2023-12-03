using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class UnblockClientIP : UIRequest
    {
        #region Properties
        public IPv4Address IpAddress { get; }
        public Action Callback { get; }
        #endregion

        public UnblockClientIP(IPv4Address ipAddress, Action callback)
        {
            IpAddress = ipAddress;
            Callback = callback;
        }
    }
}
