using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class DeleteConversationUIRequest : UIRequest
    {
        #region Properties
        public ulong ConversationId { get; }
        #endregion

        public DeleteConversationUIRequest(ulong conversationId)
        {
            ConversationId = conversationId;
        }
    }
}
