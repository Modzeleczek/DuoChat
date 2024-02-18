using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking.Packets.ServerToClient;
using Shared.MVVM.ViewModel.Results;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Client.MVVM.ViewModel.Conversations
{
    public class FindUserViewModel : ConversationCancellableViewModel
    {
        #region Commands
        public RelayCommand SelectUser { get; }
        #endregion

        #region Properties
        private string _loginSearchText = string.Empty;
        public string LoginSearchText
        {
            get { return _loginSearchText; }
            set
            {
                _loginSearchText = value;
                OnPropertyChanged();

                _client.Request(new FindUserUIRequest(LoginSearchText));
            }
        }

        public ObservableCollection<User> FoundUsers { get; } =
            new ObservableCollection<User>();
        #endregion

        #region Fields
        private readonly ClientMonolith _client;
        #endregion

        private FindUserViewModel(ClientMonolith client, Conversation conversation)
            : base(client, conversation)
        {
            _client = client;

            SelectUser = new RelayCommand(obj =>
                OnRequestClose(new Success((User)obj!)));

            client.ReceivedUsersList += OnReceivedUsersLists;
        }

        private void OnReceivedUsersLists(RemoteServer server, FoundUsersList.User[] users)
        {
            // Wątek Client.Process
            /* Jeżeli weszliśmy do tego widoku, to znaczy, że jesteśmy właścicielem albo
            administratorem konwersacji i nasze id (Account.RemoteId) zostanie odfiltrowane. */
            var participantIds = _conversation.Participations.Select(p => p.ParticipantId).ToHashSet();
            var userObservables = users.Where(u => u.Id != _conversation.Owner.Id
                && !participantIds.Contains(u.Id)).Select(u => new User { Id = u.Id, Login = u.Login });

            UIInvoke(() =>
            {
                FoundUsers.Clear();
                foreach (var user in userObservables)
                    FoundUsers.Add(user);
            });
        }

        public static Result ShowDialog(Window owner, ClientMonolith client, Conversation conversation)
        {
            var vm = new FindUserViewModel(client, conversation);
            var win = new FindUserWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
