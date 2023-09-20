using Shared.MVVM.Core;
using System;
using System.IO;

namespace Server.MVVM.Model.Persistence
{
    public class Storage
    {
        #region Fields
        private const string ROOT_DIRECTORY = "storage";
        public Database Database { get; }
        #endregion

        public Storage()
        {
            CreateRootDirectoryIfNotExists();

            /* Zapewniamy istnienie storage/database.sqlite
            Zapisujemy referencję, aby wykonać walidację pliku bazy danych
            tylko raz na początku programu, a nie przy każdym zapytaniu do niej.
            Jeżeli chcemy walidować przy każdym zapytaniu, to w Storage trzeba
            zrobić publiczne metody odpowiadające każdej możliwej operacji na 
            każdym repozytorium bazy danych i przy każdej operacji pobierać obiekt
            bazy danych za pomocą GetDatabase. */
            Database = GetDatabase();

            /* TODO: załączniki
            Zapewniamy istnienie storage/attachments/
            GetAttachmentsFileSystemStorage(); */
        }

        private void CreateRootDirectoryIfNotExists()
        {
            /* Konstruktor wykonujemy tylko raz na początku całego programu,
            przy konstruowaniu obiektu Server. */
            if (Directory.Exists(ROOT_DIRECTORY))
                return;

            try { Directory.CreateDirectory(ROOT_DIRECTORY); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |creating| " +
                    $"|directory| '{ROOT_DIRECTORY}'.");
            }
        }

        private Database GetDatabase()
        {
            // storage/database.sqlite
            return new Database(Path.Combine(ROOT_DIRECTORY, "database.sqlite"));
        }

        /* private void GetAttachmentsStorage()
        {
            // storage/attachments/
            return new AttachmentsFileSystemStorage(
                Path.Combine(ROOT_DIRECTORY, "attachments"));
        } */
    }
}
