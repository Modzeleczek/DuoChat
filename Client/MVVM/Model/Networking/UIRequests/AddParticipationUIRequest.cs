using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class AddParticipationUIRequest : UIRequest
    {
        #region Properties
        public ulong ConversationId { get; }
        public ulong ParticipantId { get; }
        #endregion

        public AddParticipationUIRequest(ulong conversationId, ulong participantId)
        {
            ConversationId = conversationId;
            ParticipantId = participantId;
        }
    }
}
