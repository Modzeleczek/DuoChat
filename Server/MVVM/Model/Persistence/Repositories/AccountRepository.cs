using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System.Data;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    // Wzorzec DataAccessObject (DAO)
    // Id jest kluczem głównym, a login jest kluczem kandydującym.
    public abstract class AccountRepository : Repository<AccountDto, (ulong id, string login)>
    {
        #region Fields
        private const string TABLE = "Account";
        protected const string F_id = "id";
        protected const string F_login = "login";
        private const string F_public_key = "public_key";
        private const string F_is_blocked = "is_blocked";
        #endregion

        protected AccountRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}({F_login}, {F_public_key}, {F_is_blocked}) " +
                $"VALUES(@{F_login}, @{F_public_key}, @{F_is_blocked});";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl, AccountDto dto)
        {
            parColl.AddWithValue($"@{F_login}", dto.Login);
            var bytes = dto.PublicKey.ToBytesNoLength();
            parColl.Add($"@{F_public_key}", DbType.Binary, bytes.Length).Value = bytes;
            parColl.AddWithValue($"@{F_is_blocked}", dto.IsBlocked);
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
            return $"SELECT {F_id}, {F_login}, {F_public_key}, {F_is_blocked} FROM {TABLE};";
        }

        protected override AccountDto ReadOneEntity(SQLiteDataReader reader)
        {
            return new AccountDto
            {
                Id = (ulong)(long)reader[F_id], // reader.GetInt64(0)
                Login = (string)reader[F_id], // reader.GetString(1)
                PublicKey = PublicKey.FromBytesNoLength((byte[])reader[F_public_key]),
                IsBlocked = (byte)(long)reader[F_is_blocked]
            };
        }

        protected override string ExistsQuery()
        {
            return $"SELECT COUNT({F_id}) FROM {TABLE} WHERE {KeyCondition()};";
        }
        protected abstract string KeyCondition();

        protected override string GetQuery()
        {
            return $"SELECT {F_id}, {F_login}, {F_public_key}, {F_is_blocked} FROM {TABLE} WHERE {KeyCondition()};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_login} = @new_{F_login}, {F_public_key} = @{F_public_key} " +
                $"{F_is_blocked} = @{F_is_blocked} WHERE {KeyCondition()};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl, AccountDto dto)
        {
            parColl.AddWithValue($"@new_{F_login}", dto.Login);
            var bytes = dto.PublicKey.ToBytesNoLength();
            parColl.Add($"@{F_public_key}", DbType.Binary, bytes.Length).Value = bytes;
            parColl.AddWithValue($"@{F_is_blocked}", dto.IsBlocked);
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} WHERE {KeyCondition()} ;";
        }
    }
}
