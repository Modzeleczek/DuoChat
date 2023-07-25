using Shared.MVVM.ViewModel.Results;
using System;

namespace Shared.MVVM.Model.Networking
{
    public class InterlocutorFailure : Failure
    {
        public InterlocutorFailure(Exception reason, params string[] message)
            : base(reason, message) {}
    }
}
