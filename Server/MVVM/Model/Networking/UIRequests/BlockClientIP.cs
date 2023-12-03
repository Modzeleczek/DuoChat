using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;

namespace Server.MVVM.Model.Networking.UIRequests
{
    internal class BlockClientIP : UIRequest
    {
        #region Properties
        public IPv4Address IPAddress { get; }
        #endregion

        public BlockClientIP(IPv4Address ipAddress)
        {
            IPAddress = ipAddress;
        }
    }
}
