using Client.MVVM.Model;
using System;

namespace Client.MVVM.ViewModel
{
    public class DialogViewModel : ViewModel
    {
        public Status Status { get; protected set; } = new Status(1);
        public event EventHandler RequestClose = null;

        protected virtual void OnRequestClose(Status status)
        {
            if (RequestClose != null) RequestClose(this, null);
            Status = status;
        }
    }
}
