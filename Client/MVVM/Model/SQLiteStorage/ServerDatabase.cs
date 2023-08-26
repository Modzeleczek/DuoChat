using Client.MVVM.Model.SQLiteStorage.Repositories;
using Shared.MVVM.Core;
using Shared.MVVM.Model.SQLiteStorage;
using System;
using System.Data.SQLite;
using System.IO;

namespace Client.MVVM.Model.SQLiteStorage
{
    public class ServerDatabase : SQLiteDatabase
    {
        #region Properties
        public AccountRepository Accounts { get; }
        #endregion

        public ServerDatabase(string path) : base(path)
        {
            Accounts = new AccountRepository(CreateConnection);

            CreateOrValidateFile();
        }

        protected override string DDLEmbeddedResource()
        {
            return "Client.MVVM.Model.SQLiteStorage.database.sql";
        }

        private void ValidateDatabase()
        {
            try
            {
                var query = "PRAGMA integrity_check;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    // cmd.CommandType = System.Data.CommandType.Text; // w SQLite dostępny jest tylko typ polecenia Text
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader.GetString(0) == "ok") // (string)reader["integrity_check"]
                            {
                                // w wyniku zapytania nie może być więcej niż tylko 1 rekord "ok"
                                if (!reader.Read())
                                    return;
                                else
                                    throw new Error("|SQLite integrity check returned " +
                                        "more than single 'ok' row.|");
                            }
                        }
                        throw new Error("|SQLite integrity check returned no rows.|");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    "|executing SQLite integrity check.|");
            }
        }

        private void CreateOrValidateFile()
        {
            if (File.Exists(_path))
            {
                ValidateDatabase();
                return;
            }

            // Plik bazy danych jeszcze nie istnieje i musi zostać utworzony.
            try { SQLiteConnection.CreateFile(_path); }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    $"|creating| |server's SQLite file| '{_path}'.");
            }

            try { RecreateSchema(); }
            catch (Error createSchemaError)
            {
                createSchemaError.Prepend("|Could not| " +
                    "|create| |server's SQLite schema|.");

                try { File.Delete(_path); }
                catch (Exception deleteException)
                {
                    /* TODO: w sytuacji "undo" Errorów, czyli np. jak
                    tu, że tworzymy plik i nie uda się go zainicjalizować
                    danymi, można w obiekcie createSchemaError, czyli
                    "pierwotnego" błędu, trzymać referencje do kolejnych
                    "łańcuchowych" błędów. */
                    var deleteError = new Error(deleteException,
                        "|Error occured while| |deleting| " +
                        $"|server's SQLite file| '{_path}'.");
                    createSchemaError.Append(deleteError.Message);
                }
                throw;
            }
        }
    }
}
