using Shared.MVVM.Model.SQLiteStorage.DTO;

namespace Server.MVVM.Model.Persistence.DTO
{
    public class ConversationParticipationDto : IDto<(ulong, ulong)>
    {
        #region Properties
        public ulong ConversationId { get; set; } = 0;

        public ulong ParticipantId { get; set; } = 0;

        /* Liczba milisekund od 01.01.1970. Jeżeli jest
        ujemne, to liczba milisekund przed 01.01.1970. */
        public long JoinTime { get; set; } = 0;

        public byte IsAdministrator { get; set; } = 0;
        #endregion

        public (ulong, ulong) GetRepositoryKey()
        {
            return (ConversationId, ParticipantId);
        }
    }
}
