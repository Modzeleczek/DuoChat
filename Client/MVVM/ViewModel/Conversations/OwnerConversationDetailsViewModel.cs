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
    public class OwnerConversationDetailsViewModel : AdminConversationDetailsViewModel
    {
        #region Commands
        public RelayCommand DeleteConversation { get; }
        public RelayCommand ApplyName { get; }
        public RelayCommand ToggleAdmin { get; }
        #endregion

        private OwnerConversationDetailsViewModel(ClientMonolith client,
            Dictionary<ulong, User> knownUsers, Account activeAccount, Conversation conversation)
            : base(client, knownUsers, activeAccount, conversation)
        {
            // Właściciel nie ma przycisku do opuszczania konwersacji, bo nie może jej opuścić.
            LeaveConversation = null;

            DeleteConversation = new RelayCommand(_ =>
            {
                client.Request(new DeleteConversationUIRequest(conversation.Id));
            });

            ApplyName = new RelayCommand(_ =>
            {
                client.Request(new EditConversationUIRequest(conversation.Id, conversation.Name));
            });

            ToggleAdmin = new RelayCommand(obj =>
            {
                var convParObs = (ConversationParticipation)obj!;
                client.Request(new EditParticipationUIRequest(conversation.Id,
                    convParObs.ParticipantId, (byte)(convParObs.IsAdministrator ? 0 : 1)));
            });
        }

        public new static Result ShowDialog(Window owner, ClientMonolith client,
            Dictionary<ulong, User> knownUsers, Account activeAccount, Conversation conversation)
        {
            var vm = new OwnerConversationDetailsViewModel(client, knownUsers,
                activeAccount, conversation);
            var win = new OwnerConversationDetailsWindow(owner, vm);
            vm.RequestClose += win.Close;
            win.ShowDialog();
            return vm.Result;
        }
    }
}
