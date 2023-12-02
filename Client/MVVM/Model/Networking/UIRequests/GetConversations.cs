using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class GetConversations : UIRequest
    {
        #region Properties
        public ServerPrimaryKey ServerKey { get; }
        #endregion

        public GetConversations(ServerPrimaryKey serverKey)
        {
            ServerKey = serverKey;
        }
    }
}
