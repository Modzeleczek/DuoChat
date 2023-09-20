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
    public class UserRepository : Repository
    {
        public UserRepository(Func<SQLiteConnection> connectionCreator) :
            base(connectionCreator)
        { }

        public void AddUser(UserDTO user)
        {
            EnsureUserExists(user.Login, false);

            try
            {
                var query = "INSERT INTO User(login, public_key) VALUES(@login, @public_key);";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", user.Login);
                    var bytes = user.PublicKey.ToBytesNoLength();
                    cmd.Parameters.Add("@public_key", DbType.Binary, bytes.Length).Value = bytes;
                    con.Open();
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        private void EnsureUserExists(string login, bool shouldExist)
        {
            // Login, z perspektywy aplikacji, jest identyfikatorem rekordu tabel User.
            bool userExists;
            try { userExists = UserExists(login); }
            catch (Error e)
            {
                e.Prepend("|Could not| |check if| |user| |already exists.|");
                throw;
            }

            if (shouldExist) // Powinien istnieć, a nie istnieje
            {
                if (!userExists)
                    throw UserDoesNotExistError(login);
            }
            else // Nie powinien istnieć, a istnieje
            {
                if (userExists)
                    throw new Error($"|User with login| {login} |already exists.|");
            }
        }

        private Error UserDoesNotExistError(string login) =>
            new Error($"|User with login| {login} |does not exist.|");

        public List<UserDTO> GetAllUsers()
        {
            try
            {
                var query = "SELECT id, login, public_key FROM User;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<UserDTO>();
                        while (reader.Read())
                        {
                            list.Add(ReadOneUser(reader));
                        }
                        return list;
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        private UserDTO ReadOneUser(SQLiteDataReader reader)
        {
            return new UserDTO
            {
                Id = (long)reader["id"], // reader.GetInt64(0)
                Login = (string)reader["login"], // reader.GetString(1)
                PublicKey = PublicKey.FromBytesNoLength((byte[])reader["public_key"])
            };
        }

        public bool UserExists(string login)
        {
            try
            {
                var query = "SELECT COUNT(id) FROM User WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    var count = (long)cmd.ExecuteScalar(); // nie da się zrzutować na int
                    if (count > 1)
                        throw new Error($"|More than one user with login| '{login}' |exists|.");
                    // powinno być możliwe tylko 0 lub 1, bo "login" to klucz główny tabeli Account
                    if (count == 1)
                        return true;
                    return false;
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public UserDTO GetUser(string login)
        {
            EnsureUserExists(login, true);

            try
            {
                var query = "SELECT id, login, public_key FROM User WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw UserDoesNotExistError(login);
                        return ReadOneUser(reader);
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public void UpdateUser(string login, UserDTO user)
        {
            // Czy stary użytkownik istnieje?
            EnsureUserExists(login, true);

            if (user.Login != login) // jeżeli zmieniamy login
            {
                // Czy nowy użytkownik jeszcze nie istnieje?
                EnsureUserExists(user.Login, false);
            }

            try
            {
                var query = "UPDATE User SET login = @new_login, public_key = @public_key WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@new_login", user.Login);
                    var bytes = user.PublicKey.ToBytesNoLength();
                    cmd.Parameters.Add("@public_key", DbType.Binary, bytes.Length).Value = bytes;
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    /* po sprawdzeniu na górze, że jest dokładnie 1 wiersz z loginem
                    "login" (nie user.Login), nie powinno się wykonać */
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }

        public void DeleteUser(string login)
        {
            EnsureUserExists(login, true);

            try
            {
                var query = "DELETE FROM User WHERE login = @login;";
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    con.Open();
                    /* UserExists wyrzuciłoby wyjątek, jeżeli istniałoby
                    kilka kont o tym samym loginie, który jest identyfikatorem
                    z perspektywy aplikacji */
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
    }
}
