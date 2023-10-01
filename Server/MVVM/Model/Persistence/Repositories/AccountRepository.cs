using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    // Wzorzec DataAccessObject (DAO)
    public class AccountRepository : Repository
    {
        public AccountRepository(Func<SQLiteConnection> connectionCreator) :
            base(connectionCreator)
        { }

        public void AddAccount(AccountDTO account)
        {
            EnsureAccountExists(account.Login, false);

            try
            {
                var query = "INSERT INTO Account(login, public_key) VALUES(@login, @public_key);";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", account.Login);
                    var bytes = account.PublicKey.ToBytesNoLength();
                    cmd.Parameters.Add("@public_key", DbType.Binary, bytes.Length).Value = bytes;
                    con.Open();
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        private void EnsureAccountExists(string login, bool shouldExist)
        {
            // Login, z perspektywy aplikacji, jest identyfikatorem rekordu tabel Account.
            bool accountExists;
            try { accountExists = AccountExists(login); }
            catch (Error e)
            {
                e.Prepend("|Could not| |check if| |account| |already exists.|");
                throw;
            }

            if (shouldExist) // Powinien istnieć, a nie istnieje
            {
                if (!accountExists)
                    throw AccountDoesNotExistError(login);
            }
            else // Nie powinien istnieć, a istnieje
            {
                if (accountExists)
                    throw new Error($"|Account with login| {login} |already exists.|");
            }
        }

        private Error AccountDoesNotExistError(string login) =>
            new Error($"|Account with login| {login} |does not exist.|");

        public List<AccountDTO> GetAllAccounts()
        {
            try
            {
                var query = "SELECT id, login, public_key FROM Account;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<AccountDTO>();
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

        private AccountDTO ReadOneAccount(SQLiteDataReader reader)
        {
            return new AccountDTO
            {
                Id = (long)reader["id"], // reader.GetInt64(0)
                Login = (string)reader["login"], // reader.GetString(1)
                PublicKey = PublicKey.FromBytesNoLength((byte[])reader["public_key"])
            };
        }

        public bool AccountExists(string login)
        {
            try
            {
                var query = "SELECT COUNT(id) FROM Account WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    var count = (long)cmd.ExecuteScalar(); // nie da się zrzutować na int
                    if (count > 1)
                        throw new Error($"|More than one account with login| '{login}' |exists|.");
                    // powinno być możliwe tylko 0 lub 1, bo "login" to pole unikalne tabeli Account
                    if (count == 1)
                        return true;
                    return false;
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public AccountDTO GetAccount(string login)
        {
            EnsureAccountExists(login, true);

            try
            {
                var query = "SELECT id, login, public_key FROM Account WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw AccountDoesNotExistError(login);
                        return ReadOneAccount(reader);
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public void UpdateAccount(string login, AccountDTO account)
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
                var query = "UPDATE Account SET login = @new_login, public_key = @public_key " +
                    "WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@new_login", account.Login);
                    var bytes = account.PublicKey.ToBytesNoLength();
                    cmd.Parameters.Add("@public_key", DbType.Binary, bytes.Length).Value = bytes;
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
                    kilka kont o tym samym loginie, który jest identyfikatorem
                    z perspektywy aplikacji. */
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
    }
}
