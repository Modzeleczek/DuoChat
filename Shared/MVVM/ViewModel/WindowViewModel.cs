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
        public event Action RequestClose = null;

        protected void OnRequestClose(Status status)
        {
            Status = status;
            /* używamy typu Action jako handlerów eventu RequestClose, bo we
            wszystkich miejscach w programie status viewmodelu pobieramy z
            settera Status, a nie poprzez parametr handlera eventu RequestClose */
            if (RequestClose != null) RequestClose();
        }
    }
}
