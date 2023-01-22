using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public string Password { get; set; }

        public PrivateKey PrivateKey { get; set; }

        private string nickname;
        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
        }

        public WriteableBitmap Image { get; set; }
        #endregion

        public static Account Random(Random rng) =>
            new Account
            {
                Login = rng.Next().ToString(),
                Password = rng.Next().ToString(),
                PrivateKey = null,
                Nickname = rng.Next().ToString(),
                Image = new WriteableBitmap(100, 100, 96, 96, PixelFormats.Bgra32, null)
            };
    }
}
