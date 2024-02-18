using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking.Packets.ServerToClient.Participation;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Client.MVVM.ViewModel.Conversations
{
    public class RegularConversationDetailsViewModel : ConversationCancellableViewModel
    {
        #region Commands
        public RelayCommand? LeaveConversation { get; protected set; }
        #endregion

        #region Properties
        public Conversation Conversation { get => _conversation; }

        public ObservableCollection<ConversationParticipation> FilteredParticipations { get; } =
            new ObservableCollection<ConversationParticipation>();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();

                FilteredParticipations.Clear();
                var filteredParticipations = Conversation.Participations.Where(
                    p => p.Participant.Login.Contains(value));
                foreach (var fp in filteredParticipations)
                    FilteredParticipations.Add(fp);
            }
        }
        #endregion

        #region Fields
        protected readonly ClientMonolith _client;
        protected readonly Dictionary<ulong, User> _knownUsers;
        #endregion

        protected RegularConversationDetailsViewModel(ClientMonolith client, Conversation conversation,
            Dictionary<ulong, User> knownUsers)
            : base(client, conversation)
        {
            _client = client;
            _knownUsers = knownUsers;
            SearchText = string.Empty;

            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (DialogWindow)windowLoadedE!;
                window.Closable = true;
            });

            LeaveConversation = new RelayCommand(_ =>
            {
                _client.Request(new DeleteParticipationUIRequest(conversation.Id, conversation.Parent.RemoteId));
            });

            client.ReceivedRequestError += OnReceivedRequestError;
            client.ReceivedAddedParticipation += OnReceivedAddedParticipation;
            client.ReceivedEditedParticipation += OnReceivedEditedParticipation;
        }

        private void OnReceivedRequestError(RemoteServer server, string errorMsg)
        {
            // Wątek Client.Process
            UIInvoke(() => Alert(errorMsg));
        }

        private void OnReceivedAddedParticipation(RemoteServer server,
            AddedParticipation.Participation inParticipation)
        {
            // Wątek Client.Process
            if (inParticipation.ConversationId == Conversation.Id)
                UIInvoke(RefreshFilteredParticipations);
        }

        private void RefreshFilteredParticipations()
        {
            SearchText = _searchText;
        }

        private void OnReceivedEditedParticipation(RemoteServer server,
            EditedParticipation.Participation inParticipation)
        {
            if (inParticipation.ConversationId != Conversation.Id)
                return;

            UIInvoke(() =>
            {
                if (inParticipation.ParticipantId == Conversation.Parent.RemoteId)
                    /* Cancellation oznacza, że użytkownik zamknął okno lub zostało zamknięte w
                    którymś handlerze eventu. Success oznacza, że okno ma zostać otwarte ponownie,
                    bo aktywnemu użytkownikowi zmieniono uprawnienia. */
                    OnRequestClose(new Success());
                else
                    // Odświeżamy tylko jeżeli nie reloadujemy okna.
                    RefreshFilteredParticipations();
            });
        }

        protected override void OnDeletedNonActiveAccountParticipation()
        {
            UIInvoke(RefreshFilteredParticipations);
        }

        public static Result ShowDialog(Window owner, ClientMonolith client, Conversation conversation,
            Dictionary<ulong, User> knownUsers)
        {
            var vm = new RegularConversationDetailsViewModel(client, conversation, knownUsers);
            var win = new RegularConversationDetailsWindow(owner, vm);
            vm.RequestClose += win.Close;
            win.ShowDialog();
            return vm.Result;
        }
    }
}
