using Shared.MVVM.Model;
using Shared.MVVM.View.Localization;
using System;
using System.Collections.Generic;
using System.Data;
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

        private Status QueryErrorStatus(int code) =>
            new Status(code, null, d["Error occured while"], d["executing query."]);

        private Status ConnectionErrorStatus(int code) =>
            new Status(code, null, d["Error occured while"],
                d["connecting to server's database file."]);

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
                    d["executing DDL query creating server database."]); // -2
            }
        }

        private Status DatabaseFileHealthy()
        {
            if (!File.Exists(path))
                return FileDoesNotExistStatus(-1); // -1
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
                                    return new Status(0); // 0
                            }
                        }
                        return new Status(1); // 1
                    }
                }
            }
            catch (Exception)
            {
                return new Status(-2, null, d["Error occured while"],
                    d["executing SQLite integrity check."]); // -2
            }
        }

        private Status DatabaseStateValid()
        {
            var healthyStatus = DatabaseFileHealthy();
            if (healthyStatus.Code < 0)
                return healthyStatus.Prepend(-1, d["Error occured while"],
                    d["validating server's database."]); // -1
            return healthyStatus; // 0, 1
        }

        public Status AddAccount(Account account)
        {
            var checkStatus = DatabaseStateValid();
            if (checkStatus.Code != 0)
                return checkStatus.Prepend(-1); // -1

            SQLiteConnection con = null;
            try { con = CreateConnection(); }
            catch (Exception)
            {
                con?.Dispose();
                return ConnectionErrorStatus(-2); // -2
            }

            try
            {
                var existsStatus = AccountExists(con, account.Login);
                if (existsStatus.Code < 0)
                    return existsStatus.Prepend(-4, d["Error occured while"],
                        d["checking if"], d["account"], d["already exists."]); // -4
                if (existsStatus.Code == 0)
                    return existsStatus.Prepend(-5); // -5

                var query = "INSERT INTO Account (login, private_key) VALUES (@p0, @p1);";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@p0", account.Login);
                    var bytes = account.PrivateKey.ToBytes();
                    cmd.Parameters.Add("@p1", DbType.Binary, bytes.Length).Value = bytes;
                    // con.Open było już wywołane w AccountExists
                    var count = cmd.ExecuteNonQuery();
                    if (count != 1)
                        return new Status(-6,
                            d["Number of rows affected by the query is other than 1."]); // -6
                    return new Status(0); // 0
                }
            }
            catch (Exception)
            { return QueryErrorStatus(-3); } // -3
            finally { con?.Dispose(); }
        }

        public Status GetAllAccounts()
        {
            var checkStatus = DatabaseStateValid();
            if (checkStatus.Code != 0)
                return checkStatus.Prepend(-1); // -1

            SQLiteConnection con = null;
            try { con = CreateConnection(); }
            catch (Exception)
            {
                con?.Dispose();
                return ConnectionErrorStatus(-2); // -2
            }

            try
            {
                var query = "SELECT login, private_key FROM Account;";
                using (var cmd = new SQLiteCommand(query, con))
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
                                PrivateKey = Shared.MVVM.Model.Cryptography.PrivateKey
                                    .FromBytes(privateKey)
                            });
                        }
                        return new Status(0, list); // 0
                    }
                }
            }
            catch (Exception) { return QueryErrorStatus(-3); } // -3
            finally { con?.Dispose(); }
        }

        private Status AccountExists(SQLiteConnection con, string login)
        {
            try
            {
                var query = "SELECT COUNT(login) FROM Account WHERE login = @p0;";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@p0", login);
                    con.Open();
                    var count = (long)cmd.ExecuteScalar(); // nie da się zrzutować na int
                    if (count > 1)
                        return new Status(-2, null, d["More than one account with login"],
                            $"'{login}'", d["exist."]); // -2
                    // powinno być możliwe tylko 0 lub 1, bo "login" to klucz główny tabeli Account
                    if (count == 1)
                        return new Status(0, null, d["Account with login"], $"{login}",
                            d["already exists."]); // 0
                    return new Status(1, null, d["Account with login"], $"{login}",
                            d["does not exist."]); // 1
                }
            }
            catch (Exception) { return QueryErrorStatus(-1); } // -1
        }

        public Status AccountExists(string login)
        {
            var checkStatus = DatabaseStateValid();
            if (checkStatus.Code != 0)
                return checkStatus.Prepend(-1); // -1

            SQLiteConnection con = null;
            try { con = CreateConnection(); }
            catch (Exception)
            {
                con?.Dispose();
                return ConnectionErrorStatus(-2); // -2
            }

            try
            {
                var existsStatus = AccountExists(con, login);
                if (existsStatus.Code < 0)
                    // przesuwamy kody błędów o -3, bo w aktualnej funkcji już był kod -1, -2, -3
                    return existsStatus.Prepend(existsStatus.Code - 3);
                return existsStatus; // 0, 1
            }
            catch (Exception) { return QueryErrorStatus(-3); } // -3
            finally { con?.Dispose(); }
        }

        /* public Status GetAccount(string login)
        {
            var checkStatus = DatabaseStateValid();
            if (checkStatus.Code != 0)
                return checkStatus.Prepend(-1); // -1
        }

        public Status UpdateAccount(string login, Account account)
        {
            var checkStatus = DatabaseStateValid();
            if (checkStatus.Code != 0)
                return checkStatus.Prepend(-1); // -1
        } */

        public Status DeleteAccount(string login)
        {
            var checkStatus = DatabaseStateValid();
            if (checkStatus.Code != 0)
                return checkStatus.Prepend(-1); // -1

            SQLiteConnection con = null;
            try { con = CreateConnection(); }
            catch (Exception)
            {
                con?.Dispose();
                return ConnectionErrorStatus(-2); // -2
            }

            try
            {
                var existsStatus = AccountExists(con, login);
                if (existsStatus.Code < 0)
                    return existsStatus.Prepend(-4, d["Error occured while"],
                        d["checking if"], d["account"], d["already exists."]); // -4
                if (existsStatus.Code == 1)
                    return existsStatus.Prepend(-5); // -5

                var query = "DELETE FROM Account WHERE login = @p0;";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@p0", login);
                    con.Open();
                    var count = cmd.ExecuteNonQuery();
                    if (count != 1)
                        return new Status(-6,
                            d["Number of rows affected by the query is other than 1."]); // -6
                    return new Status(0); // 0
                }
            }
            catch (Exception) { return QueryErrorStatus(-3); } // -3
            finally { con?.Dispose(); }
        }
    }
}
