using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class EncryptedMessageCopyDto : IDto<(ulong messageId, ulong recipientId)>
    {
        #region Properties
        public ulong MessageId { get; set; } = 0;

        public ulong RecipientId { get; set; } = 0;

        public byte[] Content { get; set; } = null!;

        public long? ReceiveTime { get; set; } = 0;
        #endregion

        public (ulong messageId, ulong recipientId) GetRepositoryKey()
        {
            return (MessageId, RecipientId);
        }
    }
}
