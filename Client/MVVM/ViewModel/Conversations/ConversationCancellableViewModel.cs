using Client.MVVM.Model.Networking;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Model.Networking.Packets.ServerToClient.Participation;
using Shared.MVVM.ViewModel;

namespace Client.MVVM.ViewModel.Conversations
{
    public abstract class ConversationCancellableViewModel : WindowViewModel
    {
        #region Fields
        protected readonly Conversation _conversation;
        #endregion

        protected ConversationCancellableViewModel(ClientMonolith client, Conversation conversation)
        {
            _conversation = conversation;

            client.ServerEndedConnection += OnServerEndedConnection;
            client.ReceivedDeletedConversation += OnReceivedDeletedConversation;
            client.ReceivedDeletedParticipation += OnReceivedDeletedParticipation;
        }
    
        private void OnServerEndedConnection(RemoteServer server, string errorMsg)
        {
            // Wątek Client.Process
            UIInvoke(Cancel);
        }

        private void OnReceivedDeletedConversation(RemoteServer server, ulong inConversationId)
        {
            // Wątek Client.Process
            /* Ignorujemy powiadomienia o konwersacjach innych niż Conversation,
            do którego należy aktualny (aktywny) użytkownik. */
            if (inConversationId == _conversation.Id)
                UIInvoke(Cancel);
        }

        private void OnReceivedDeletedParticipation(RemoteServer server,
            DeletedParticipation.Participation inParticipation)
        {
            // Wątek Client.Process
            if (inParticipation.ConversationId != _conversation.Id)
                return;

            if (inParticipation.ParticipantId == _conversation.Parent.RemoteId)
                UIInvoke(Cancel);
            else
                OnDeletedNonActiveAccountParticipation();
        }

        protected virtual void OnDeletedNonActiveAccountParticipation() { }
    }
}
