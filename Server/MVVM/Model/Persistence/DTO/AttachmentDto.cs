using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class AttachmentDto : IDto<ulong>
    {
        #region Properties
        public ulong Id { get; set; } = 0;

        public ulong MessageId { get; set; } = 0;

        public string Name { get; set; } = null!;
        #endregion

        public ulong GetRepositoryKey()
        {
            return Id;
        }
    }
}
