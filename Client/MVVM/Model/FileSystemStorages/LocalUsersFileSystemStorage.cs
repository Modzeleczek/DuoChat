namespace Client.MVVM.Model.FileSystemStorages
{
    // Nazwa lokalnego użytkownika jest kluczem głównym.
    public class LocalUsersFileSystemStorage : DirectoryFileSystemStorage<LocalUserPrimaryKey>
    {
        public LocalUsersFileSystemStorage(string rootDirectoryPath)
            : base(rootDirectoryPath)
        { }

        protected override LocalUserPrimaryKey EntryNameToKey(string name)
        {
            return new LocalUserPrimaryKey(name);
        }

        protected override bool KeysEqual(LocalUserPrimaryKey a, LocalUserPrimaryKey b)
        {
            return a.Equals(b);
        }

        protected override string KeyToEntryName(LocalUserPrimaryKey key)
        {
            return key.ToString();
        }
    }
}
