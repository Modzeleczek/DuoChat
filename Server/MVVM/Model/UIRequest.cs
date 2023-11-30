using System;
using System.Threading;
using BaseUIRequest = Shared.MVVM.Model.UIRequest;

namespace Server.MVVM.Model
{
    public class UIRequest : BaseUIRequest
    {
        #region Classes
        public enum Operations : byte
        {
            StopServer, DisconnectClient, BlockClientIP, UnblockClientIP, BlockAccount,
            UnblockAccount
        }
        #endregion

        #region Properties
        public Operations Operation { get; }
        #endregion

        public UIRequest(Operations operation, object? parameter, Action? callback,
            int millisecondsTimeout = Timeout.Infinite)
            : base(parameter, callback, millisecondsTimeout)
        {
            Operation = operation;
        }
    }
}
