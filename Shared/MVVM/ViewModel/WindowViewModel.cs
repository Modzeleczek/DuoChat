using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
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
            Close = new RelayCommand(e => OnRequestClose(new Cancellation()));
        }

        public Result Result { get; protected set; } = new Cancellation();
        public event Action RequestClose = null;

        private readonly object _closeLock = new object();
        private bool _closed = false;

        protected void OnRequestClose(Result result)
        {
            lock (_closeLock)
            {
                if (_closed) return;
                _closed = true;
                Result = result;
                /* używamy typu Action jako handlerów eventu RequestClose, bo we
                wszystkich miejscach w programie status viewmodelu pobieramy z
                gettera Result, a nie poprzez parametr handlera eventu RequestClose */
                if (RequestClose != null) RequestClose();
            }
        }

        public void Cancel()
        {
            // Dla wywołującego viewmodelu.
            OnRequestClose(new Cancellation());
        }
    }
}
