using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking.Packets.ServerToClient.Participation;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Client.MVVM.ViewModel.Conversations
{
    public class RegularConversationDetailsViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand? LeaveConversation { get; protected set; }
        #endregion

        #region Properties
        public Conversation Conversation { get; }

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
        private readonly Account _activeAccount;
        #endregion

        protected RegularConversationDetailsViewModel(ClientMonolith client,
            Dictionary<ulong, User> knownUsers, Account activeAccount, Conversation conversation)
        {
            _client = client;
            _knownUsers = knownUsers;
            _activeAccount = activeAccount;
            Conversation = conversation;
            SearchText = string.Empty;

            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (DialogWindow)windowLoadedE!;
                window.Closable = true;
            });

            LeaveConversation = new RelayCommand(_ =>
            {
                _client.Request(new DeleteParticipationUIRequest(conversation.Id, activeAccount.RemoteId));
            });

            client.ReceivedRequestError += OnReceivedRequestError;
            client.ServerEndedConnection += OnServerEndedConnection;
            client.ReceivedDeletedConversation += OnReceivedDeletedConversation;
            client.ReceivedAddedParticipation += OnReceivedAddedParticipation;
            // TODO: w EditedParticipation wyłączać i jeszcze raz włączać odpowiedni ViewModel, jeżeli odebrano/przyznano nam uprawnienia admina
            client.ReceivedEditedParticipation += OnReceivedEditedParticipation;
            client.ReceivedDeletedParticipation += OnReceivedDeletedParticipation;
        }

        private void OnReceivedRequestError(RemoteServer server, string errorMsg)
        {
            // Wątek Client.Process
            UIInvoke(() => Alert(errorMsg));
        }

        private void OnServerEndedConnection(RemoteServer server, string errorMsg)
        {
            // Wątek Client.Process
            UIInvoke(Cancel);
        }

        private void OnReceivedDeletedConversation(RemoteServer server, ulong inConversationId)
        {
            // Wątek Client.Process
            /* Ignorujemy powiadomienia o konwersacjach innych niż Conversation
            do których należy aktualny (aktywny) użytkownik. */
            if (inConversationId == Conversation.Id)
                UIInvoke(Cancel);
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
                if (inParticipation.ParticipantId == _activeAccount.RemoteId)
                    /* Cancellation oznacza, że użytkownik zamknął okno lub zostało zamknięte w
                    którymś handlerze eventu. Success oznacza, że okno ma zostać otwarte ponownie,
                    bo aktywnemu użytkownikowi zmieniono uprawnienia. */
                    OnRequestClose(new Success());
                else
                    // Odświeżamy tylko jeżeli nie reloadujemy okna.
                    RefreshFilteredParticipations();
            });
        }

        private void OnReceivedDeletedParticipation(RemoteServer server,
            DeletedParticipation.Participation inParticipation)
        {
            // Wątek Client.Process
            if (inParticipation.ConversationId != Conversation.Id)
                return;

            UIInvoke(() =>
            {
                RefreshFilteredParticipations();
                if (inParticipation.ParticipantId == _activeAccount.RemoteId)
                    Cancel();
            });
        }

        public static Result ShowDialog(Window owner, ClientMonolith client,
            Dictionary<ulong, User> knownUsers, Account activeAccount, Conversation conversation)
        {
            var vm = new RegularConversationDetailsViewModel(client, knownUsers, activeAccount, conversation);
            var win = new RegularConversationDetailsWindow(owner, vm);
            vm.RequestClose += win.Close;
            win.ShowDialog();
            return vm.Result;
        }
    }
}
