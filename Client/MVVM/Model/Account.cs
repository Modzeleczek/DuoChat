using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using System;

namespace Client.MVVM.Model
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

        public static Account Random(Random rng) =>
            new Account
            {
                Login = rng.Next().ToString(),
                PrivateKey = null
            };
    }
}
