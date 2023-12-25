using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows.Conversations;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking.Packets.ServerToClient;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class AddParticipantViewModel : WindowViewModel
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

                _client.Request(new SearchUsersUIRequest(LoginSearchText));
            }
        }

        public ObservableCollection<User> FoundUsers { get; } =
            new ObservableCollection<User>();
        #endregion

        #region Fields
        private readonly ClientMonolith _client;
        private readonly Account _activeAccount;
        #endregion

        private AddParticipantViewModel(ClientMonolith client, Account activeAccount)
        {
            _client = client;
            _activeAccount = activeAccount;

            SelectUser = new RelayCommand(obj =>
                OnRequestClose(new Success((User)obj!)));

            _client.ReceivedUsersList += OnReceivedUsersLists;
        }

        private void OnReceivedUsersLists(RemoteServer server, FoundUsersList.User[] users)
        {
            // Wątek Client.Process
            var userObservables = users.Where(u => u.Id != _activeAccount.RemoteId)
                .Select(u => new User { Id = u.Id, Login = u.Login });

            UIInvoke(() =>
            {
                FoundUsers.Clear();
                foreach (var user in userObservables)
                    FoundUsers.Add(user);
            });
        }

        public static Result ShowDialog(Window owner, ClientMonolith client, Account selectedAccount)
        {
            var vm = new AddParticipantViewModel(client, selectedAccount);
            var win = new AddParticipantWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
