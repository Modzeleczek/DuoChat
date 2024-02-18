using Client.MVVM.Model.Networking;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Client.MVVM.ViewModel.Observables.Messages;
using Shared.MVVM.ViewModel.Results;
using System.Windows;

namespace Client.MVVM.ViewModel.Conversations
{
    public class MessageRecipientsViewModel : ConversationCancellableViewModel
    {
        #region Properties
        public Message Message { get; }
        #endregion

        private MessageRecipientsViewModel(ClientMonolith client, Conversation conversation, Message message)
            : base(client, conversation)
        {
            Message = message;
        }

        public static Result ShowDialog(Window owner, ClientMonolith client, Conversation conversation,
            Message message)
        {
            var vm = new MessageRecipientsViewModel(client, conversation, message);
            var win = new MessageRecipientsWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
