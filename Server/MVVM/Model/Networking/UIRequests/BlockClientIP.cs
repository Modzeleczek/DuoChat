using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class BlockClientIP : UIRequest
    {
        #region Properties
        public IPv4Address IpAddress { get; }
        public Action<string?> Callback { get; }
        #endregion

        public BlockClientIP(IPv4Address ipAddress, Action<string?> callback)
        {
            IpAddress = ipAddress;
            Callback = callback;
        }
    }
}
