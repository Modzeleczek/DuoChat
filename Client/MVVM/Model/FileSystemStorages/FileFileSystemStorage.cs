using Shared.MVVM.Core;
using System;
using System.IO;

namespace Client.MVVM.Model.FileSystemStorages
{
    public abstract class FileFileSystemStorage<PrimaryKey> : FileSystemStorage<PrimaryKey>
    {
        public FileFileSystemStorage(string rootDirectoryPath)
            : base(rootDirectoryPath)
        {
            // Nie usuwamy poza procedurą CRUDa.
            // RemoveSubdirectories();
        }

        private void RemoveSubdirectories()
        {
            // Usuwamy podkatalogi, bo FileFileSystemStorage
            // może przechowywać tylko pliki.

            var root = _rootDirectoryPath;
            // Tworzymy listę podkatalogów z katalogu.
            string[] subdirectories;
            try { subdirectories = Directory.GetDirectories(root); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    $"|listing subdirectories in directory| '{root}'.");
            }

            // Usuwamy z katalogu wszystkie podkatalogi.
            foreach (var subdirectory in subdirectories)
            {
                try { Directory.Delete(subdirectory, true); }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while| " +
                        $"|deleting| |subdirectory| '{subdirectory}'.");
                }
            }
        }

        protected override bool EntryExists(string path)
        {
            return File.Exists(path);
        }

        protected override void CreateEntry(string path)
        {
            File.Create(path);
        }

        protected override string[] GetEntryPaths()
        {
            // Zwraca tablicę ścieżek do plików.
            return Directory.GetFiles(_rootDirectoryPath);
        }

        protected override void MoveEntry(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        protected override void DeleteEntry(string path)
        {
            File.Delete(path);
        }
    }
}
