using Client.MVVM.Model.BsonStorages;
using System;
using System.IO;

namespace Client.MVVM.Model
{
    public class UserDatabase
    {
        public string DirectoryPath { get; private set; }

        public string ServersStoragePath { get => Path.Combine(DirectoryPath, "servers.bson"); }

        public UserDatabase(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public bool Exists() => Directory.Exists(DirectoryPath);

        public void Delete() => Directory.Delete(DirectoryPath, true);

        public void Create() => Directory.CreateDirectory(DirectoryPath);

        public void Rename(string newPath)
        {
            string oldPath = DirectoryPath;
            DirectoryPath = newPath;
            Directory.Move(oldPath, newPath);
        }

        public ServersStorage GetServersStorage() => new ServersStorage(ServersStoragePath);

        public Status GetServerDatabase(Guid serverGuid)
        {
            var serStor = new ServersStorage(ServersStoragePath);
            var getSta = serStor.Get(serverGuid);
            if (getSta.Code != 0) return getSta;
            var server = (Server)getSta.Data;
            var daoPath = Path.Combine(DirectoryPath, server.GUID.ToString());
            return new Status(0, null, new DataAccessObject(daoPath));
        }
    }
}
