using Client.MVVM.Model;
using Newtonsoft.Json;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using System;
using System.Security;

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
                // IV musi mieć długość równą długości bloku
                if (value.Length != Aes.BLOCK_LENGTH)
                    throw new ArgumentException("Database initialization vector " +
                        $"is not {Aes.KEY_LENGTH} bytes long.", nameof(value));
                _dbInitializationVector = value;
            }
        }

        private byte[] _dbSalt = null;
        public byte[] DbSalt
        {
            get => _dbSalt;
            set
            {
                if (value.Length != Aes.KEY_LENGTH)
                    throw new ArgumentException("Database salt is not " +
                        $"{Aes.BLOCK_LENGTH} bytes long.", nameof(value));
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

        public void ResetPassword(SecureString newPassword)
        {
            var passwordSalt = RandomGenerator.Generate(128 / 8); // 128 b sól
            PasswordSalt = passwordSalt;
            // 128 b - rozmiar klucza AESa
            PasswordDigest = PasswordCryptography.ComputeDigest(newPassword, passwordSalt, 128 / 8);
            DbInitializationVector = RandomGenerator.Generate(128 / 8); // 128 b - rozmiar bloku AESa
            DbSalt = RandomGenerator.Generate(128 / 8); // 128 b sól
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
