using Client.MVVM.Core;

namespace Client.MVVM.Model
{
    public class LocalUser : ObservableObject
    {
        private string name; // unikalny identyfikator lokalnego użytkownika w bazie;
        // nazwa jest obserwowalna przez UI, dlatego setter z OnPropertyChanged
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }

        public byte[] Salt { get; set; } // sól do skrótu hasła nie jest obserwowalna
        public byte[] Digest { get; set; } // skrót hasła nie jest obserwowalny

        public LocalUser(string name, byte[] salt, byte[] digest)
        {
            Name = name;
            Salt = salt;
            Digest = digest;
        }

        public LocalUser() { }
    }
}
