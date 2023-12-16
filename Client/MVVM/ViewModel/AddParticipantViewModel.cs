using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Collections.ObjectModel;
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
        #endregion

        private AddParticipantViewModel(ClientMonolith client)
        {
            _client = client;

            SelectUser = new RelayCommand(obj =>
                OnRequestClose(new Success((User)obj!)));

            _client.ReceivedUsersList += OnReceivedUsersList;
        }

        private void OnReceivedUsersList(RemoteServer server, User[] users)
        {
            // Wątek Client.Process
            UIInvoke(() =>
            {
                FoundUsers.Clear();
                foreach (var user in users)
                    FoundUsers.Add(user);
            });
        }

        public static Result ShowDialog(Window owner, ClientMonolith client)
        {
            var vm = new AddParticipantViewModel(client);
            var win = new AddParticipantWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
