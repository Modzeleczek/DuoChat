using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System.Collections.ObjectModel;

namespace Server.MVVM.ViewModel
{
    public class ConnectedClientsViewModel : ViewModel
    {
        #region Commands
        public RelayCommand DisconnectClient { get; }
        #endregion

        #region Properties
        private ObservableCollection<Observables.Client> _clients =
            new ObservableCollection<Observables.Client>();
        public ObservableCollection<Observables.Client> Clients
        {
            get => _clients;
            set { _clients = value; OnPropertyChanged(); }
        }

        private Observables.Client _selectedUser;
        public Observables.Client SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }
        #endregion

        public ConnectedClientsViewModel(DialogWindow owner, Model.Server server)
            : base(owner)
        {
            DisconnectClient = new RelayCommand(obj =>
            {
                var client = (Observables.Client)obj;
                client.Model.DisconnectAsync().Wait();
                Clients.Remove(client);
            });

            server.ClientConnected += (modelClient) =>
            {
                var model = (Model.Client)((Success)modelClient).Data;
                var observableClient = new Observables.Anonymous(model);

                model.LostConnection += (lostConRes) =>
                {
                    UIInvoke(() =>
                    {
                        Clients.Remove(observableClient);
                        Alert("|Client disconnected.|");
                    });
                };

                UIInvoke(() => Clients.Add(observableClient));
            };
        }
    }
}
