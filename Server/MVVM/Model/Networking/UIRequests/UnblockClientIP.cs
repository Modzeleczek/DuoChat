using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class UnblockClientIP : UIRequest
    {
        #region Properties
        public IPv4Address IPAddress { get; }
        #endregion

        public UnblockClientIP(IPv4Address ipAddress)
        {
            IPAddress = ipAddress;
        }
    }
}
