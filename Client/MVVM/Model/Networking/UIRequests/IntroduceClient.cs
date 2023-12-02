using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class IntroduceClient : UIRequest
    {
        #region Properties
        public ServerPrimaryKey ServerKey { get; }
        #endregion

        public IntroduceClient(ServerPrimaryKey serverKey)
        {
            ServerKey = serverKey;
        }
    }
}
