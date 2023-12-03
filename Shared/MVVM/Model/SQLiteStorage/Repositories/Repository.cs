using Shared.MVVM.Core;
using System;
using System.Collections.Generic;
using Shared.MVVM.Model.SQLiteStorage.DTO;
using System.Data.SQLite;

namespace Shared.MVVM.Model.SQLiteStorage.Repositories
{
    public abstract class Repository<EntityDtoT, KeyT> where EntityDtoT : IDto<KeyT>
    {
        private readonly ISQLiteConnector _sqliteConnector;

        protected Repository(ISQLiteConnector sqliteConnector)
        {
            _sqliteConnector = sqliteConnector;
        }

        protected SQLiteConnection CreateConnection() => _sqliteConnector.CreateConnection();

        #region CRUD
        public void Add(EntityDtoT dto)
        {
            EnsureEntityExists(dto.GetRepositoryKey(), false);

            try
            {
                var query = AddQuery();
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    SetAddParameters(cmd.Parameters, dto);
                    con.Open();
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
        protected abstract string AddQuery();
        protected abstract void SetAddParameters(SQLiteParameterCollection parColl, EntityDtoT dto);

        protected Error NotExactly1RowError() =>
            new Error("|Number of rows affected by the query is other than 1.|");

        protected Error QueryError(Exception? inner = null) =>
            new Error(inner, "|Error occured while| |executing query.|");

        private void EnsureEntityExists(KeyT key, bool shouldExist)
        {
            /* "key", z perspektywy aplikacji, jest identyfikatorem rekordu tabeli
            obsługiwanej przez dane Repository. Nie musi to być klucz główny tabeli,
            ale raczej powinien to być przynajmniej klucz kandydujący, który jednoznacznie
            identyfikuje każdy rekord tabeli. */
            bool entityExists;
            try { entityExists = Exists(key); }
            catch (Error e)
            {
                e.Prepend($"|Could not| |check if| {EntityName()} |already exists.|");
                throw;
            }

            if (shouldExist) // Powinien istnieć, a nie istnieje.
            {
                if (!entityExists)
                    throw EntityDoesNotExistError(key);
            }
            else // Nie powinien istnieć, a istnieje.
            {
                if (entityExists)
                    throw new Error($"{EntityName()} |with| {KeyName()} {key} |already exists.|");
            }
        }
        protected abstract string EntityName();
        protected abstract string KeyName();

        private Error EntityDoesNotExistError(KeyT key) =>
            new Error($"{EntityName()} |with| {KeyName()} {key} |does not exist.|");

        public List<EntityDtoT> GetAll()
        {
            try
            {
                var query = GetAllQuery();
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<EntityDtoT>();
                        while (reader.Read())
                            list.Add(ReadOneEntity(reader));
                        return list;
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
        protected abstract string GetAllQuery();
        protected abstract EntityDtoT ReadOneEntity(SQLiteDataReader reader);

        public bool Exists(KeyT key)
        {
            try
            {
                var query = ExistsQuery();
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    SetKeyParameter(cmd.Parameters, key);
                    con.Open();

                    object? result = cmd.ExecuteScalar();
                    if (result is null)
                        throw new Error($"'{query}' |returned no rows|.");

                    // Nie da się zrzutować na int (chyba).
                    var count = (long)result;
                    if (count > 1)
                        throw new Error($"|More than 1| {EntityName()} " +
                            $"|with| {KeyName()} '{key}' |exists|.");
                    // Powinno być możliwe tylko 0 lub 1, bo key to klucz kandydujący tabeli.
                    if (count == 1)
                        return true;
                    return false;
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
        protected abstract string ExistsQuery();
        protected abstract void SetKeyParameter(SQLiteParameterCollection parColl, KeyT key);

        public EntityDtoT Get(KeyT key)
        {
            EnsureEntityExists(key, true);

            try
            {
                var query = GetQuery();
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    SetKeyParameter(cmd.Parameters, key);
                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw EntityDoesNotExistError(key);
                        return ReadOneEntity(reader);
                    }
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
        protected abstract string GetQuery();

        public void Update(KeyT key, EntityDtoT dto)
        {
            // Czy stara encja istnieje?
            EnsureEntityExists(key, true);

            var dtoKey = dto.GetRepositoryKey();
            /* Zakładamy, że KeyT jest typem prostym (wartościowym lub stringiem)
            lub co najwyżej krotką (tuple) zawierającą tylko typy proste (w takiej
            krotce Equals porównuje parami jej kolejne pola). */
            if (!KeysEqual(key, dtoKey))
            {
                // Jeżeli zmieniamy klucz encji.
                // Czy nowa encja jeszcze nie istnieje?
                EnsureEntityExists(dtoKey, false);
            }

            try
            {
                var query = UpdateQuery();
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    SetUpdateParameters(cmd.Parameters, dto);
                    SetKeyParameter(cmd.Parameters, key);
                    con.Open();
                    /* Po sprawdzeniu na górze, że jest dokładnie 1 encja z kluczem
                    "key" (nie dtoKey), nie powinno się wykonać. */
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
        protected virtual bool KeysEqual(KeyT key1, KeyT key2)
        {
            return key1!.Equals(key2);
        }
        protected abstract string UpdateQuery();
        protected abstract void SetUpdateParameters(SQLiteParameterCollection parColl,
            EntityDtoT dto);

        public void Delete(KeyT key)
        {
            EnsureEntityExists(key, true);

            try
            {
                var query = DeleteQuery();
                using (var con = CreateConnection())
                using (var cmd = new SQLiteCommand(query, con))
                {
                    SetKeyParameter(cmd.Parameters, key);
                    con.Open();
                    /* Repository.Exists wyrzuciłoby wyjątek, jeżeli istniałoby
                    kilka encji o tym samym kluczu, który jest identyfikatorem
                    z perspektywy aplikacji. */
                    if (cmd.ExecuteNonQuery() != 1)
                        throw NotExactly1RowError();
                }
            }
            catch (Exception e) { throw QueryError(e); }
        }
        protected abstract string DeleteQuery();
        #endregion
    }
}
