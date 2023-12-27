using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Model.SQLiteStorage;
using Shared.MVVM.Model.SQLiteStorage.Repositories;
using System.Data;
using System.Data.SQLite;

namespace Server.MVVM.Model.Persistence.Repositories
{
    public class EncryptedAttachmentCopyRepository : Repository<EncryptedAttachmentCopyDto,
        (ulong attachmentId, ulong recipientId)>
    {
        #region Fields
        private const string TABLE = "EncryptedAttachmentCopy";
        private const string F_attachment_id = "attachment_id ";
        private const string F_recipient_id = "recipient_id";
        private const string F_content = "content";

        #endregion

        public EncryptedAttachmentCopyRepository(ISQLiteConnector sqliteConnector) :
            base(sqliteConnector)
        { }

        protected override string AddQuery()
        {
            return $"INSERT INTO {TABLE}(" +
                $"{F_attachment_id}, {F_recipient_id}, {F_content}) " +
                "VALUES(" +
                $"@{F_attachment_id}, @{F_recipient_id}, @{F_content})";
        }

        protected override void SetAddParameters(SQLiteParameterCollection parColl,
            EncryptedAttachmentCopyDto dto)
        {
            parColl.AddWithValue($"@{F_attachment_id}", dto.AttachmentId);
            parColl.AddWithValue($"@{F_recipient_id}", dto.RecipientId);
            parColl.AddWithValue($"@{F_content}", dto.Content);
        }

        protected override (ulong attachmentId, ulong recipientId) GetInsertedKey(
            SQLiteConnection con, EncryptedAttachmentCopyDto dto)
        {
            _ = con;
            return dto.GetRepositoryKey();
        }

        protected override string EntityName()
        {
            return "|encrypted attachment copy|";
        }

        protected override string KeyName()
        {
            return "|attachment id and recipient id;M|";
        }

        protected override string GetAllQuery()
        {
            return $"SELECT {F_attachment_id}, {F_recipient_id}, {F_content} FROM {TABLE};";
        }

        protected override EncryptedAttachmentCopyDto ReadOneEntity(SQLiteDataReader reader)
        {
            return new EncryptedAttachmentCopyDto
            {
                AttachmentId = (ulong)(long)reader[F_attachment_id],
                RecipientId = (ulong)(long)reader[F_recipient_id],
                Content = (byte[])reader[F_content]
            };
        }

        protected override string ExistsQuery()
        {
            return $"SELECT COUNT(1) FROM {TABLE} " +
                $"WHERE {F_attachment_id} = @{F_attachment_id} AND {F_recipient_id} = @{F_recipient_id};";
        }

        protected override void SetKeyParameter(SQLiteParameterCollection parColl,
            (ulong attachmentId, ulong recipientId) key)
        {
            parColl.AddWithValue($"@{F_attachment_id}", key.attachmentId);
            parColl.AddWithValue($"@{F_recipient_id}", key.recipientId);
        }

        protected override string GetQuery()
        {
            return $"SELECT {F_attachment_id}, {F_recipient_id}, {F_content} FROM {TABLE} " +
                $"WHERE {F_attachment_id} = @{F_attachment_id} AND {F_recipient_id} = @{F_recipient_id};";
        }

        protected override string UpdateQuery()
        {
            return $"UPDATE {TABLE} SET {F_content} = @{F_content} " +
                $"WHERE {F_attachment_id} = @{F_attachment_id} AND {F_recipient_id} = @{F_recipient_id};";
        }

        protected override void SetUpdateParameters(SQLiteParameterCollection parColl,
            EncryptedAttachmentCopyDto dto)
        {
            byte[] content = dto.Content;
            parColl.Add($"@{F_content}", DbType.Binary, content.Length).Value = content;
        }

        protected override string DeleteQuery()
        {
            return $"DELETE FROM {TABLE} " +
                $"WHERE {F_attachment_id} = @{F_attachment_id} AND {F_recipient_id} = @{F_recipient_id};";
        }
    }
}
