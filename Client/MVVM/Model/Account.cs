using Client.MVVM.Core;
using Shared.Cryptography;
using System.Windows.Media.Imaging;

namespace Client.MVVM.Model
{
    public class Account : ObservableObject
    {
        public string login;
        public string Login
        {
            get => login;
            set { login = value; OnPropertyChanged(); }
        }

        public string Password { get; set; }

        public Rsa.KeyPair Keys { get; set; }

        public string nickname;
        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
        }

        public WriteableBitmap Avatar { get; set; }
    }
}
