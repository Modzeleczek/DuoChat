using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class EncryptedAttachmentCopyDto : IDto<(ulong attachmentId, ulong recipientId)>
    {
        #region Properties
        public ulong AttachmentId { get; set; } = 0;

        public ulong RecipientId { get; set; } = 0;

        public byte[] Content { get; set; } = null!;
        #endregion

        public (ulong attachmentId, ulong recipientId) GetRepositoryKey()
        {
            return (AttachmentId, RecipientId);
        }
    }
}
