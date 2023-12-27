using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Client.MVVM.ViewModel.Conversations
{
    public class AdminConversationDetailsViewModel : RegularConversationDetailsViewModel
    {
        #region Commands
        public RelayCommand AddParticipant { get; }
        public RelayCommand RemoveParticipant { get; }
        #endregion

        protected AdminConversationDetailsViewModel(ClientMonolith client,
            Dictionary<ulong, User> knownUsers, Account activeAccount, Conversation conversation)
            : base(client, knownUsers, activeAccount, conversation)
        {
            AddParticipant = new RelayCommand(_ =>
            {
                var result = AddParticipantViewModel.ShowDialog(window!, client, activeAccount, conversation);
                if (!(result is Success success))
                    // Anulowanie
                    return;

                var user = (User)success.Data!;
                client.Request(new AddParticipationUIRequest(conversation.Id, user.Id));
            });

            RemoveParticipant = new RelayCommand(obj =>
            {
                var convParObs = (ConversationParticipation)obj!;
                client.Request(new DeleteParticipationUIRequest(
                    convParObs.ConversationId, convParObs.ParticipantId));
            });
        }

        public new static Result ShowDialog(Window owner, ClientMonolith client,
            Dictionary<ulong, User> knownUsers, Account activeAccount, Conversation conversation)
        {
            var vm = new AdminConversationDetailsViewModel(client, knownUsers,
                activeAccount, conversation);
            var win = new AdminConversationDetailsWindow(owner, vm);
            vm.RequestClose += win.Close;
            win.ShowDialog();
            return vm.Result;
        }
    }
}
