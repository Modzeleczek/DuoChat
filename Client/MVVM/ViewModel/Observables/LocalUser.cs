using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model.JsonSerializables;
using Client.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace Client.MVVM.ViewModel.Observables
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

        public string DirectoryPath => Path.Combine(LocalUsersStorage.USERS_DIRECTORY_NAME, Name);

        private ServersStorage serversStorage => new ServersStorage(this);
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

        public void AddServer(Server server) =>
            serversStorage.Add(server);

        public List<Server> GetAllServers() =>
            serversStorage.GetAll();

        public bool ServerExists(IPv4Address ipAddress, Port port) =>
            serversStorage.Exists(ipAddress, port);

        public void UpdateServer(IPv4Address ipAddress, Port port, Server server) =>
            serversStorage.Update(ipAddress, port, server);

        public void DeleteServer(IPv4Address ipAddress, Port port) =>
            serversStorage.Delete(ipAddress, port);

        private ServerDatabase GetServerDatabase(IPv4Address ipAddress, Port port)
        {
            try { return serversStorage.GetServerDatabase(ipAddress, port); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| |getting| " +
                    "|access to server's database.|");
                throw;
            }
        }

        public List<Account> GetAllAccounts(IPv4Address ipAddress, Port port) =>
            GetServerDatabase(ipAddress, port).Accounts.GetAllAccounts();

        public void AddAccount(IPv4Address ipAddress, Port port, Account account) =>
            /* w funkcji wywołującej aktualną funkcję (AddAccount)
            dodajemy "Error occured while adding account to server's database" */
            GetServerDatabase(ipAddress, port).Accounts.AddAccount(account);

        public bool AccountExists(IPv4Address ipAddress, Port port, string login) =>
            GetServerDatabase(ipAddress, port).Accounts.AccountExists(login);

        public void UpdateAccount(IPv4Address ipAddress, Port port, string login, Account account) =>
            GetServerDatabase(ipAddress, port).Accounts.UpdateAccount(login, account);

        public void DeleteAccount(IPv4Address ipAddress, Port port, string login) =>
            GetServerDatabase(ipAddress, port).Accounts.DeleteAccount(login);
    }
}
