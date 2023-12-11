using Shared.MVVM.Model;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class DisconnectAccount : UIRequest
    {
        #region Properties
        public string Login { get; }
        #endregion

        public DisconnectAccount(string login)
        {
            Login = login;
        }
    }
}
