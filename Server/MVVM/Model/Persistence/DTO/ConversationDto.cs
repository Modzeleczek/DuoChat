using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class ConversationDto : IDto<ulong>
    {
        #region Properties
        public ulong Id { get; set; } = 0;

        public ulong OwnerId { get; set; } = 0;

        public string Name { get; set; } = null!;
        #endregion

        public ConversationDto() { }

        public ulong GetRepositoryKey()
        {
            return Id;
        }
    }
}
