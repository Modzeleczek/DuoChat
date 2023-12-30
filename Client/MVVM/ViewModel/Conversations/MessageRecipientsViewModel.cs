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

        private MessageRecipientsViewModel(Message message)
        {
            Message = message;
        }

        public static Result ShowDialog(Window owner, Message message)
        {
            var vm = new MessageRecipientsViewModel(message);
            var win = new MessageRecipientsWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
