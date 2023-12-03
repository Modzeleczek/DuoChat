using Shared.MVVM.Model;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class StopServer : UIRequest
    {
        #region Properties
        public Action Callback { get; }
        #endregion

        public StopServer(Action callback)
        {
            Callback = callback;
        }
    }
}
