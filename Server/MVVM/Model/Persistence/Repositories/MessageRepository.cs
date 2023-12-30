using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using EMC = Server.MVVM.Model.Persistence.Repositories.EncryptedMessageCopyRepository;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class MessageRepository : Repository<MessageDto, ulong>
    {
        #region Fields
        public const string TABLE = "Message";
        public const string F_id = "id";
        public const string F_conversation_id = "conversation_id";
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
            object senderId = reader[F_sender_id];
            ulong? senderIdUlong = senderId == DBNull.Value ? null : (ulong)(long)senderId;

            return new MessageDto
            {
                Id = (ulong)(long)reader[F_id],
                ConversationId = (ulong)(long)reader[F_conversation_id],
                SenderId = senderIdUlong,
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

        public IEnumerable<MessageDto> GetNewest(ulong requesterId, ulong conversationId, uint count)
        {
            // Zwraca co najwyżej count wiadomości w porządku od najnowszej do najstarszej.
            /* Sprawdzanie m.{F_sender_id} = {requesterId} jest redundantne, bo użytkownik
            wysyła wiadomość też sam do siebie, więc zostanie obsłużony przez podzapytanie. */
            var query = @$"SELECT m.* FROM {TABLE} m 
                WHERE (m.{F_sender_id} = {requesterId} OR EXISTS 
                    (SELECT emc.{EMC.F_recipient_id} FROM {EMC.TABLE} emc 
                    WHERE emc.{EMC.F_message_id} = m.{F_id} AND emc.{EMC.F_recipient_id} = {requesterId}))
                AND {F_conversation_id} = {conversationId} ORDER BY m.{F_id} DESC LIMIT {count};";

            return ExecuteReader(query);

            /* Alternatywa z łączeniem tabel
            var query = $"SELECT m.* FROM {TABLE} m " +
                $"INNER JOIN {EMC.TABLE} emc ON emc.{EMC.F_message_id} = m.{F_id} " +
                $"WHERE (m.{F_sender_id} = {requesterId} OR emc.{EMC.F_recipient_id} = {requesterId}) " +
                $"AND m.{F_conversation_id} = {conversationId} " +
                $"ORDER BY m.{F_id} DESC LIMIT {count};";
            
            return ExecuteReader(query).DistinctBy(m => m.Id); */
        }

        public IEnumerable<MessageDto> GetOlderThan(ulong requesterId, ulong conversationId, ulong messageId,
            uint count)
        {
            // Zwraca co najwyżej count wiadomości w porządku od najnowszej do najstarszej.
            var query = @$"SELECT m.* FROM {TABLE} m 
                WHERE (m.{F_sender_id} = {requesterId} OR EXISTS 
                    (SELECT emc.{EMC.F_recipient_id} FROM {EMC.TABLE} emc 
                    WHERE emc.{EMC.F_message_id} = m.{F_id} AND emc.{EMC.F_recipient_id} = {requesterId}))
                AND {F_conversation_id} = {conversationId} AND m.{F_id} < {messageId} 
                ORDER BY {F_id} DESC LIMIT {count}";

            return ExecuteReader(query);

            /* Alternatywa z łączeniem tabel
             var query = $"SELECT m.* FROM {TABLE} m " +
                $"INNER JOIN {EMC.TABLE} emc ON emc.{EMC.F_message_id} = m.{F_id} " +
                $"WHERE (m.{F_sender_id} = {requesterId} OR emc.{EMC.F_recipient_id} = {requesterId}) " +
                $"AND m.{F_conversation_id} = {conversationId} AND m.{F_id} < {messageId} " +
                $"ORDER BY {F_id} DESC LIMIT {count};";
            
             return ExecuteReader(query).DistinctBy(m => m.Id).OrderByDescending(m => m.Id); */
        }
    }
}
