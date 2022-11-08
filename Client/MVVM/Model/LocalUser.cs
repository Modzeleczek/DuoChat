using Client.MVVM.Core;

namespace Client.MVVM.Model
{
    public class LocalUser : ObservableObject
    {
        #region Properties
        private string name; // nazwa jest obserwowalna przez UI, dlatego setter z OnPropertyChanged
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }

        public byte[] PasswordDigest { get; set; } // skrót hasła nie jest obserwowalny
        #endregion
    }
}
