using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class AttachmentRepository : Repository<AttachmentDto, ulong>
    {
        #region Fields
        private const string TABLE = "Attachment";
        private const string F_id = "id";
        private const string F_message_id = "message_id";
        private const string F_name = "name";
        #endregion

        public AttachmentRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}({F_message_id}, {F_name}) " +
                $"VALUES(@{F_message_id}, @{F_name});";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl, AttachmentDto dto)
        {
            parColl.AddWithValue($"@{F_message_id}", dto.MessageId);
            parColl.AddWithValue($"@{F_name}", dto.Name);
        }

        protected override ulong GetInsertedKey(SQLiteConnection con, AttachmentDto dto)
        {
            dto.Id = (ulong)con.LastInsertRowId;
            return dto.GetRepositoryKey();
        }

        protected override string EntityName()
        {
            return "|attachment|";
        }

        protected override string KeyName()
        {
            return "|id;M|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_id}, {F_message_id}, {F_name} FROM {TABLE};";
        }

        protected override AttachmentDto ReadOneEntity(SQLiteDataReader reader)
        {
            return new AttachmentDto
            {
                Id = (ulong)(long)reader[F_id],
                MessageId = (ulong)(long)reader[F_message_id],
                Name = (string)reader[F_name],
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
            return $"SELECT {F_id}, {F_message_id}, {F_name} FROM {TABLE} WHERE {F_id} = @{F_id};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_message_id} = @{F_message_id}, {F_name} = @{F_name} " +
                $"WHERE {F_id} = @{F_id};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl, AttachmentDto dto)
        {
            parColl.AddWithValue($"@{F_message_id}", dto.MessageId);
            parColl.AddWithValue($"@{F_name}", dto.Name);
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} WHERE {F_id} = @{F_id};";
        }
    }
}
