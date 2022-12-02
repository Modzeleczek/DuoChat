using Client.MVVM.Core;
using Shared.Cryptography;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.MVVM.Model
{
    public class Account : ObservableObject
    {
        private string login;
        public string Login
        {
            get => login;
            set { login = value; OnPropertyChanged(); }
        }

        public string Password { get; set; }

        public RSA.KeyPair Keys { get; set; }

        private string nickname;
        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
        }

        public WriteableBitmap Image { get; set; }

        public static Account Random(Random rng) =>
            new Account
            {
                Login = rng.Next().ToString(),
                Password = rng.Next().ToString(),
                Keys = new RSA.KeyPair(rng.Next(), rng.Next(), rng.Next()),
                Nickname = rng.Next().ToString(),
                Image = new WriteableBitmap(100, 100, 96, 96, PixelFormats.Bgra32, null)
            };
    }
}
