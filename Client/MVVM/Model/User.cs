using Client.MVVM.Core;
using System.Numerics;

namespace Client.MVVM.Model
{
    public class User : ObservableObject
    {
        public BigInteger PublicKey { get; set; }

        private string nickname;
        public string Nickname
        {
            get => nickname;
            set { nickname = value; OnPropertyChanged(); }
        }

        private string imagePath;
        public string ImagePath
        {
            get => imagePath;
            set { imagePath = value; OnPropertyChanged(); }
        }
    }
}
