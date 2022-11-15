using Client.MVVM.View.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;

namespace Client.MVVM.Model
{
    public static class LocalUsersStorage
    {
        private const string path = "local_users.bson";
        private static readonly Strings d = Strings.Instance;

        private static List<LocalUser> Load()
        {
            List<LocalUser> ret;
            if (File.Exists(path))
            {
                using (var br = new BinaryReader(File.OpenRead(path)))
                using (var bdr = new BsonDataReader(br, true, DateTimeKind.Utc))
                {
                    var ser = new JsonSerializer();
                    ret = ser.Deserialize<List<LocalUser>>(bdr);
                }
            }
            else ret = new List<LocalUser>();
            return ret;
        }

        private static void Save(List<LocalUser> users)
        {
            // jeżeli plik nie istnieje, to zostanie stworzony
            using (var bw = new BinaryWriter(File.OpenWrite(path)))
            using (var bdw = new BsonDataWriter(bw))
            {
                var ser = new JsonSerializer();
                ser.Serialize(bdw, users);
            }
        }

        public static Status Add(LocalUser user)
        {
            var users = Load();
            if (Exists(user.Name, users))
                return new Status(1, d["User with name"] + $" '{user.Name}' " +
                    d["already exists."]);
            users.Add(user);
            Save(users);
            return new Status(0);
        }

        public static List<LocalUser> GetAll() => Load();

        private static bool Exists(string userName, List<LocalUser> users)
        {
            for (int i = 0; i < users.Count; ++i)
                if (users[i].Name == userName)
                    return true;
            return false;
        }

        public static bool Exists(string userName) => Exists(userName, Load());

        public static Status Get(string userName, out LocalUser user)
        {
            user = null;
            var users = Load();
            for (int i = 0; i < users.Count; ++i)
            {
                var u = users[i];
                if (u.Name == userName)
                {
                    user = u;
                    return new Status(0);
                }
            }
            return new Status(1, d["User with name"] + $" '{userName}' " + d["does not exist."]);
        }

        public static Status Update(string userName, LocalUser user)
        {
            // w obiekcie user może być nowa nazwa użytkownika, ale nie może być zajęta
            var users = Load();
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
                    Save(users);
                    return new Status(0);
                }
            }
            return new Status(1, d["User with name"] + $" '{userName}' " + d["does not exist."]);
        }

        public static Status Delete(string userName)
        {
            var users = Load();
            for (int i = 0; i < users.Count; ++i)
            {
                var u = users[i];
                if (u.Name == userName)
                {
                    users.RemoveAt(i);
                    Save(users);
                    return new Status(0);
                }
            }
            return new Status(1, d["User with name"] + $" '{userName}' " + d["does not exist."]);
        }
    }
}
