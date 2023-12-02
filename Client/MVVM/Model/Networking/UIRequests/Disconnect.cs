using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class Disconnect : UIRequest
    {
        #region Properties
        public ServerPrimaryKey ServerKey { get; }
        #endregion

        public Disconnect(ServerPrimaryKey serverKey)
        {
            ServerKey = serverKey;
        }
    }
}
