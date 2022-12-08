using Shared.Cryptography;
using Shared.MVVM.Core;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.MVVM.Model
{
    public class User : ObservableObject
    {
        public RSA.Key<int> PublicKey { get; set; }

        private string nickname;
        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
        }

        private WriteableBitmap image;
        public WriteableBitmap Image
        {
            get => image;
            set { image = value; OnPropertyChanged(); }
        }

        public static User Random(Random rng) =>
            new User
            {
                PublicKey = new RSA.Key<int>(rng.Next(), rng.Next()),
                Nickname = rng.Next().ToString(),
                Image = new WriteableBitmap(100, 100, 96, 96, PixelFormats.Bgra32, null)
            };
    }
}
