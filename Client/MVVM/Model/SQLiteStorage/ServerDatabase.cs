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
        }

        protected override string DDLEmbeddedResource()
        {
            return "Client.MVVM.Model.SQLiteStorage.client.sql";
        }

        private void ValidateDatabase()
        {
            if (!File.Exists(_path))
                throw FileDoesNotExistError();
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
    }
}
