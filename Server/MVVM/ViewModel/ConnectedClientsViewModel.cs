using Server.MVVM.Model;
using Server.MVVM.Model.Networking;
using Server.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace Server.MVVM.ViewModel
{
    public class ConnectedClientsViewModel : UserControlViewModel
    {
        #region Commands
        public RelayCommand DisconnectClient { get; }
        public RelayCommand BlockIP { get; }
        #endregion

        #region Properties
        private ObservableCollection<ClientObservable> _clients =
            new ObservableCollection<ClientObservable>();
        public ObservableCollection<ClientObservable> Clients
        {
            get => _clients;
            set { _clients = value; OnPropertyChanged(); }
        }
        #endregion

        #region Fields
        private readonly ServerMonolith _server;
        private readonly ILogger _logger;
        #endregion

        public ConnectedClientsViewModel(DialogWindow owner, ServerMonolith server, ILogger logger)
            : base(owner)
        {
            _server = server;
            _logger = logger;

            DisconnectClient = new RelayCommand(obj =>
            {
                // Wykonywane przez wątek UI
                var client = (ClientObservable)obj!;
                var key = client.GetPrimaryKey();
                client.DisableInteraction();

                RequestClientRemove(client, UIRequest.Operations.DisconnectClient, key);
            });

            BlockIP = new RelayCommand(obj =>
            {
                // Wątek UI
                var client = (ClientObservable)obj!;
                var key = client.GetPrimaryKey();
                client.DisableInteraction();

                RequestClientRemove(client, UIRequest.Operations.BlockClientIP, key.IpAddress);
            });

            server.ClientConnected += ClientConnected;
            server.ClientHandshaken += ClientHandshaken;
            server.ClientEndedConnection += ClientEndedConnection;
        }

        private void RequestClientRemove(ClientObservable clientObs,
            UIRequest.Operations operation, object parameter)
        {
            // Wątek UI
            window!.SetEnabled(false);
            _server.Request(new UIRequest(operation, parameter,
                () => UIInvoke(() => window.SetEnabled(true))));
            Clients.Remove(clientObs);
        }

        private void ClientConnected(Client client)
        {
            // Wątek Server.Process
            var clientObs = new ClientObservable(client.GetPrimaryKey());

            UIInvoke(() =>
            {
                Clients.Add(clientObs);
                _logger.Log($"{clientObs.DisplayedName} |connected|.");
            });
        }

        private void ClientHandshaken(Client client)
        {
            // Wątek Server.Process
            var clientObs = FindClientObservable(client);

            UIInvoke(() =>
            {
                clientObs.Authenticate(client.Login!);
                _logger.Log($"{clientObs.DisplayedName} |was authenticated|.");
            });
        }

        private ClientObservable FindClientObservable(Client client)
        {
            var clientKey = client.GetPrimaryKey();
            var clientObs = Clients.Single(c => clientKey.Equals(c.GetPrimaryKey()));
            // if (clientObs is null) nieprawdopodobne
            return clientObs;
        }

        private void ClientEndedConnection(Client client, string statusMsg)
        {
            // Wątek Server.Process
            var clientObs = FindClientObservable(client);

            UIInvoke(() =>
            {
                Clients.Remove(clientObs);
                _logger.Log($"{clientObs.DisplayedName} {statusMsg}");
            });
        }
    }
}
