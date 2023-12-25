using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;

namespace Client.MVVM.ViewModel.Observables
{
    public class User : ObservableObject
    {
        public ulong Id { get; set; } = 0;

        private string _login = null!;
        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(); }
        }

        public PublicKey PublicKey { get; set; } = null!;

        private bool _isBlocked = false;
        public bool IsBlocked
        {
            get => _isBlocked;
            set { _isBlocked = value; OnPropertyChanged(); }
        }
    }
}
