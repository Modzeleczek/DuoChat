using System.Collections.Generic;
using System.IO;
using System;
using Shared.MVVM.Core;
using System.Linq;

namespace Client.MVVM.Model.FileSystemStorages
{
    public abstract class FileSystemStorage<PrimaryKey>
    {
        #region Fields
        protected readonly string _rootDirectoryPath;
        #endregion

        protected FileSystemStorage(string rootDirectoryPath)
        {
            _rootDirectoryPath = rootDirectoryPath;

            CreateRootDirectoryIfNotExists();
        }

        private void CreateRootDirectoryIfNotExists()
        {
            var root = _rootDirectoryPath;
            if (!Directory.Exists(root))
            {
                // Tworzymy katalog.
                try { Directory.CreateDirectory(root); }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while| |creating| " +
                        $"|directory| '{root}'.");
                }
            }
        }

        #region Errors
        protected Error DirectoryEntryAlreadyExists(string path) =>
            new Error($"|Directory entry| '{path}' |already exists.|");
        #endregion

        private string KeyToEntryPath(PrimaryKey key)
        {
            return Path.Combine(_rootDirectoryPath, KeyToEntryName(key));
        }

        protected abstract string KeyToEntryName(PrimaryKey key);

        public void Add(PrimaryKey key)
        {
            var path = KeyToEntryPath(key);
            if (EntryExists(path))
                throw DirectoryEntryAlreadyExists(path);

            try { CreateEntry(path); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |creating| " +
                    $"|directory entry| '{path}'.");
            }
        }

        // Wzorzec Template Method
        protected abstract bool EntryExists(string path);

        protected abstract void CreateEntry(string path);

        public List<PrimaryKey> GetAll()
        {
            try
            {
                return GetEntryPaths().Select(
                    e => EntryNameToKey(Path.GetFileName(e))).ToList();
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    $"|listing entries in directory| '{_rootDirectoryPath}'.");
            }
        }

        protected abstract string[] GetEntryPaths();

        protected abstract PrimaryKey EntryNameToKey(string name);

        public bool Exists(PrimaryKey key)
        {
            return EntryExists(KeyToEntryPath(key));
        }

        public void Update(PrimaryKey oldKey, PrimaryKey newKey)
        {
            var oldPath = KeyToEntryPath(oldKey);
            if (!EntryExists(oldPath))
                throw new Error($"|Directory entry| '{oldPath}' |does not exist.|");

            if (KeysEqual(oldKey, newKey))
            {
                // Jeżeli nie chcemy zmieniać wartości klucza głównego.
                return;
            }

            var newPath = KeyToEntryPath(newKey);
            if (EntryExists(newPath))
                throw DirectoryEntryAlreadyExists(newPath);

            try { MoveEntry(oldPath, newPath); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |renaming| " +
                    $"|directory entry| '{oldPath}'.");
            }
        }

        protected abstract bool KeysEqual(PrimaryKey a, PrimaryKey b);

        protected abstract void MoveEntry(string sourcePath, string destinationPath);

        public void Delete(PrimaryKey key)
        {
            var path = KeyToEntryPath(key);
            if (!EntryExists(path))
                return;
            /* Nie wyrzucamy wyjątku, jeżeli nie istnieje
            katalog, który chcemy usunąć - zakładamy, że
            operacja usuwania została wykonana. */

            try { DeleteEntry(path); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |deleting| " +
                    $"|directory entry| '{path}'.");
            }
        }

        protected abstract void DeleteEntry(string path);

        public void DeleteMany(Predicate<PrimaryKey> predicate)
        {
            var keys = GetAll();
            foreach (var key in keys)
            {
                if (predicate(key))
                    Delete(key);
            }
        }
    }
}
