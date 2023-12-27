using Client.MVVM.Model.Networking;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables.Messages;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class MessageRecipientsViewModel : WindowViewModel
    {
        #region Properties
        public Message Message { get; }
        #endregion

        #region Fields
        private readonly ClientMonolith _client;
        #endregion

        private MessageRecipientsViewModel(ClientMonolith client, Message message)
        {
            _client = client;
            Message = message;
        }

        public static Result ShowDialog(Window owner, ClientMonolith client, Message message)
        {
            var vm = new MessageRecipientsViewModel(client, message);
            var win = new MessageRecipientsWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
