using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Converters;
using System;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class DialogViewModel : ObservableObject
    {
        #region Commands
        public RelayCommand WindowLoaded { get; protected set; }
        #endregion

        protected Window window;
        public Status Status { get; protected set; } = new Status(1);
        public event EventHandler RequestClose = null;
        protected readonly Strings d = Strings.Instance;

        protected virtual void OnRequestClose(Status status)
        {
            if (RequestClose != null) RequestClose(this, null);
            Status = status;
        }
    }
}
