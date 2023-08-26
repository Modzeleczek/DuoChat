using Shared.MVVM.Core;
using System;
using System.IO;

namespace Client.MVVM.Model.FileSystemStorages
{
    public abstract class DirectoryFileSystemStorage<PrimaryKey> : FileSystemStorage<PrimaryKey>
    {
        protected DirectoryFileSystemStorage(string rootDirectoryPath)
            : base(rootDirectoryPath)
        {
            // Nie usuwamy poza procedurą CRUDa.
            // RemoveFiles();
        }

        private void RemoveFiles()
        {
            // Usuwamy pliki, bo DirectoryFileSystemStorage
            // może przechowywać tylko podkatalogi.

            var root = _rootDirectoryPath;
            // Tworzymy listę plików z katalogu.
            string[] files;
            try { files = Directory.GetFiles(root); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    $"|listing files in directory| '{root}'.");
            }

            // Usuwamy z katalogu wszystkie pliki.
            foreach (var file in files)
            {
                try { File.Delete(file); }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while| " +
                        $"|deleting| |file| '{file}'.");
                }
            }
        }

        protected override bool EntryExists(string path)
        {
            return Directory.Exists(path);
        }

        protected override void CreateEntry(string path)
        {
            Directory.CreateDirectory(path);
        }

        protected override string[] GetEntryPaths()
        {
            // Zwraca tablicę ścieżek do podkatalogów.
            return Directory.GetDirectories(_rootDirectoryPath);
        }

        protected override void MoveEntry(string sourcePath, string destinationPath)
        {
            Directory.Move(sourcePath, destinationPath);
        }

        protected override void DeleteEntry(string path)
        {
            Directory.Delete(path, true);
        }
    }
}
