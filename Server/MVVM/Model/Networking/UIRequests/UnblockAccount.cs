using Shared.MVVM.Model;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class UnblockAccount : UIRequest
    {
        #region Properties
        public string Login { get; }
        public Action Callback { get; }
        #endregion

        public UnblockAccount(string login, Action callback)
        {
            Login = login;
            Callback = callback;
        }
    }
}
