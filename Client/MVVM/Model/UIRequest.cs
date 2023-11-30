using System;
using BaseUIRequest = Shared.MVVM.Model.UIRequest;

namespace Client.MVVM.Model
{
    public class UIRequest : BaseUIRequest
    {
        #region Classes
        public enum Operations : byte
        {
            Disconnect = 0, IntroduceClient, GetConversations
        }
        #endregion

        #region Properties
        public Operations Operation { get; }
        #endregion

        public UIRequest(Operations operation, object? parameter, Action? callback)
            : base(parameter, callback)
        {
            Operation = operation;
        }
    }
}
