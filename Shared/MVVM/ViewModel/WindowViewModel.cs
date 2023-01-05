using Shared.MVVM.Core;
using Shared.MVVM.Model;
using System;

namespace Shared.MVVM.ViewModel
{
    public class WindowViewModel : ViewModel
    {
        #region Commands
        private RelayCommand _windowLoaded;
        public RelayCommand WindowLoaded
        {
            get => _windowLoaded;
            protected set { _windowLoaded = value; OnPropertyChanged(); }
        }

        private RelayCommand _close;
        public RelayCommand Close
        {
            get => _close;
            protected set { _close = value; OnPropertyChanged(); }
        }
        #endregion

        protected WindowViewModel()
        {
            Close = new RelayCommand(e => OnRequestClose(new Status(1)));
        }

        public Status Status { get; protected set; } = new Status(1);
        public event EventHandler RequestClose = null;

        protected virtual void OnRequestClose(Status status)
        {
            if (RequestClose != null) RequestClose(this, null);
            Status = status;
        }
    }
}
