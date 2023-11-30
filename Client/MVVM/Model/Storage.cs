using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model.FileSystemStorages;
using Client.MVVM.Model.SQLiteStorage;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace Client.MVVM.Model
{
    // Wzorzec Fasada
    public class Storage
    {
        public Storage()
        {
            CreateRootDirectoryIfNotExists();

            // Zapewniamy istnienie storage/local_users.bson
            GetLocalUsersBsonStorage();

            // Zapewniamy istnienie storage/local_users/
            GetLocalUsersFileSystemStorage();

            /* Nie wywołujemy, bo to usuwa, żeby zapewnić poprawny
            stan struktury katalogów. Trzymamy się zasady, że nie
            usuwamy poza procedurą (inaczej niż przy użyciu operacji
            CRUDa) niepoprawnych danych, tylko informujemy Errorami
            o wykryciu niepoprawnego stanu. */
            // SynchronizeLocalUsersBsonAndFs();
        }

        private void CreateRootDirectoryIfNotExists()
        {
            // Konstruktor wykonujemy tylko raz na początku całego programu.
            // Zapewniamy istnienie storage/
            var rootDirectoryPath = ResolvePath();
            if (Directory.Exists(rootDirectoryPath))
                return;

            try { Directory.CreateDirectory(rootDirectoryPath); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |creating| " +
                    $"|directory| '{rootDirectoryPath}'.");
            }
        }

        private void SynchronizeLocalUsersBsonAndFs()
        {
            // Zapewniamy istnienie storage/local_users.bson
            var bson = GetLocalUsersBsonStorage();

            // Zapewniamy istnienie storage/local_users/
            var fs = GetLocalUsersFileSystemStorage();

            /* Z pliku "local_users.bson" usuwamy użytkowników, którzy nie
            mają swojego podkatalogu w katalogu "local_users". */
            var fsHashSet = fs.GetAll().ToHashSet();
            bson.DeleteMany(user => !fsHashSet.Contains(user.GetPrimaryKey()));

            /* Usuwamy podkatalogi z katalogu "local_users",
            do których nie ma użytkowników w pliku "local_users.bson". */
            var bsonHashSet = bson.GetAll().Select(user => user.GetPrimaryKey()).ToHashSet();
            fs.DeleteMany(userKey => !bsonHashSet.Contains(userKey));

            /* W tym momencie istnieje tylko część wspólna nazw lokalnych
            użytkowników z katalogu "local_users" i pliku "local_users.bson". */
        }

        private string ResolvePath(bool? localUsersBson = null,
            LocalUserPrimaryKey? localUserKey = null,
            bool? serversBson = null, ServerPrimaryKey? serverKey = null)
        {
            // Tu można ustawić korzeń całej struktury katalogów.
            string path = "storage";

            if (localUsersBson is null)
                // storage/
                return path;

            if (localUsersBson.Value)
                // storage/local_users.bson
                return PathAppend(ref path, "local_users.bson");
            PathAppend(ref path, "local_users");

            if (localUserKey is null)
                // storage/local_users/
                return path;
            PathAppend(ref path, localUserKey.ToString()!);

            if (serversBson is null)
                // storage/local_users/{localUserKey}/
                return path;

            if (serversBson.Value)
                // storage/local_users/{localUserKey}/servers.bson
                return PathAppend(ref path, "servers.bson");
            PathAppend(ref path, "servers");

            if (serverKey is null)
                // storage/local_users/{localUserKey}/servers/
                return path;

            // storage/local_users/{localUserKey}/servers/{serverKey}
            return PathAppend(ref path, serverKey.ToString()!);
        }

        private string PathAppend(ref string path, string newPart)
        {
            return path = Path.Combine(path, newPart);
        }

        private LocalUsersStorage GetLocalUsersBsonStorage()
        {
            // storage/local_users.bson
            /* TODO: usunąć z konstruktora CreateFileIfNotExists
            i sprawdzać tu, czy istnieją po kolei elementy na ścieżce
            (storage/local_users.bson). Dzięki temu, nie tylko przy
            tworzeniu obiektu Storage na początku programu, ale też
            przy każdej operacji CRUDa, będziemy sprawdzać, czy stan
            struktury danych jest poprawny. */
            return new LocalUsersStorage(ResolvePath(true));
        }

        private LocalUsersFileSystemStorage GetLocalUsersFileSystemStorage()
        {
            // storage/local_users/
            return new LocalUsersFileSystemStorage(ResolvePath(false));
        }

        private ServersStorage GetServersBsonStorage(LocalUserPrimaryKey localUserKey)
        {
            // storage/local_users/{localUserKey}/servers.bson
            return new ServersStorage(ResolvePath(false, localUserKey, true));
        }

        private ServersFileSystemStorage GetServersFileSystemStorage(LocalUserPrimaryKey localUserKey)
        {
            // storage/local_users/{localUserKey}/
            return new ServersFileSystemStorage(ResolvePath(false, localUserKey, false));
        }

        private ServerDatabase GetServerDatabase(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey)
        {
            // storage/local_users/{localUserKey}/servers/{serverKey}
            return new ServerDatabase(ResolvePath(false, localUserKey, false, serverKey));
        }

        #region LocalUser
        public void AddLocalUser(LocalUser user)
        {
            GetLocalUsersBsonStorage().Add(user);
            var userKey = user.GetPrimaryKey();

            var fs = GetLocalUsersFileSystemStorage();
            try { fs.Add(userKey); }
            catch (Error addError)
            {
                DeleteNewlyAddedLocalUser(userKey, ref addError);
                throw;
            }

            try { GetServersBsonStorage(userKey); }
            catch (Error getServerError)
            {
                getServerError.Prepend("|Could not| " +
                    "|create| |local user's server BSON file|.");

                DeleteNewlyAddedLocalUser(userKey, ref getServerError);
                try { fs.Delete(userKey); }
                catch (Error undoError)
                {
                    undoError.Prepend("|Could not| " +
                        "|delete| |the newly-added user's directory.|");
                    getServerError.Append(undoError.Message);
                }
                throw;
            }
        }

        private void DeleteNewlyAddedLocalUser(LocalUserPrimaryKey localUserKey, ref Error primaryError)
        {
            // Usuwamy ostatnio dodanego.
            try { GetLocalUsersBsonStorage().Delete(localUserKey); }
            catch (Error undoError)
            {
                undoError.Prepend("|Error occured while| " +
                    "|deleting| |the newly-added| |user.|");
                primaryError.Append(undoError.Message);
            }
        }

        public List<LocalUser> GetAllLocalUsers()
        {
            return GetLocalUsersBsonStorage().GetAll();
            /* TODO: sprawdzać, czy wszystkie rekordy z BSONa mają swoje
            katalogi i rzucać wyjątek, jeżeli nie wszystkie. */
        }

        public bool LocalUserExists(LocalUserPrimaryKey localUserKey)
        {
            return GetLocalUsersBsonStorage().Exists(localUserKey);
        }

        public LocalUser GetLocalUser(LocalUserPrimaryKey localUserKey)
        {
            return GetLocalUsersBsonStorage().Get(localUserKey);
        }

        public void UpdateLocalUser(LocalUserPrimaryKey localUserKey, LocalUser newUser)
        {
            var bson = GetLocalUsersBsonStorage();
            var backup = bson.Get(localUserKey);
            var newLocalUserKey = newUser.GetPrimaryKey();

            bson.Update(localUserKey, newUser);

            try { GetLocalUsersFileSystemStorage().Update(localUserKey, newLocalUserKey); }
            catch (Error updateError)
            {
                // Przywracamy ostatnio zaktualizowanego.
                try { bson.Update(newLocalUserKey, backup); }
                catch (Error undoError)
                {
                    undoError.Prepend("|Could not| " +
                        "|restore| |the updated| |user.|");
                    updateError.Append(undoError.Message);
                }
                throw;
            }
        }

        public void DeleteLocalUser(LocalUserPrimaryKey localUserKey)
        {
            var bson = GetLocalUsersBsonStorage();
            var backup = bson.Get(localUserKey);

            bson.Delete(localUserKey);

            try { GetLocalUsersFileSystemStorage().Delete(localUserKey); }
            catch (Error deleteError)
            {
                // Przywracamy ostatnio usuniętego.
                try { bson.Add(backup); }
                catch (Error undoError)
                {
                    undoError.Prepend("|Could not| " +
                        "|restore| |the deleted| |user.|");
                    deleteError.Append(undoError.Message);
                }
                throw;
            }
        }

        public void SetLoggedLocalUser(bool isLogged, LocalUserPrimaryKey localUserKey = default)
        {
            GetLocalUsersBsonStorage().SetLogged(isLogged, localUserKey);
        }

        public LocalUserPrimaryKey? GetLoggedLocalUserKey()
        {
            return GetLocalUsersBsonStorage().GetLogged();
        }

        public void SetActiveLanguage(int id)
        {
            GetLocalUsersBsonStorage().SetActiveLanguage(id);
        }

        public int GetActiveLanguage()
        {
            return GetLocalUsersBsonStorage().GetActiveLanguage();
        }

        public void SetActiveTheme(int id)
        {
            GetLocalUsersBsonStorage().SetActiveTheme(id);
        }

        public int GetActiveTheme()
        {
            return GetLocalUsersBsonStorage().GetActiveTheme();
        }
        #endregion

        #region Server
        public void AddServer(LocalUserPrimaryKey localUserKey, ViewModel.Observables.Server server)
        {
            /* Tu nie łapiemy wyjątków, bo są to operacje "nieatomowe" i nie mutujące
            (nie zmieniające aktualnie panującego, prawidłowego stanu obiektów)
            czyli nie wymagające cofania przy niepowodzeniu którejkolwiek z nich. */
            var bsonStorage = GetServersBsonStorage(localUserKey);
            var fsStorage = GetServersFileSystemStorage(localUserKey);
            var serverKey = server.GetPrimaryKey();

            bsonStorage.Add(server);

            try { fsStorage.Add(serverKey); }
            catch (Error addError)
            {
                /* fsStorage.Add wyrzuciło Error, więc cofamy
                bsonStorage.Add, które pomyślnie się wykonało -
                usuwamy ostatnio dodany serwer. */
                try { bsonStorage.Delete(serverKey); }
                catch (Error undoError)
                {
                    undoError.Prepend("|Could not| |delete| " +
                        "|the newly-added| |server BSON entry|.");
                    addError.Append(undoError.Message);
                }
                throw;
            }
        }

        public List<ViewModel.Observables.Server> GetAllServers(LocalUserPrimaryKey localUserKey)
        {
            return GetServersBsonStorage(localUserKey).GetAll();
        }

        public bool ServerExists(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey)
        {
            return GetServersBsonStorage(localUserKey).Exists(serverKey);
        }

        public ViewModel.Observables.Server GetServer(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey)
        {
            return GetServersBsonStorage(localUserKey).Get(serverKey);
        }

        public void UpdateServer(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey, ViewModel.Observables.Server newServer)
        {
            var bsonStorage = GetServersBsonStorage(localUserKey);
            var fsStorage = GetServersFileSystemStorage(localUserKey);
            var newServerKey = newServer.GetPrimaryKey();
            var backup = bsonStorage.Get(serverKey);

            bsonStorage.Update(serverKey, newServer);

            try { fsStorage.Update(serverKey, newServerKey); }
            catch (Error updateError)
            {
                try { bsonStorage.Update(newServerKey, backup); }
                catch (Error undoError)
                {
                    undoError.Prepend("|Could not| " +
                        "|restore| |the updated| |server BSON entry|.");
                    updateError.Append(undoError.Message);
                }
                throw;
            }
        }

        public void DeleteServer(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey)
        {
            var bsonStorage = GetServersBsonStorage(localUserKey);
            var fsStorage = GetServersFileSystemStorage(localUserKey);
            var backup = bsonStorage.Get(serverKey);

            bsonStorage.Delete(serverKey);

            try { fsStorage.Delete(serverKey); }
            catch (Error deleteError)
            {
                try { bsonStorage.Add(backup); }
                catch (Error undoError)
                {
                    undoError.Prepend("|Could not| " +
                        "|restore| |the deleted| |server BSON entry|.");
                    deleteError.Append(undoError.Message);
                }
                throw;
            }
        }
        #endregion

        #region Account
        public void AddAccount(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey,
            Account account)
        {
            GetServerDatabase(localUserKey, serverKey).Accounts.Add(account);
        }

        public List<Account> GetAllAccounts(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey)
        {
            return GetServerDatabase(localUserKey, serverKey).Accounts.GetAll();
        }

        public bool AccountExists(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey,
            string accountLogin)
        {
            return GetServerDatabase(localUserKey, serverKey).Accounts.Exists(accountLogin);
        }

        public Account GetAccount(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey,
            string accountLogin)
        {
            return GetServerDatabase(localUserKey, serverKey).Accounts.Get(accountLogin);
        }

        public void UpdateAccount(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey,
            string accountLogin, Account newAccount)
        {
            GetServerDatabase(localUserKey, serverKey).Accounts.Update(accountLogin, newAccount);
        }

        public void DeleteAccount(LocalUserPrimaryKey localUserKey, ServerPrimaryKey serverKey,
            string accountLogin)
        {
            GetServerDatabase(localUserKey, serverKey).Accounts.Delete(accountLogin);
        }
        #endregion

        #region Encryption
        public void EncryptLocalUser(ref ProgressReporter reporter,
            LocalUserPrimaryKey localUserKey, SecureString password)
        {
            var localUser = GetLocalUser(localUserKey);
            var userDirectoryPath = ResolvePath(false, localUserKey);

            FileEncryptor.EncryptDirectory(reporter,
                userDirectoryPath,
                PasswordCryptography.ComputeDigest(
                    password, localUser.DbSalt, Aes.KEY_LENGTH),
                localUser.DbInitializationVector);
        }

        public void DecryptLocalUser(ref ProgressReporter reporter,
            LocalUserPrimaryKey localUserKey, SecureString password)
        {
            var localUser = GetLocalUser(localUserKey);
            var userDirectoryPath = ResolvePath(false, localUserKey);

            FileEncryptor.DecryptDirectory(reporter,
                userDirectoryPath,
                PasswordCryptography.ComputeDigest(
                    password, localUser.DbSalt, Aes.KEY_LENGTH),
                localUser.DbInitializationVector);

            // Nie wywołujemy, bo usuwa poza procedurą CRUDa.
            // SynchronizeServersBsonAndFs(localUserKey);
        }
        #endregion

        public void SynchronizeServersBsonAndFs(LocalUserPrimaryKey localUserKey)
        {
            var srvBson = GetServersBsonStorage(localUserKey);
            var srvFs = GetServersFileSystemStorage(localUserKey);

            var srvFsHashSet = srvFs.GetAll().ToHashSet();
            srvBson.DeleteMany(server => !srvFsHashSet.Contains(server.GetPrimaryKey()));

            var srvBsonHashSet = srvBson.GetAll()
                .Select(server => server.GetPrimaryKey()).ToHashSet();
            srvFs.DeleteMany(serverKey => !srvBsonHashSet.Contains(serverKey));
        }

        /* TODO: zrobić, że operacje na serwerach i kontach można
        wykonywać tylko dla zalogowanego (odszyfrowanego użytkownika). */
    }
}
