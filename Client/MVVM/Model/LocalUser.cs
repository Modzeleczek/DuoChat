using Client.MVVM.Core;
using Client.MVVM.Model.BsonStorages;
using System;
using System.IO;

namespace Client.MVVM.Model
{
    public class LocalUser : ObservableObject
    {
        private string name; // unikalny identyfikator lokalnego użytkownika w bazie;
        // nazwa jest obserwowalna przez UI, dlatego setter z OnPropertyChanged
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }

        public byte[] PasswordSalt { get; set; } // sól do skrótu hasła nie jest obserwowalna
        public byte[] PasswordDigest { get; set; } // skrót hasła nie jest obserwowalny

        // wektor inicjujący i sól hasła do odszyfrowania bazy danych
        public byte[] DBInitializationVector { get; set; }
        public byte[] DBSalt { get; set; }

        public LocalUser(string name, byte[] passwordSalt, byte[] passwordDigest,
            byte[] dbInitializationVector, byte[] dbSalt)
        {
            Name = name;
            PasswordSalt = passwordSalt;
            PasswordDigest = passwordDigest;
            // IV musi mieć długość równą długości bloku (dla Rijndaela zgodnego ze
            // specyfikacją AESa blok musi być 128-bitowy)
            if (dbInitializationVector.Length != 128 / 8)
                throw new ArgumentException("Database initialization vector is not 128 bits long.");
            DBInitializationVector = dbInitializationVector;
            // używamy też 128-bitowych kluczy w AES
            if (dbSalt.Length != 128 / 8)
                throw new ArgumentException("Database salt is not 128 bits long.");
            DBSalt = dbSalt;
        }

        public LocalUser() { }

        private string GetDirectoryPath() =>
            Path.Combine(LocalUsersStorage.USERS_DIRECTORY_PATH, Name);

        public UserDatabase GetDatabase() => new UserDatabase(GetDirectoryPath());
    }
}
