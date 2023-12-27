using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class EncryptedMessageCopyRepository : Repository<EncryptedMessageCopyDto,
        (ulong messageId, ulong recipientId)>
    {
        #region Fields
        private const string TABLE = "EncryptedMessageCopy";
        private const string F_message_id = "message_id";
        private const string F_recipient_id = "recipient_id";
        private const string F_content = "content";
        private const string F_receive_time = "receive_time";
        #endregion

        public EncryptedMessageCopyRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}(" +
                $"{F_message_id}, {F_recipient_id}, {F_content}, {F_receive_time}) " +
                "VALUES(" +
                $"@{F_message_id}, @{F_recipient_id}, @{F_content}, @{F_receive_time})";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl,
            EncryptedMessageCopyDto dto)
        {
            parColl.AddWithValue($"@{F_message_id}", dto.MessageId);
            parColl.AddWithValue($"@{F_recipient_id}", dto.RecipientId);
            parColl.AddWithValue($"@{F_content}", dto.Content);
            parColl.AddWithValue($"@{F_receive_time}", dto.ReceiveTime);
        }

        protected override (ulong messageId, ulong recipientId) GetInsertedKey(
            SQLiteConnection con, EncryptedMessageCopyDto dto)
        {
            _ = con;
            return dto.GetRepositoryKey();
        }

        protected override string EntityName()
        {
            return "|encrypted message copy|";
        }

        protected override string KeyName()
        {
            return "|message id and recipient id;M|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_message_id}, {F_recipient_id}, {F_content}, {F_receive_time} FROM {TABLE};";
        }

        protected override EncryptedMessageCopyDto ReadOneEntity(SQLiteDataReader reader)
        {
            object receiveTime = reader[F_receive_time];
            // alternatywa: receiveTime is DBNull
            long? receiveTimeLong = receiveTime == DBNull.Value ? null : (long)receiveTime;

            return new EncryptedMessageCopyDto
            {
                MessageId = (ulong)(long)reader[F_message_id],
                RecipientId = (ulong)(long)reader[F_recipient_id],
                Content = (byte[])reader[F_content],
                ReceiveTime = receiveTimeLong
            };
        }

        protected override string ExistsQuery()
        {
            return $"SELECT COUNT(1) FROM {TABLE} " +
                $"WHERE {F_message_id} = @{F_message_id} AND {F_recipient_id} = @{F_recipient_id};";
        }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl,
            (ulong messageId, ulong recipientId) key)
        {
            parColl.AddWithValue($"@{F_message_id}", key.messageId);
            parColl.AddWithValue($"@{F_recipient_id}", key.recipientId);
        }

        protected override string GetQuery()
        {
            return $"SELECT {F_message_id}, {F_recipient_id}, {F_content}, {F_receive_time} FROM {TABLE} " +
                $"WHERE {F_message_id} = @{F_message_id} AND {F_recipient_id} = @{F_recipient_id};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_content} = @{F_content}, {F_receive_time} = @{F_receive_time} " +
                $"WHERE {F_message_id} = @{F_message_id} AND {F_recipient_id} = @{F_recipient_id};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl,
            EncryptedMessageCopyDto dto)
        {
            byte[] content = dto.Content;
            parColl.Add($"@{F_content}", DbType.Binary, content.Length).Value = content;
            parColl.AddWithValue($"@{F_receive_time}", dto.ReceiveTime);
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} " +
                $"WHERE {F_message_id} = @{F_message_id} AND {F_recipient_id} = @{F_recipient_id};";
        }

        public IEnumerable<EncryptedMessageCopyDto> GetUnreceived(ulong recipientId,
            IEnumerable<ulong> messageIds)
        {
            var query = $"SELECT * FROM {TABLE} WHERE {F_recipient_id} = {recipientId} " +
                $"AND {F_message_id} IN ({string.Join(',', messageIds)});";
            return ExecuteReader(query);
        }
    }
}
