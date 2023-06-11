using Client.MVVM.Model.JsonSerializables;
using Shared.MVVM.Model;
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

        private class BsonStructure
        {
            public bool IsLogged { get; set; } = false;
            public string LoggedUserName { get; set; } = "";
            public int ActiveLanguageId { get; set; } = 0;
            public int ActiveThemeId { get; set; } = 0;
            public List<LocalUserSerializable> Users { get; set; } = new List<LocalUserSerializable>();
        }

        private Status UserAlreadyExistsStatus(int code, string name) =>
            new Status(code, null, $"|User with name| {name} |already exists.|");

        private Status UserDoesNotExistStatus(int code, string name) =>
            new Status(code, null, $"|User with name| {name} |does not exist.|");

        private Status UsersDirectoryAlreadyExistsStatus(int code, string name) =>
            new Status(code, null, $"|User's directory with name| {name} |already exists.|");

        private Status UsersDirectoryDoesNotExistStatus(int code, string name) =>
            new Status(code, null, $"|User's directory with name| {name} |does not exist.|");

        public Status EnsureValidDatabaseState()
        {
            var path = USERS_DIRECTORY_NAME;
            if (!Directory.Exists(path))
            {
                try
                { Directory.CreateDirectory(path); }
                catch (Exception)
                {
                    return new Status(-1, null, "|Error occured while| |creating| " +
                        $"|directory| {path}."); // -1
                }
            }
            path = usersBsonPath;
            if (!File.Exists(path))
            {
                var saveStatus = Save(new BsonStructure());
                if (saveStatus.Code != 0)
                    return saveStatus.Prepend(-2, "|Error occured while| |creating| " +
                        $"|file| {path}."); // -2
            }

            var loadStatus = Load<BsonStructure>(usersBsonPath);
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-3); // -3

            var structure = (BsonStructure)loadStatus.Data;
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
                var saveStatus = Save(structure);
                if (saveStatus.Code != 0)
                    return saveStatus.Prepend(-4, "|Error occured while| |deleting| " +
                        "|users| |not having| |a directory|."); // -4
            }

            // usuwamy pliki i katalogi z katalogu "databases", do których nie ma w BSONie użytkowników
            users = structure.Users;
            var hashSet = new HashSet<string>();
            foreach (var u in users)
                hashSet.Add(u.Name);
            DirectoryInfo di;
            try
            { di = new DirectoryInfo(USERS_DIRECTORY_NAME); }
            catch (Exception)
            {
                return new Status(-5, null, "|Error occured while| " +
                    "|listing files and subdirectories in directory|" +
                    $"'{USERS_DIRECTORY_NAME}'."); // -5
            }
            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name != USERS_BSON_NAME)
                {
                    try { file.Delete(); }
                    catch (Exception)
                    {
                        return new Status(-6, null, "|Error occured while| " +
                            $"|deleting| |file| {file.Name}."); // -6
                    }
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                if (!hashSet.Contains(dir.Name))
                {
                    try { dir.Delete(true); }
                    catch (Exception)
                    {
                        return new Status(-7, null, "|Error occured while| " +
                            $"|deleting| |directory| {dir.Name}."); // -7
                    }
                }
            }
            return new Status(0, structure); // 0
        }

        private Status Load()
        {
            var ensureStatus = EnsureValidDatabaseState();
            if (ensureStatus.Code != 0)
                return ensureStatus.Prepend(-1, "|Error occured while| " +
                    "|ensuring valid user database state.|"); // -1
            return ensureStatus; // 0
        }

        private Status Save(BsonStructure users) => Save(usersBsonPath, users);

        private string UserDirectoryPath(string userName) =>
            Path.Combine(USERS_DIRECTORY_NAME, userName);

        public Status Add(LocalUser user)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            var users = structure.Users;
            var name = user.Name;
            if (Exists(name, users))
                return UserAlreadyExistsStatus(-2, name); // -2

            var directoryPath = UserDirectoryPath(name);
            if (Directory.Exists(directoryPath))
                return UsersDirectoryAlreadyExistsStatus(-3, name); // -3

            users.Add(user.ToSerializable());
            var saveStatus = Save(structure);
            if (saveStatus.Code != 0)
                return saveStatus.Prepend(-4, "|Error occured while| " +
                    "|saving| |user database file.|"); // -4

            try
            { Directory.CreateDirectory(directoryPath); }
            catch (Exception)
            {
                var createDirStatus = new Status(-5, null, "|Error occured while| " +
                    "|creating| |user's directory.|");

                users.RemoveAt(users.Count - 1); // usuwamy ostatnio dodanego
                saveStatus = Save(structure);
                if (saveStatus.Code != 0)
                    return createDirStatus.Append(-6, "|Error occured while| " +
                        "|deleting| |the newly-added| |user.| " + saveStatus.Message); // -6
                return createDirStatus; // -5
            }

            var ensureStatus = new ServersStorage(user).EnsureValidDatabaseState();
            if (ensureStatus.Code != 0)
            {
                ensureStatus.Prepend(-7, "|Error occured while| " +
                    "|creating| |user's server database.|"); // -7

                users.RemoveAt(users.Count - 1); // usuwamy ostatnio dodanego
                saveStatus = Save(structure);
                if (saveStatus.Code != 0)
                    ensureStatus.Append(-8, "|Error occured while| " +
                        "|deleting| |the newly-added| |user.| " + saveStatus.Message); // -8

                try { Directory.Delete(directoryPath); }
                catch (Exception)
                {
                    ensureStatus.Append(-9, "|Error occured while| " +
                        "|deleting| |the newly-added user's directory.|"); // -9
                }
                return ensureStatus;
            }

            return new Status(0); // 0
        }

        public Status GetAll()
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
            var users = structure.Users;

            var observableUsers = new List<LocalUser>(users.Count);
            foreach (var su in users)
                observableUsers.Add(su.ToObservable());
            return new Status(0, observableUsers); // 0
        }

        private bool Exists(string userName, List<LocalUserSerializable> users)
        {
            for (int i = 0; i < users.Count; ++i)
                if (users[i].KeyEquals(userName))
                    return true;
            return false;
        }

        public Status Exists(string userName)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            if (Exists(userName, structure.Users))
            {
                return UserAlreadyExistsStatus(0, userName); // 0
            }
            else
                return UserDoesNotExistStatus(1, userName); // 1
        }

        public Status Get(string userName)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
            var users = structure.Users;

            for (int i = 0; i < users.Count; ++i)
            {
                var u = users[i];
                if (u.KeyEquals(userName))
                    return new Status(0, u.ToObservable()); // 0
            }
            return UserDoesNotExistStatus(-2, userName); // -2
        }

        public Status Update(string userName, LocalUser user)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
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
                            return UserAlreadyExistsStatus(-2, newUserName); // -2
                    for (j = i + 1; j < users.Count; ++j)
                        if (users[j].Equals(userSerializable))
                            return UserAlreadyExistsStatus(-2, newUserName); // -2

                    string dbSaveError = "|Error occured while| " +
                        "|saving| |user database file.|";
                    if (userSerializable.KeyEquals(userName))
                    {
                        // jeżeli metodą Update nie zmieniamy nazwy użytkownika
                        users[i] = userSerializable;
                        var saveStatus = Save(structure);
                        if (saveStatus.Code != 0)
                            return saveStatus.Prepend(-3, dbSaveError); // -3
                    }
                    else
                    {
                        var newDirectoryPath = UserDirectoryPath(newUserName);
                        if (Directory.Exists(newDirectoryPath))
                            return UsersDirectoryAlreadyExistsStatus(-4, newUserName); // -4

                        var oldUserBackup = users[i];
                        users[i] = userSerializable;
                        var saveStatus = Save(structure);
                        if (saveStatus.Code != 0)
                            return saveStatus.Prepend(-5, dbSaveError); // -5

                        var oldDirectoryPath = UserDirectoryPath(userName);
                        if (Directory.Exists(oldDirectoryPath))
                        {
                            try
                            { Directory.Move(oldDirectoryPath, newDirectoryPath); }
                            catch (Exception)
                            {
                                var renameDirStatus = new Status(-6, null,
                                    "|Error occured while| |renaming| |user's directory.|"); // -6

                                users[i] = oldUserBackup; // przywracamy ostatnio zastąpionego
                                saveStatus = Save(structure);
                                if (saveStatus.Code != 0)
                                    return renameDirStatus.Append(-7, "|Error occured while| " +
                                        "|restoring the updated| |user.| " +
                                        saveStatus.Message); // -7
                                return renameDirStatus; // -6
                            }
                        }
                    }
                    return new Status(0); // 0
                }
            }
            return UserDoesNotExistStatus(-8, userName); // -8
        }

        public Status Delete(string userName)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;
            var users = structure.Users;

            for (int i = 0; i < users.Count; ++i)
            {
                if (users[i].KeyEquals(userName))
                {
                    var userBackup = users[i];
                    users.RemoveAt(i);
                    var saveStatus = Save(structure);
                    if (saveStatus.Code != 0)
                        return saveStatus.Prepend(-2, "|Error occured while| " +
                            "|saving| |user database file.|"); // -2

                    var directoryPath = UserDirectoryPath(userName);
                    if (Directory.Exists(directoryPath))
                    {
                        try
                        { Directory.Delete(directoryPath, true); }
                        catch (Exception)
                        {
                            var deleteDirStatus = new Status(-3, null, "|Error occured while| " +
                                "|deleting| |user's directory.|"); // -3

                            users.Insert(i, userBackup); // przywracamy ostatnio usuniętego
                            saveStatus = Save(structure);
                            if (saveStatus.Code != 0)
                                return deleteDirStatus.Append(-4, "|Error occured while| " +
                                    "|restoring the deleted| |user.| " + saveStatus.Message); // -4
                            return deleteDirStatus; // -3
                        }
                    }
                    return new Status(0); // 0
                }
            }
            return UserDoesNotExistStatus(-5, userName); // -5
        }

        public Status SetLogged(bool isLogged, string userName = "")
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            structure.IsLogged = isLogged;
            structure.LoggedUserName = userName;

            var saveStatus = Save(structure);
            if (saveStatus.Code != 0)
                return saveStatus.Prepend(-2); // -2

            return new Status(0); // 0
        }

        public Status GetLogged()
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            if (!structure.IsLogged)
                return new Status(-2, null, "|No user is logged.|"); // -2
            
            return new Status(0, structure.LoggedUserName);
        }

        public Status SetActiveLanguage(int id)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            structure.ActiveLanguageId = id;

            var saveStatus = Save(structure);
            if (saveStatus.Code != 0)
                return saveStatus.Prepend(-2); // -2

            return new Status(0);
        }

        public Status GetActiveLanguage()
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            return new Status(0, structure.ActiveLanguageId);
        }

        public static Status ValidateUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return new Status(1, null, "|Username cannot be empty.|");
            /* nie możemy pozwolić na stworzenie drugiego użytkownika o takiej samej nazwie
             * case-insensitive, ponieważ NTFS ma case-insensitive nazwy plików i katalogów;
             * najprościej temu zapobiec, wymuszając nazwy użytkowników
             * złożone z tylko małych liter */
            foreach (var c in userName)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                    return new Status(2, null,
                        "|Username may contain only lowercase letters and digits.|");
            return new Status(0);
        }

        public Status SetActiveTheme(int id)
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            structure.ActiveThemeId = id;

            var saveStatus = Save(structure);
            if (saveStatus.Code != 0)
                return saveStatus.Prepend(-2); // -2

            return new Status(0);
        }

        public Status GetActiveTheme()
        {
            var loadStatus = Load();
            if (loadStatus.Code != 0)
                return loadStatus.Prepend(-1); // -1
            var structure = (BsonStructure)loadStatus.Data;

            return new Status(0, structure.ActiveThemeId);
        }
    }
}
