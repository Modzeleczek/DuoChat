using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class ConversationParticipationRepository : Repository<ConversationParticipationDto,
        (ulong conversationId, ulong participantId)>
    {
        #region Fields
        private const string TABLE = "ConversationParticipation";
        private const string F_conversation_id = "conversation_id";
        private const string F_participant_id = "participant_id";
        private const string F_join_time = "join_time";
        private const string F_is_administrator = "is_administrator";
        #endregion

        public ConversationParticipationRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}(" +
                $"{F_conversation_id}, {F_participant_id}, {F_join_time}, {F_is_administrator}) " +
                "VALUES(" +
                $"@{F_conversation_id}, @{F_participant_id}, @{F_join_time}, @{F_is_administrator})";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl,
            ConversationParticipationDto dto)
        {
            parColl.AddWithValue($"@{F_conversation_id}", dto.ConversationId);
            parColl.AddWithValue($"@{F_participant_id}", dto.ParticipantId);
            parColl.AddWithValue($"@{F_join_time}", dto.JoinTime);
            parColl.AddWithValue($"@{F_is_administrator}", dto.IsAdministrator);
        }

        protected override (ulong conversationId, ulong participantId) GetInsertedKey(
            SQLiteConnection con, ConversationParticipationDto dto)
        {
            _ = con;
            return dto.GetRepositoryKey();
        }

        protected override string EntityName()
        {
            return "|conversation participation|";
        }

        protected override string KeyName()
        {
            return "|conversation id and participant id;N|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_conversation_id}, {F_participant_id}, {F_join_time}, " +
                $"{F_is_administrator} FROM {TABLE};";
        }

        protected override ConversationParticipationDto ReadOneEntity(SQLiteDataReader reader)
        {
            return new ConversationParticipationDto
            {
                ConversationId = (ulong)(long)reader[F_conversation_id],
                ParticipantId = (ulong)(long)reader[F_participant_id],
                JoinTime = (long)reader[F_join_time],
                IsAdministrator = (byte)(long)reader[F_is_administrator]
            };
        }

        protected override string ExistsQuery()
        {
            return $"SELECT COUNT(DISTINCT {F_conversation_id}, {F_participant_id}) FROM {TABLE} " +
                $"WHERE {F_conversation_id} = @{F_conversation_id} AND " +
                $"{F_participant_id} = @{F_participant_id};";
        }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl,
            (ulong conversationId, ulong participantId) key)
        {
            parColl.AddWithValue($"@{F_conversation_id}", key.conversationId);
            parColl.AddWithValue($"@{F_participant_id}", key.participantId);
        }

        protected override string GetQuery()
        {
            return $"SELECT {F_conversation_id}, {F_participant_id}, {F_join_time}, " +
                $"{F_is_administrator} FROM {TABLE} " +
                $"WHERE {F_conversation_id} = @{F_conversation_id} AND " +
                $"{F_participant_id} = @{F_participant_id};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_join_time} = @{F_join_time}, " +
                $"{F_is_administrator} = @{F_is_administrator} " +
                $"WHERE {F_conversation_id} = @{F_conversation_id} AND " +
                $"{F_participant_id} = @{F_participant_id};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl,
            ConversationParticipationDto dto)
        {
            parColl.AddWithValue($"@{F_join_time}", dto.JoinTime);
            parColl.AddWithValue($"@{F_is_administrator}", dto.IsAdministrator);
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} WHERE {F_conversation_id} = @{F_conversation_id} AND " +
                $"{F_participant_id} = @{F_participant_id};";
        }

        public IEnumerable<ConversationParticipationDto> GetByParticipantId(ulong participantId)
        {
            var query = $"SELECT * FROM {TABLE} WHERE {F_participant_id} = {participantId};";
            return ExecuteReader(query);
        }

        public IEnumerable<ConversationParticipationDto> GetByConversationIds(
            IEnumerable<ulong> conversationIds)
        {
            if (!conversationIds.Any())
                return Enumerable.Empty<ConversationParticipationDto>();

            var query = $"SELECT * FROM {TABLE} WHERE ${F_conversation_id} IN " +
                $"({string.Join(',', conversationIds)});";
            return ExecuteReader(query);
        }
    }
}
