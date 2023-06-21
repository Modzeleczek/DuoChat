using Shared.MVVM.Core;
using System;

namespace Shared.MVVM.ViewModel.Results
{
    public class Failure : Result
    {
        public Error Reason { get; }

        public Failure(Exception reason, string message)
        {
            Reason = new Error(reason, message);
        }

        public Failure(string message)
        {
            Reason = new Error(message);
        }
    }
}
