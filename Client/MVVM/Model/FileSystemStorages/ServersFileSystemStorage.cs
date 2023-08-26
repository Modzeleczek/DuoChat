using Client.MVVM.Model.SQLiteStorage;

namespace Client.MVVM.Model.FileSystemStorages
{
    public class ServersFileSystemStorage : FileFileSystemStorage<ServerPrimaryKey>
    {
        public ServersFileSystemStorage(string rootDirectoryPath)
            : base(rootDirectoryPath)
        { }

        protected override void CreateEntry(string path)
        {
            /* Metody abstrakcyjne są jednocześnie wirtualne,
            więc można całkowicie nadpisać ich kod. */
            new ServerDatabase(path);
        }

        protected override ServerPrimaryKey EntryNameToKey(string name)
        {
            return new ServerPrimaryKey(name);
        }

        protected override bool KeysEqual(ServerPrimaryKey a, ServerPrimaryKey b)
        {
            return a.Equals(b);
        }

        protected override string KeyToEntryName(ServerPrimaryKey key)
        {
            return key.ToString();
        }
    }
}
