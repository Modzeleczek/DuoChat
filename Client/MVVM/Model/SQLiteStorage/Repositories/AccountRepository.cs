using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
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
            try
            {
                if (AccountExists(account.Login))
                    throw AccountExistsError(account.Login);
            }
            catch (Error e)
            {
                e.Prepend(CheckingAccountExistError());
            }

            try
            {
                var query = "INSERT INTO Account (login, private_key) VALUES (@p0, @p1);";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@p0", account.Login);
                    var bytes = account.PrivateKey.ToBytes();
                    cmd.Parameters.Add("@p1", DbType.Binary, bytes.Length).Value = bytes;
                    con.Open();
                    var count = cmd.ExecuteNonQuery();
                    if (count != 1)
                        throw new Error(
                            "|Number of rows affected by the query is other than 1.|");
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

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
                            var login = (string)reader["login"];
                            var privateKey = (byte[])reader["private_key"];
                            list.Add(new Account
                            {
                                Login = login,
                                PrivateKey = Shared.MVVM.Model.Cryptography.PrivateKey
                                    .FromBytes(privateKey)
                            });
                        }
                        return list;
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public bool AccountExists(string login)
        {
            try
            {
                var query = "SELECT COUNT(login) FROM Account WHERE login = @p0;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@p0", login);
                    con.Open();
                    var count = (long)cmd.ExecuteScalar(); // nie da się zrzutować na int
                    if (count > 1)
                        throw new Error("|More than one account with login| " +
                            $"'{login}' |exist.|");
                    // powinno być możliwe tylko 0 lub 1, bo "login" to klucz główny tabeli Account
                    if (count == 1)
                        return true;
                    return false;
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        /* public Account GetAccount(string login)
        {
            ValidateDatabase();
        } */

        public void UpdateAccount(string login, Account account)
        {
            try
            {
                // stare konto nie istnieje
                if (!AccountExists(login))
                    throw AccountDoesNotExistError(login);
            }
            catch (Error e)
            {
                e.Prepend(CheckingAccountExistError());
                throw;
            }

            if (account.Login != login) // jeżeli zmieniamy login
            {
                try
                {
                    // nowe konto już istnieje
                    if (AccountExists(account.Login))
                        throw AccountExistsError(account.Login);
                }
                catch (Error e)
                {
                    e.Prepend(CheckingAccountExistError());
                    throw;
                }
            }

            try
            {
                var query = "UPDATE Account SET login = @p0, private_key = @p1 WHERE login = @p2;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@p0", account.Login);
                    var bytes = account.PrivateKey.ToBytes();
                    cmd.Parameters.Add("@p1", DbType.Binary, bytes.Length).Value = bytes;
                    cmd.Parameters.AddWithValue("@p2", login);
                    con.Open();
                    var count = cmd.ExecuteNonQuery();
                    /* po sprawdzeniu na górze, że jest dokładnie 1 wiersz z loginem
                    "login" (nie account.Login), nie powinno się wykonać */
                    if (count != 1)
                        throw new Error(
                            "|Number of rows affected by the query is other than 1.|");
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        private Error AccountDoesNotExistError(string login) =>
            new Error($"|Account with login| {login} |does not exist.|");

        private string CheckingAccountExistError() =>
            "|Error occured while| |checking if| |account| |already exists.|";

        private Error AccountExistsError(string login) =>
            new Error($"|Account with login| {login} |already exists.|");

        public void DeleteAccount(string login)
        {
            try
            {
                if (!AccountExists(login))
                    throw AccountDoesNotExistError(login);
            }
            catch (Error e)
            {
                e.Prepend(CheckingAccountExistError());
                throw;
            }

            try
            {
                var query = "DELETE FROM Account WHERE login = @p0;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@p0", login);
                    con.Open();
                    var count = cmd.ExecuteNonQuery();
                    /* AccountExists wyrzuciłoby wyjątek, jeżeli istniałoby
                    kilka kont o tym samym loginie, który jest kluczem głównym */
                    if (count != 1)
                        throw new Error(
                            "|Number of rows affected by the query is other than 1.|");
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
    }
}
