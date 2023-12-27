using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class MessageRepository : Repository<MessageDto, ulong>
    {
        #region Fields
        private const string TABLE = "Message";
        private const string F_id = "id";
        private const string F_conversation_id = "conversation_id";
        private const string F_sender_id = "sender_id";
        private const string F_send_time = "send_time";
        #endregion

        public MessageRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}({F_conversation_id}, {F_sender_id}, {F_send_time}) " +
                $"VALUES(@{F_conversation_id}, @{F_sender_id}, @{F_send_time});";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl, MessageDto dto)
        {
            parColl.AddWithValue($"@{F_conversation_id}", dto.ConversationId);
            parColl.AddWithValue($"@{F_sender_id}", dto.SenderId);
            parColl.AddWithValue($"@{F_send_time}", dto.SendTime);
        }

        protected override ulong GetInsertedKey(SQLiteConnection con, MessageDto dto)
        {
            dto.Id = (ulong)con.LastInsertRowId;
            return dto.GetRepositoryKey();
        }

        protected override string EntityName()
        {
            return "|message|";
        }

        protected override string KeyName()
        {
            return "|id;M|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_id}, {F_conversation_id}, {F_sender_id}, {F_send_time} FROM {TABLE};";
        }

        protected override MessageDto ReadOneEntity(SQLiteDataReader reader)
        {
            return new MessageDto
            {
                Id = (ulong)(long)reader[F_id],
                ConversationId = (ulong)(long)reader[F_conversation_id],
                SenderId = (ulong)(long)reader[F_sender_id],
                SendTime = (long)reader[F_send_time]
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
            return $"SELECT {F_id}, {F_conversation_id}, {F_sender_id}, {F_send_time} " +
                $"FROM {TABLE} WHERE {F_id} = @{F_id};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_conversation_id} = @{F_conversation_id}, " +
                $"{F_sender_id} = @{F_sender_id}, {F_send_time} = @{F_send_time} WHERE {F_id} = @{F_id};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl, MessageDto dto)
        {
            parColl.AddWithValue($"@{F_conversation_id}", dto.ConversationId);
            parColl.AddWithValue($"@{F_sender_id}", dto.SenderId);
            parColl.AddWithValue($"@{F_send_time}", dto.SendTime);
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} WHERE {F_id} = @{F_id};";
        }

        public IEnumerable<MessageDto> GetByConversationId(ulong conversationId)
        {
            var query = $"SELECT * FROM {TABLE} WHERE {F_conversation_id} = {conversationId};";
            return ExecuteReader(query);
        }
    }
}
