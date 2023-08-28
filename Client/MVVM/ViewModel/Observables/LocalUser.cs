using Client.MVVM.Model;
using Newtonsoft.Json;
using Shared.MVVM.Core;
using System;
using System.Security;
using System.Security.Cryptography;

namespace Client.MVVM.ViewModel.Observables
{
    /* Nie potrzeba customowego JsonConvertera, bo klasa LocalUser
    nie ma pól o customowych klasach (tylko wbudowane typy). */
    public class LocalUser : ObservableObject
    {
        #region Properties
        // Unikalny identyfikator (klucz główny)
        private string _name = null;
        // JsonProperty, żeby właściwość została BSON-zserializowana, mimo że ma prywatny setter.
        [JsonProperty]
        public string Name
        {
            // Getter musi być publiczny, aby można było bindować nazwę do odczytu w XAMLu.
            get => _name;
            // Nazwa jest obserwowalna przez UI, dlatego setter z OnPropertyChanged.
            private set { _name = value; OnPropertyChanged(); }
        }

        // Sól do skrótu hasła nie jest obserwowalna.
        public byte[] PasswordSalt { get; set; } = null;

        // Skrót hasła nie jest obserwowalny.
        public byte[] PasswordDigest { get; set; } = null;

        // Wektor inicjujący i sól hasła do odszyfrowania bazy danych.
        private byte[] _dbInitializationVector = null;
        public byte[] DbInitializationVector
        {
            get => _dbInitializationVector;
            set
            {
                /* IV musi mieć długość równą długości bloku (dla Rijndaela zgodnego ze
                specyfikacją AESa blok musi być 128-bitowy). */
                if (value.Length != 128 / 8)
                    throw new ArgumentException("Database initialization vector is not 128 bits long.",
                        nameof(value));
                _dbInitializationVector = value;
            }
        }

        private byte[] _dbSalt = null;
        public byte[] DbSalt
        {
            get => _dbSalt;
            set
            {
                // Używamy 128-bitowych kluczy w AES.
                if (value.Length != 128 / 8)
                    throw new ArgumentException("Database salt is not 128 bits long.", nameof(value));
                _dbSalt = value;
            }
        }
        #endregion

        // Do BSON-deserializacji obiektu klasy LocalUser
        public LocalUser() { }

        public LocalUser(LocalUserPrimaryKey key, SecureString password)
        {
            SetPrimaryKey(key);
            ResetPassword(password);
        }

        private byte[] GenerateRandom(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }

        public void ResetPassword(SecureString newPassword)
        {
            var pc = new PasswordCryptography();
            var passwordSalt = GenerateRandom(128 / 8); // 128 b sól
            PasswordSalt = passwordSalt;
            PasswordDigest = pc.ComputeDigest(newPassword, passwordSalt); // 128 b - rozmiar klucza AESa
            DbInitializationVector = GenerateRandom(128 / 8); // 128 b - rozmiar bloku AESa
            DbSalt = GenerateRandom(128 / 8); // 128 b sól
        }

        public LocalUserPrimaryKey GetPrimaryKey()
        {
            return new LocalUserPrimaryKey(Name);
        }

        public void SetPrimaryKey(LocalUserPrimaryKey key)
        {
            Name = key.Name;
        }

        public void CopyTo(LocalUser user)
        {
            user.Name = Name;
            user.PasswordSalt = PasswordSalt;
            user.PasswordDigest = PasswordDigest;
            user.DbInitializationVector = DbInitializationVector;
            user.DbSalt = DbSalt;
        }
    }
}
