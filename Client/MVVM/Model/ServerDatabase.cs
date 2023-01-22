using Shared.MVVM.Model;
using Shared.MVVM.View.Localization;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Client.MVVM.Model
{
    public class ServerDatabase
    {
        private static readonly Translator d = Translator.Instance;
        private string path;

        public ServerDatabase(string path)
        {
            this.path = path;
        }

        private Status FileDoesNotExistStatus(int code) =>
            new Status(code, null, d["Server database file"], $"({path})", d["does not exist."]);

        private SQLiteConnection CreateConnection()
        {
            var connectionString = $"Data Source={path}; Version=3; New=True; Compress=True; " +
                $"foreign keys=true; Journal Mode=Off";
            return new SQLiteConnection(connectionString, true);
        }

        // https://stackoverflow.com/a/3314213/14357934
        private string ReadEmbeddedResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            using (Stream stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null) return null;
                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        public Status ResetDatabase()
        {
            string ddl = ReadEmbeddedResource("Client.MVVM.Model.client.sql");
            if (ddl == null)
                throw new KeyNotFoundException(
                    "Embedded resource with client database SQL code does not exist.");
            if (!File.Exists(path))
                return FileDoesNotExistStatus(-1);
            try
            {
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(ddl, con))
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return new Status(0);
                }
            }
            catch (Exception)
            {
                return new Status(-2, null, d["Error occured while"],
                    d["executing DDL code creating server database."]); // -2
            }
        }

        public Status DatabaseFileHealthy()
        {
            if (!File.Exists(path))
                return FileDoesNotExistStatus(-1); // -1
            try
            {
                string q = "PRAGMA integrity_check;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(q, con))
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
                                    return new Status(0);
                            }
                        }
                        return new Status(1);
                    }
                }
            }
            catch (Exception)
            {
                return new Status(-2, null, d["Error occured while"],
                    d["validating server's database."]); // -2
            }
        }

        public Status GetAllAccounts()
        {
            if (!File.Exists(path))
                return FileDoesNotExistStatus(-1); // -1
            string q = "SELECT login, private_key FROM Account;";
            using (var con = CreateConnection())
            using (var cmd = new SQLiteCommand(q, con))
            {
                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    var list = new List<Account>();
                    while (reader.Read())
                    {
                        var login = (string)reader["login"];
                        var privateKey = (byte[])reader["private_key"];
                        list.Add(new Account
                        {
                            Login = login,
                            PrivateKey = Shared.MVVM.Model.Cryptography.PrivateKey.FromBytes(privateKey)
                        });;
                    }
                    return new Status(0, list);
                }
            }
        }
    }
}
