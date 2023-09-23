using Server.MVVM.Model;
using Server.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System;
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
        private Model.Server _server;
        #endregion

        public ConnectedClientsViewModel(DialogWindow owner, Model.Server server)
            : base(owner)
        {
            _server = server;

            DisconnectClient = new RelayCommand(obj =>
            {
                // Wykonywane przez wątek UI
                var client = (ClientObservable)obj;
                var key = client.GetPrimaryKey();
                server.DisconnectClientAsync(key);
                // Klient zostanie usunięty z listy w ClientEndedConnection.
            });

            BlockIP = new RelayCommand(obj =>
            {
                // Wątek UI
                var client = (ClientObservable)obj;
                var key = client.GetPrimaryKey();
                /* TODO: tabela w bazie danych i w Server.Process przy
                akceptowaniu klienta sprawdzanie, czy nie jest zbanowany. */
            });

            server.ClientConnected += ClientConnected;
            server.ClientAuthenticated += ClientAuthenticated;
            server.ClientEndedConnection += ClientEndedConnection;
        }

        private void ClientConnected(Model.Client client)
        {
            /* Synchronizujemy wszystkie operacje wykonywane na stanie serwera.
            Wątek Server.Process; write lock */
            var clientObservable = new ClientObservable(client.GetPrimaryKey());

            UIInvoke(() =>
            {
                Clients.Add(clientObservable);
                _server.Log($"{clientObservable.DisplayedName} |connected|.");
            });
        }

        private void ClientAuthenticated(Model.Client client)
        {
            // Wątek Client.ProcessHandle; write lock
            var primaryKey = client.GetPrimaryKey();
            var observableClient = Clients.FirstOrDefault(e => primaryKey.Equals(e.GetPrimaryKey()));
            if (observableClient is null)
            {
                // Nieprawdopodobne: rozłączamy, bo klienta nie ma na liście.
                client.DisconnectAsync();
                return;
            }

            UIInvoke(() =>
            {
                observableClient.Authenticate(client.Login);
                _server.Log($"{observableClient.DisplayedName} |was authenticated|.");
            });
        }

        private void ClientEndedConnection(Model.Client client, Result result)
        {
            // Wątek Client.Process; write lock
            var primaryKey = client.GetPrimaryKey();
            var clientObservable = Clients.SingleOrDefault(
                e => primaryKey.Equals(e.GetPrimaryKey()));
            if (clientObservable is null)
            {
                // Nieprawdopodobne
                client.DisconnectAsync();
                return;
            }

            string message;
            if (result is Success)
                message = "|disconnected|.";
            else if (result is Failure failure)
                message = failure.Reason.Prepend("|crashed|.").Message;
            else // result is Cancellation
                message = "|was disconnected|.";
            message = $"{clientObservable.DisplayedName} {message}";

            UIInvoke(() =>
            {
                Clients.Remove(clientObservable);
                /* Jesteśmy w write locku, dzięki czemu mamy pewność, że
                wiadomość w logu o aktualnym dostępie do ObservableCollection
                (Remove) nie zostanie wyprzedzona przez wiadomość z
                jakiegokolwiek innego dostępu. */
                _server.Log(message);
            });
        }
    }
}
