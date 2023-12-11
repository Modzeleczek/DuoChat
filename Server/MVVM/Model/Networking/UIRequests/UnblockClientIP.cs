using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class UnblockClientIP : UIRequest
    {
        #region Properties
        public IPv4Address IpAddress { get; }
        #endregion

        public UnblockClientIP(IPv4Address ipAddress)
        {
            IpAddress = ipAddress;
        }
    }
}
