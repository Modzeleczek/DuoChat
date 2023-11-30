using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class ClientIPBlockDto : IDto<uint>
    {
        #region Properties
        public uint IpAddress { get; set; } = 0;
        #endregion

        public uint GetRepositoryKey()
        {
            return IpAddress;
        }
    }
}
