using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class ConversationRepository : Repository<ConversationDto, ulong>
    {
        #region Fields
        private const string TABLE = "Conversation";
        private const string F_id = "id";
        private const string F_owner_id = "owner_id";
        private const string F_name = "name";
        #endregion

        public ConversationRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}({F_owner_id}, {F_name}) VALUES(@{F_owner_id}, @{F_name});";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl, ConversationDto dto)
        {
            parColl.AddWithValue($"@{F_owner_id}", dto.OwnerId);
            parColl.AddWithValue($"@{F_name}", dto.Name);
        }

        protected override ulong GetInsertedKey(SQLiteConnection con, ConversationDto dto)
        {
            dto.Id = (ulong)con.LastInsertRowId;
            return dto.GetRepositoryKey();
        }

        protected override string EntityName()
        {
            return "|conversation|";
        }

        protected override string KeyName()
        {
            return "|id;M|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_id}, {F_owner_id}, {F_name} FROM {TABLE};";
        }

        protected override ConversationDto ReadOneEntity(SQLiteDataReader reader)
        {
            return new ConversationDto
            {
                Id = (ulong)(long)reader[F_id],
                OwnerId = (ulong)(long)reader[F_owner_id],
                Name = (string)reader[F_name]
            };
        }

        protected override string ExistsQuery()
        {
            return $"SELECT COUNT({F_id}) FROM {TABLE} WHERE {F_id} = @{F_id};";
        }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl, ulong key)
        {
            parColl.AddWithValue($"@{F_id}", key);
        }

        protected override string GetQuery()
        {
            return $"SELECT {F_id}, {F_owner_id}, {F_name} FROM {TABLE} WHERE {F_id} = @{F_id};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_owner_id} = @{F_owner_id}, {F_name} = @{F_name} " +
                $"WHERE {F_id} = @{F_id};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl,
            ConversationDto dto)
        {
            parColl.AddWithValue($"@{F_owner_id}", dto.OwnerId);
            parColl.AddWithValue($"@{F_name}", dto.Name);
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} WHERE {F_id} = @{F_id};";
        }

        public IEnumerable<ConversationDto> GetByIds(IEnumerable<ulong> ids)
        {
            if (!ids.Any())
                return Enumerable.Empty<ConversationDto>();

            var query = $"SELECT * FROM {TABLE} WHERE {F_id} IN ({string.Join(',', ids)});";
            return ExecuteReader(query);
        }

        public IEnumerable<ConversationDto> GetByOwnerId(ulong ownerId)
        {
            var query = $"SELECT * FROM {TABLE} WHERE {F_owner_id} = {ownerId};";
            return ExecuteReader(query);
        }
    }
}
