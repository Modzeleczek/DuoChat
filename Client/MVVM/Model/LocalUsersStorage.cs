using Client.MVVM.View.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Client.MVVM.Model
{
    public class LocalUsersStorage
    {
        private const string path = "local_users.bson";
        private static readonly Strings d = Strings.Instance;

        private class FileStructure
        {
            public bool IsLogged { get; set; } = false;
            public string LoggedUserName { get; set; } = "";
            public List<LocalUser> Users { get; set; } = new List<LocalUser>();
        }

        private FileStructure Load()
        {
            FileStructure ret;
            if (File.Exists(path))
            {
                using (var br = new BinaryReader(File.OpenRead(path)))
                using (var bdr = new BsonDataReader(br, false, DateTimeKind.Utc))
                {
                    var ser = new JsonSerializer();
                    ret = ser.Deserialize<FileStructure>(bdr);
                }
            }
            else ret = new FileStructure();
            return ret;
        }

        private void Save(FileStructure users)
        {
            // jeżeli plik nie istnieje, to zostanie stworzony
            using (var bw = new BinaryWriter(File.OpenWrite(path)))
            using (var bdw = new BsonDataWriter(bw))
            {
                var ser = new JsonSerializer();
                ser.Serialize(bdw, users);
            }
        }

        public Status Add(LocalUser user)
        {
            var fileStr = Load();
            var users = fileStr.Users;
            if (Exists(user.Name, users))
                return new Status(1, d["User with name"] + $" '{user.Name}' " + d["already exists."]);
            users.Add(user);
            Save(fileStr);
            return new Status(0);
        }

        public List<LocalUser> GetAll() => Load().Users;

        private bool Exists(string userName, List<LocalUser> users)
        {
            for (int i = 0; i < users.Count; ++i)
                if (users[i].Name == userName)
                    return true;
            return false;
        }

        public bool Exists(string userName) => Exists(userName, Load().Users);

        public Status Get(string userName)
        {
            var users = Load().Users;
            for (int i = 0; i < users.Count; ++i)
            {
                var u = users[i];
                if (u.Name == userName)
                    return new Status(0, null, u);
            }
            return new Status(1,
                d["User with name"] + $" '{userName}' " + d["does not exist."], null);
        }

        public Status Update(string userName, LocalUser user)
        {
            // w obiekcie user może być nowa nazwa użytkownika, ale nie może być zajęta
            var fileStr = Load();
            var users = fileStr.Users;
            for (int i = 0; i < users.Count; ++i)
            {
                if (users[i].Name == userName)
                {
                    string error = d["User with name"] + $" '{user.Name}' " + d["already exists."];
                    int j;
                    for (j = 0; j < i; ++j)
                        if (users[j].Name == user.Name)
                            return new Status(2, error);
                    for (j = i + 1; j < users.Count; ++j)
                        if (users[j].Name == user.Name)
                            return new Status(2, error);
                    users[i] = user;
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return new Status(1, d["User with name"] + $" '{userName}' " + d["does not exist."]);
        }

        public Status Delete(string userName)
        {
            var fileStr = Load();
            var users = fileStr.Users;
            for (int i = 0; i < users.Count; ++i)
            {
                var u = users[i];
                if (u.Name == userName)
                {
                    users.RemoveAt(i);
                    Save(fileStr);
                    return new Status(0);
                }
            }
            return new Status(1, d["User with name"] + $" '{userName}' " + d["does not exist."]);
        }

        public Status SetLogged(bool isLogged, string userName = "")
        {
            var fileStr = Load();
            fileStr.IsLogged = isLogged;
            fileStr.LoggedUserName = userName;
            Save(fileStr);
            return new Status(0);
        }

        public Status GetLogged()
        {
            var fileStr = Load();
            if (!fileStr.IsLogged) return new Status(1, d["No user is logged."]);
            return new Status(0, null, fileStr.LoggedUserName);
        }
    }
}
