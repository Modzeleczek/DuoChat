using Shared.MVVM.Model;
using System;

namespace Server.MVVM.Model.Networking.UIRequests
{
    public class StopProcess : UIRequest
    {
        #region Properties
        public Action? Callback { get; }
        #endregion

        public StopProcess(Action? callback)
        {
            Callback = callback;
        }
    }
}
