using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Client.MVVM.ViewModel.Observables
{
    public class Account : ObservableObject, IDto<string>
    {
        #region Properties
        // Id otrzymane od serwera w pakiecie Authentication. Nie przechowujemy go w bazie danych klienta.
        public ulong RemoteId { get; set; }

        private string _login = null!;
        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(); }
        }

        public PrivateKey PrivateKey { get; set; } = null!;
        #endregion

        public void CopyTo(Account account)
        {
            account.Login = _login;
            account.PrivateKey = PrivateKey;
        }

        public string GetRepositoryKey()
        {
            return Login;
        }
    }
}
