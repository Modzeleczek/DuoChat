using Shared.MVVM.Model;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class DisconnectAccount : UIRequest
    {
        #region Properties
        public string Login { get; }
        public Action Callback { get; }
        #endregion

        public DisconnectAccount(string login, Action callback)
        {
            Login = login;
            Callback = callback;
        }
    }
}
