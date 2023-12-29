using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class MessageDto : IDto<ulong>
    {
        #region Properties
        public ulong Id { get; set; } = 0;

        public ulong ConversationId { get; set; } = 0;

        public ulong? SenderId { get; set; } = 0;

        public long SendTime { get; set; } = 0;
        #endregion

        public ulong GetRepositoryKey()
        {
            return Id;
        }
    }
}
