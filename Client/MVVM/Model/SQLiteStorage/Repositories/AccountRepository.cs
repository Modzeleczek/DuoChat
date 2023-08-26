using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace Client.MVVM.Model.SQLiteStorage.Repositories
{
    public class AccountRepository : Repository
    {
        public AccountRepository(Func<SQLiteConnection> connectionCreator) :
            base(connectionCreator)
        { }

        public void AddAccount(Account account)
        {
            EnsureAccountExists(account.Login, false);

            try
            {
                var query = "INSERT INTO Account (login, private_key) VALUES (@login, @private_key);";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", account.Login);
                    var bytes = account.PrivateKey.ToBytes();
                    cmd.Parameters.Add("@private_key", DbType.Binary, bytes.Length).Value = bytes;
                    con.Open();
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        private void EnsureAccountExists(string login, bool shouldExist)
        {
            try
            {
                if (shouldExist) // Powinien istnieć, a nie istnieje
                {
                    if (!AccountExists(login))
                        throw new Error(NotExistsMsg(login));
                }
                else // Nie powinien istnieć, a istnieje
                {
                    if (AccountExists(login))
                        throw new Error(AlreadyExistsMsg(login));
                }
            }
            catch (Error e)
            {
                e.Prepend("|Could not| |check if| |account| |already exists.|");
                throw;
            }
        }

        #region Errors
        public static string AlreadyExistsMsg(string login) =>
            $"|Account with login| {login} |already exists.|";

        public static string NotExistsMsg(string login) =>
            $"|Account with login| {login} |does not exist.|";
        #endregion

        public List<Account> GetAllAccounts()
        {
            try
            {
                var query = "SELECT login, private_key FROM Account;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<Account>();
                        while (reader.Read())
                        {
                            list.Add(ReadOneAccount(reader));
                        }
                        return list;
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        private Account ReadOneAccount(SQLiteDataReader reader)
        {
            return new Account
            {
                Login = (string)reader["login"], // reader.GetString(0)
                PrivateKey = PrivateKey.FromBytes((byte[])reader["private_key"])
            };
        }

        public bool AccountExists(string login)
        {
            try
            {
                var query = "SELECT COUNT(login) FROM Account WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    var count = (long)cmd.ExecuteScalar(); // nie da się zrzutować na int
                    if (count > 1)
                        throw new Error($"|More than one account with login| '{login}' |exists|.");
                    // powinno być możliwe tylko 0 lub 1, bo "login" to klucz główny tabeli Account
                    if (count == 1)
                        return true;
                    return false;
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public Account GetAccount(string login)
        {
            EnsureAccountExists(login, true);

            try
            {
                var query = "SELECT login, private_key FROM Account WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new Error(NotExistsMsg(login));
                        return ReadOneAccount(reader);
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public void UpdateAccount(string login, Account account)
        {
            // Czy stare konto istnieje?
            EnsureAccountExists(login, true);

            if (account.Login != login) // jeżeli zmieniamy login
            {
                // Czy nowe konto jeszcze nie istnieje?
                EnsureAccountExists(account.Login, false);
            }

            try
            {
                var query = "UPDATE Account SET login = @new_login, private_key = @private_key WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@new_login", account.Login);
                    var bytes = account.PrivateKey.ToBytes();
                    cmd.Parameters.Add("@private_key", DbType.Binary, bytes.Length).Value = bytes;
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    /* po sprawdzeniu na górze, że jest dokładnie 1 wiersz z loginem
                    "login" (nie account.Login), nie powinno się wykonać */
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public void DeleteAccount(string login)
        {
            EnsureAccountExists(login, true);

            try
            {
                var query = "DELETE FROM Account WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    /* AccountExists wyrzuciłoby wyjątek, jeżeli istniałoby
                    kilka kont o tym samym loginie, który jest kluczem głównym */
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
    }
}
