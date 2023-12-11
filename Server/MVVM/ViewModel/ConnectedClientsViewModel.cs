using Server.MVVM.Model;
using Server.MVVM.Model.Networking;
using Server.MVVM.Model.Networking.UIRequests;
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
                var clientKey = client.GetPrimaryKey();
                client.DisableInteraction();

                window!.SetEnabled(false);
                _server.Request(new DisconnectClient(clientKey, () => UIInvoke(() =>
                    // Klient zostanie usunięty z Clients w OnClientEndedConnection.
                    window.SetEnabled(true))));
            });

            BlockIP = new RelayCommand(obj =>
            {
                // Wątek UI
                var client = (ClientObservable)obj!;
                var clientKey = client.GetPrimaryKey();
                client.DisableInteraction();

                window!.SetEnabled(false);
                _server.Request(new BlockClientIP(clientKey.IpAddress, (errorMsg) => UIInvoke(() =>
                {
                    if (!(errorMsg is null))
                    {
                        Alert(errorMsg);
                        return;
                    }

                    // Klient zostanie usunięty z Clients w OnClientEndedConnection.
                    window.SetEnabled(true);
                })));
            });

            server.ClientConnected += OnClientConnected;
            server.ClientHandshaken += OnClientHandshaken;
            server.ClientEndedConnection += OnClientEndedConnection;
        }

        private void OnClientConnected(Client client)
        {
            // Wątek Server.Process
            var clientObs = new ClientObservable(client.GetPrimaryKey());

            UIInvoke(() =>
            {
                Clients.Add(clientObs);
                _logger.Log($"{clientObs.DisplayedName} |connected|.");
            });
        }

        private void OnClientHandshaken(Client client)
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

        private void OnClientEndedConnection(Client client, string statusMsg)
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
