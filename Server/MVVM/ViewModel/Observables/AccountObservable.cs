using Shared.MVVM.Core;

namespace Server.MVVM.ViewModel.Observables
{
    public class AccountObservable : ObservableObject
    {
        #region Properties
        private string _login = string.Empty;
        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(); }
        }

        private bool _isBlocked = false;
        public bool IsBlocked
        {
            get => _isBlocked;
            set { _isBlocked = value; OnPropertyChanged(); }
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }
        #endregion
    }
}
