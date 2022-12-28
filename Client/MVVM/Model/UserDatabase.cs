using Client.MVVM.Model.BsonStorages;
using System;
using System.IO;

namespace Client.MVVM.Model
{
    public class UserDatabase
    {
        public string DirectoryPath
        { get => Path.Combine(LocalUsersStorage.USERS_DIRECTORY_PATH, UserName); }
        public string UserName { get; private set; }

        public string ServersStoragePath
        { get => Path.Combine(DirectoryPath, "servers.bson"); }

        public UserDatabase(string userName)
        {
            UserName = userName;
        }

        public bool Exists() => Directory.Exists(DirectoryPath);

        public void Delete() => Directory.Delete(DirectoryPath, true);

        public void Create() => Directory.CreateDirectory(DirectoryPath);

        public void Rename(string newUserName)
        {
            string oldDirPat = DirectoryPath;
            UserName = newUserName;
            Directory.Move(oldDirPat, DirectoryPath);
        }

        public ServersStorage GetServersStorage() => new ServersStorage(ServersStoragePath);

        public Status GetServerDatabase(Guid guid)
        {
            var serStor = new ServersStorage(ServersStoragePath);
            var getSta = serStor.Get(guid);
            if (getSta.Code != 0) return getSta;
            var server = (Server)getSta.Data;
            var daoPath = Path.Combine(DirectoryPath, server.GUID.ToString());
            return new Status(0, null, new DataAccessObject(daoPath));
        }
    }
}
