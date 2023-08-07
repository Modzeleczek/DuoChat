using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;

namespace Client.MVVM.ViewModel.Observables
{
    public class Account : ObservableObject
    {
        #region Properties
        private string login;
        public string Login
        {
            get => login;
            set { login = value; OnPropertyChanged(); }
        }

        public PrivateKey PrivateKey { get; set; }
        #endregion

        public bool KeyEquals(string login) => Login == login;

        public void CopyTo(Account account)
        {
            account.Login = login;
            account.PrivateKey = PrivateKey;
        }
    }
}
