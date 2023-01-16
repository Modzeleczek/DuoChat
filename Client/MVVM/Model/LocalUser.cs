using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model.JsonSerializables;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.View.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Client.MVVM.Model
{
    public class LocalUser : ObservableObject
    {
        #region Properties
        private string _name; // unikalny identyfikator
        // nazwa jest obserwowalna przez UI, dlatego setter z OnPropertyChanged
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public byte[] PasswordSalt { get; set; } // sól do skrótu hasła nie jest obserwowalna

        public byte[] PasswordDigest { get; set; } // skrót hasła nie jest obserwowalny

        // wektor inicjujący i sól hasła do odszyfrowania bazy danych
        public byte[] DbInitializationVector { get; set; }

        public byte[] DbSalt { get; set; }

        public string DirectoryPath => Path.Combine(LocalUsersStorage.USERS_DIRECTORY_PATH, Name);

        private ServersStorage _serversStorage =>
            new ServersStorage(Path.Combine(DirectoryPath, "servers.bson"));
        #endregion

        public LocalUser() { }

        public LocalUser(string name, SecureString password)
        {
            Name = name;
            ResetPassword(password);
        }

        public LocalUserSerializable ToSerializable() =>
            new LocalUserSerializable
            {
                Name = Name,
                PasswordSalt = PasswordSalt,
                PasswordDigest = PasswordDigest,
                DbInitializationVector = DbInitializationVector,
                DbSalt = DbSalt
            };

        public bool DirectoryExists() => Directory.Exists(DirectoryPath);

        public void DeleteDirectory() => Directory.Delete(DirectoryPath, true);

        public void CreateDirectory() => Directory.CreateDirectory(DirectoryPath);

        public Status Rename(string newName)
        {
            var d = Translator.Instance;
            var oldDirectoryPath = DirectoryPath;
            if (!Directory.Exists(oldDirectoryPath))
                return new Status(-1, d["User's directory does not exist."]);
            var oldName = Name;
            try
            {
                Name = newName;
                var newDirectoryPath = DirectoryPath;
                if (Directory.Exists(newDirectoryPath))
                    return new Status(-2, d["A directory with user's new name already exists."]);
                Directory.Move(oldDirectoryPath, DirectoryPath);
                return new Status(0);
            }
            catch (Exception)
            {
                Name = oldName;
                return new Status(-3, d["Error occured while renaming user's directory."]);
            }
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

        public Status AddServer(Server server) => _serversStorage.Add(server.ToSerializable());

        public List<Server> GetAllServers() =>
            _serversStorage.GetAll().Select(e => e.ToObservable()).ToList();

        public bool ServerExists(Guid guid) => _serversStorage.Exists(guid);

        public Status DeleteServer(Guid guid) => _serversStorage.Delete(guid);
    }
}
