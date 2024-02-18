using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows;

namespace Client.MVVM.ViewModel.Conversations
{
    public class AdminConversationDetailsViewModel : RegularConversationDetailsViewModel
    {
        #region Commands
        public RelayCommand AddParticipant { get; }
        public RelayCommand RemoveParticipant { get; }
        #endregion

        protected AdminConversationDetailsViewModel(ClientMonolith client, Conversation conversation,
            Dictionary<ulong, User> knownUsers)
            : base(client, conversation, knownUsers)
        {
            AddParticipant = new RelayCommand(_ =>
            {
                var result = FindUserViewModel.ShowDialog(window!, client, conversation);
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

        public new static Result ShowDialog(Window owner, ClientMonolith client, Conversation conversation,
            Dictionary<ulong, User> knownUsers)
        {
            var vm = new AdminConversationDetailsViewModel(client, conversation, knownUsers);
            var win = new AdminConversationDetailsWindow(owner, vm);
            vm.RequestClose += win.Close;
            win.ShowDialog();
            return vm.Result;
        }
    }
}
