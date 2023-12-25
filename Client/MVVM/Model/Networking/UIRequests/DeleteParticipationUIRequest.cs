using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class DeleteParticipationUIRequest : UIRequest
    {
        #region Properties
        public ulong ConversationId { get; }
        public ulong ParticipantId { get; }
        #endregion

        public DeleteParticipationUIRequest(ulong conversationId, ulong participantId)
        {
            ConversationId = conversationId;
            ParticipantId = participantId;
        }
    }
}
