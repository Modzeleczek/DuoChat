using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using Shared.MVVM.Model.SQLiteStorage;
using Client.MVVM.ViewModel.Observables;
using System.Data.SQLite;
using System.Data;

namespace Client.MVVM.Model.SQLiteStorage.Repositories
{
    public class AccountRepository : Repository<Account, string>
    {
        #region Fields
        private const string TABLE = "Account";
        private const string F_login = "login";
        private const string F_private_key = "private_key";
        #endregion

        public AccountRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        #region Errors
        public static string AlreadyExistsMsg(string login) =>
            $"|Account with login| {login} |already exists.|";

        public static string NotExistsMsg(string login) =>
            $"|Account with login| {login} |does not exist.|";
        #endregion

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}({F_login}, {F_private_key}) VALUES(@{F_login}, @{F_private_key});";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl, Account dto)
        {
            parColl.AddWithValue($"@{F_login}", dto.Login);
            var bytes = dto.PrivateKey.ToBytes();
            parColl.Add($"@{F_private_key}", DbType.Binary, bytes.Length).Value = bytes;
        }

        protected override string EntityName()
        {
            return "|account|";
        }

        protected override string KeyName()
        {
            return "|login;M|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_login}, {F_private_key} FROM {TABLE};";
        }

        protected override Account ReadOneEntity(SQLiteDataReader reader)
        {
            return new Account
            {
                // (ulong)(long)
                Login = (string)reader[F_login], // reader.GetString(0)
                PrivateKey = PrivateKey.FromBytes((byte[])reader[F_private_key])
            };
        }

        protected override string ExistsQuery()
        {
            return $"SELECT COUNT({F_login}) FROM {TABLE} WHERE {F_login} = @{F_login};";
        }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl, string key)
        {
            parColl.AddWithValue($"@{F_login}", key);
        }

        protected override string GetQuery()
        {
            return $"SELECT {F_login}, {F_private_key} FROM {TABLE} WHERE {F_login} = @{F_login};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_login} = @new_{F_login}, {F_private_key} = @{F_private_key} " +
                $"WHERE {F_login} = @{F_login};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl, Account dto)
        {
            parColl.AddWithValue($"@new_{F_login}", dto.Login);
            var bytes = dto.PrivateKey.ToBytes();
            parColl.Add($"@{F_private_key}", DbType.Binary, bytes.Length).Value = bytes;
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} WHERE {F_login} = @{F_login};";
        }
    }
}
