using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class EditParticipationUIRequest : UIRequest
    {
        #region Properties
        public ulong ConversationId { get; }
        public ulong ParticipantId { get; }
        public byte IsAdministrator { get; }
        #endregion

        public EditParticipationUIRequest(ulong conversationId, ulong participantId, byte isAdministrator)
        {
            ConversationId = conversationId;
            ParticipantId = participantId;
            IsAdministrator = isAdministrator;
        }
    }
}
