using Client.MVVM.Model.JsonSerializables;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace Client.MVVM.Model.BsonStorages
{
    public class LocalUsersStorage : BsonStorage
    {
        public const string USERS_DIRECTORY_NAME = "databases";
        private const string USERS_BSON_NAME = "local_users.bson";

        private string usersBsonPath => Path.Combine(USERS_DIRECTORY_NAME, USERS_BSON_NAME);

        public class BsonStructure
        {
            public bool IsLogged { get; set; } = false;
            public string LoggedUserName { get; set; } = "";
            public int ActiveLanguageId { get; set; } = 0;
            public int ActiveThemeId { get; set; } = 0;
            public List<LocalUserSerializable> Users { get; set; } = new List<LocalUserSerializable>();
        }

        private Error UserAlreadyExistsError(string name) =>
            new Error($"|User with name| {name} |already exists.|");

        private Error UserDoesNotExistError(string name) =>
            new Error($"|User with name| {name} |does not exist.|");

        private Error UsersDirectoryAlreadyExistsError(string name) =>
            new Error($"|User's directory with name| {name} |already exists.|");

        private Error UsersDirectoryDoesNotExistError(string name) =>
            new Error($"|User's directory with name| {name} |does not exist.|");

        public BsonStructure EnsureValidDatabaseState()
        {
            var path = USERS_DIRECTORY_NAME;
            if (!Directory.Exists(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while| |creating| " +
                        $"|directory| {path}.");
                }
            }
            path = usersBsonPath;
            if (!File.Exists(path))
            {
                try { Save(new BsonStructure()); }
                catch (Error e)
                {
                    e.Prepend($"|Error occured while| |creating| |file| {path}.");
                    throw;
                }
            }

            var structure = Load<BsonStructure>(usersBsonPath);
            /* usuwamy użytkowników obecnych w BSONie, ale
            nieposiadających swoich katalogów z plikiem "servers.bson" */
            var users = structure.Users;
            var filteredUsers = new List<LocalUserSerializable>();
            bool shouldOverwrite = false;
            foreach (var u in users)
            {
                var dirPath = UserDirectoryPath(u.Name);
                if (!Directory.Exists(dirPath))
                {
                    shouldOverwrite = true;
                    continue;
                }
                filteredUsers.Add(u);
            }
            if (shouldOverwrite)
            {
                structure.Users = filteredUsers;
                try { Save(structure); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |deleting| " +
                        "|users| |not having| |a directory|.");
                    throw;
                }
            }

            // usuwamy pliki i katalogi z katalogu "databases", do których nie ma w BSONie użytkowników
            users = structure.Users;
            var hashSet = new HashSet<string>();
            foreach (var u in users)
                hashSet.Add(u.Name);
            DirectoryInfo di;
            try { di = new DirectoryInfo(USERS_DIRECTORY_NAME); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    "|listing files and subdirectories in directory|" +
                    $"'{USERS_DIRECTORY_NAME}'.");
            }
            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name != USERS_BSON_NAME)
                {
                    try { file.Delete(); }
                    catch (Exception e)
                    {
                        throw new Error(e, "|Error occured while| " +
                            $"|deleting| |file| {file.Name}.");
                    }
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                if (!hashSet.Contains(dir.Name))
                {
                    try { dir.Delete(true); }
                    catch (Exception e)
                    {
                        throw new Error(e, "|Error occured while| " +
                            $"|deleting| |directory| {dir.Name}.");
                    }
                }
            }
            return structure;
        }

        private BsonStructure Load()
        {
            try { return EnsureValidDatabaseState(); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| " +
                    "|ensuring valid user database state.|");
                throw;
            }
        }

        private void Save(BsonStructure users) => Save(usersBsonPath, users);

        private string UserDirectoryPath(string userName) =>
            Path.Combine(USERS_DIRECTORY_NAME, userName);

        public void Add(LocalUser user)
        {
            var structure = Load();

            var users = structure.Users;
            var name = user.Name;
            if (Exists(name, users))
                throw UserAlreadyExistsError(name);

            var directoryPath = UserDirectoryPath(name);
            if (Directory.Exists(directoryPath))
                throw UsersDirectoryAlreadyExistsError(name);

            users.Add(user.ToSerializable());
            try { Save(structure); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| |saving| |user database file.|");
                throw;
            }

            try { Directory.CreateDirectory(directoryPath); }
            catch (Exception e)
            {
                var createDirError = new Error(e, "|Error occured while| " +
                    "|creating| |user's directory.|");

                users.RemoveAt(users.Count - 1); // usuwamy ostatnio dodanego
                try { Save(structure); }
                catch (Error saveError)
                {
                    createDirError.Append("|Error occured while| " +
                        "|deleting| |the newly-added| |user.| " + saveError.Message);
                }
                throw createDirError;
            }

            try { new ServersStorage(user).EnsureValidDatabaseState(); }
            catch (Error ensureError)
            {
                ensureError.Prepend("|Error occured while| " +
                    "|creating| |user's server database.|");

                users.RemoveAt(users.Count - 1); // usuwamy ostatnio dodanego
                try { Save(structure); }
                catch (Error saveError)
                {
                    ensureError.Append("|Error occured while| " +
                        "|deleting| |the newly-added| |user.| " + saveError.Message);
                }

                try { Directory.Delete(directoryPath); }
                catch (Exception)
                {
                    ensureError.Append("|Error occured while| " +
                        "|deleting| |the newly-added user's directory.|");
                }
                throw ensureError;
            }
        }

        public List<LocalUser> GetAll()
        {
            var users = Load().Users;

            var observableUsers = new List<LocalUser>(users.Count);
            foreach (var su in users)
                observableUsers.Add(su.ToObservable());
            return observableUsers;
        }

        private bool Exists(string userName, List<LocalUserSerializable> users)
        {
            for (int i = 0; i < users.Count; ++i)
                if (users[i].KeyEquals(userName))
                    return true;
            return false;
        }

        public bool Exists(string userName)
        {
            return Exists(userName, Load().Users);
        }

        public LocalUser Get(string userName)
        {
            var users = Load().Users;

            for (int i = 0; i < users.Count; ++i)
            {
                var u = users[i];
                if (u.KeyEquals(userName))
                    return u.ToObservable();
            }
            return null;
        }

        public void Update(string userName, LocalUser user)
        {
            var structure = Load();
            var users = structure.Users;

            /* var oldDirectoryExistsStatus = DirectoryExists(userName);
            if (oldDirectoryExistsStatus.Code == 1)
                return oldDirectoryExistsStatus.Prepend(-9); */

            // w obiekcie user może być nowa nazwa użytkownika, ale nie może być zajęta
            for (int i = 0; i < users.Count; ++i)
            {
                if (users[i].KeyEquals(userName))
                {
                    var userSerializable = user.ToSerializable();
                    var newUserName = user.Name;
                    int j;
                    for (j = 0; j < i; ++j)
                        if (users[j].Equals(userSerializable))
                            throw UserAlreadyExistsError(newUserName);
                    for (j = i + 1; j < users.Count; ++j)
                        if (users[j].Equals(userSerializable))
                            throw UserAlreadyExistsError(newUserName);

                    string dbSaveError = "|Error occured while| " +
                        "|saving| |user database file.|";
                    if (userSerializable.KeyEquals(userName))
                    {
                        // jeżeli metodą Update nie zmieniamy nazwy użytkownika
                        users[i] = userSerializable;
                        try { Save(structure); }
                        catch (Error e)
                        {
                            e.Prepend(dbSaveError);
                            throw;
                        }
                    }
                    else
                    {
                        var newDirectoryPath = UserDirectoryPath(newUserName);
                        if (Directory.Exists(newDirectoryPath))
                            throw UsersDirectoryAlreadyExistsError(newUserName);

                        var oldUserBackup = users[i];
                        users[i] = userSerializable;
                        try { Save(structure); }
                        catch (Error e)
                        {
                            e.Prepend(dbSaveError);
                            throw;
                        }

                        var oldDirectoryPath = UserDirectoryPath(userName);
                        if (Directory.Exists(oldDirectoryPath))
                        {
                            try { Directory.Move(oldDirectoryPath, newDirectoryPath); }
                            catch (Exception e)
                            {
                                var renameDirError = new Error(e,
                                    "|Error occured while| |renaming| |user's directory.|");

                                users[i] = oldUserBackup; // przywracamy ostatnio zastąpionego
                                try { Save(structure); }
                                catch (Error saveError)
                                {
                                    renameDirError.Append("|Error occured while| " +
                                        "|restoring the updated| |user.| " +
                                        saveError.Message);
                                }
                                throw renameDirError;
                            }
                        }
                    }
                    return;
                }
            }
            throw UserDoesNotExistError(userName);
        }

        public void Delete(string userName)
        {
            var structure = Load();
            var users = structure.Users;

            for (int i = 0; i < users.Count; ++i)
            {
                if (users[i].KeyEquals(userName))
                {
                    var userBackup = users[i];
                    users.RemoveAt(i);
                    try { Save(structure); }
                    catch (Error e)
                    {
                        e.Prepend("|Error occured while| |saving| |user database file.|");
                        throw;
                    }

                    var directoryPath = UserDirectoryPath(userName);
                    if (Directory.Exists(directoryPath))
                    {
                        try { Directory.Delete(directoryPath, true); }
                        catch (Exception e)
                        {
                            var deleteDirError = new Error(e, "|Error occured while| " +
                                "|deleting| |user's directory.|");

                            users.Insert(i, userBackup); // przywracamy ostatnio usuniętego
                            try { Save(structure); }
                            catch (Error saveError)
                            {
                                deleteDirError.Append("|Error occured while| " +
                                    "|restoring the deleted| |user.| " + saveError.Message);
                            }
                            throw deleteDirError;
                        }
                    }
                    return;
                }
            }
            throw UserDoesNotExistError(userName);
        }

        public void SetLogged(bool isLogged, string userName = "")
        {
            var structure = Load();
            structure.IsLogged = isLogged;
            structure.LoggedUserName = userName;
            Save(structure);
        }

        public string GetLogged()
        {
            var structure = Load();
            if (!structure.IsLogged)
                return null;
            return structure.LoggedUserName;
        }

        public void SetActiveLanguage(int id)
        {
            var structure = Load();
            structure.ActiveLanguageId = id;
            Save(structure);
        }

        public int GetActiveLanguage()
        {
            return Load().ActiveLanguageId;
        }

        // zwraca null lub opis błędu
        public static string ValidateUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return "|Username cannot be empty.|";
            /* nie możemy pozwolić na stworzenie drugiego użytkownika o takiej samej nazwie
             * case-insensitive, ponieważ NTFS ma case-insensitive nazwy plików i katalogów;
             * najprościej temu zapobiec, wymuszając nazwy użytkowników
             * złożone z tylko małych liter */
            foreach (var c in userName)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                    return "|Username may contain only lowercase letters and digits.|";
            return null;
        }

        public void SetActiveTheme(int id)
        {
            var structure = Load();
            structure.ActiveThemeId = id;
            Save(structure);
        }

        public int GetActiveTheme()
        {
            return Load().ActiveThemeId;
        }
    }
}
