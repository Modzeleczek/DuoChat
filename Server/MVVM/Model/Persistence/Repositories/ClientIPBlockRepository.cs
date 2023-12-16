using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class ClientIPBlockRepository : Repository<ClientIPBlockDto, uint>
    {
        #region Fields
        private const string TABLE = "ClientIPBlock";
        private const string F_ip_address = "ip_address";
        #endregion

        public ClientIPBlockRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}({F_ip_address}) VALUES(@{F_ip_address});";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl, ClientIPBlockDto dto)
        {
            parColl.AddWithValue($"@{F_ip_address}", dto.IpAddress);
        }

        protected override uint GetInsertedKey(SQLiteConnection con, ClientIPBlockDto dto)
        {
            _ = con;
            return dto.GetRepositoryKey();
        }

        protected override string EntityName()
        {
            return "|client IP block|";
        }

        protected override string KeyName()
        {
            return "|IP address;M|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_ip_address} FROM {TABLE};";
        }

        protected override ClientIPBlockDto ReadOneEntity(SQLiteDataReader reader)
        {
            return new ClientIPBlockDto
            {
                IpAddress = (uint)(long)reader[F_ip_address]
            };
        }

        protected override string ExistsQuery()
        {
            return $"SELECT COUNT({F_ip_address}) FROM {TABLE} WHERE {F_ip_address} = @{F_ip_address};";
        }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl, uint key)
        {
            parColl.AddWithValue($"@{F_ip_address}", key);
        }

        protected override string GetQuery()
        {
            return $"SELECT {F_ip_address} FROM {TABLE} WHERE {F_ip_address} = @{F_ip_address};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_ip_address} = @new_{F_ip_address} " +
                $"WHERE {F_ip_address} = @{F_ip_address};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl,
            ClientIPBlockDto dto)
        {
            parColl.AddWithValue($"@new_{F_ip_address}", dto.IpAddress);
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} WHERE {F_ip_address} = @{F_ip_address};";
        }
    }
}
