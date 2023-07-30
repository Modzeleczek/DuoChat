using Server.MVVM.ViewModel.Observables;
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

        private Observables.Client _selectedClient;
        public Observables.Client SelectedClient
        {
            get => _selectedClient;
            set { _selectedClient = value; OnPropertyChanged(); }
        }
        #endregion

        #region Fields
        private readonly object _uiLock = new object();
        #endregion

        public ConnectedClientsViewModel(DialogWindow owner, Model.Server server, Log log)
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
                var anonymous = new Observables.Anonymous(model);

                model.LostConnection += (lostConRes) =>
                {
                    string message;
                    if (lostConRes is Success)
                        message = "|Client disconnected.|";
                    else if (lostConRes is Failure failure)
                        message = failure.Reason.Prepend("|Client crashed.|").Message;
                    else // result is Cancellation
                        message = "|Disconnected client.|";
                    /* Synchronizujemy wszystkie operacje wykonywane na
                    kolekcji ObservableCollection. */
                    lock (_uiLock) UIInvoke(() =>
                    {
                        Clients.Remove(anonymous);
                        /* Log jest thread safe, bo w Append używamy lock,
                        ale bierzemy go w "lock (_clientsLock)", żeby mieć
                        pewność, że wiadomość w logu o aktualnym dostępie do
                        ObservableCollection (Remove) nie zostanie wyprzedzona
                        przez wiadomość z jakiegokolwiek innego dostępu. */
                        log.Append(message);
                    });
                };

                lock (_uiLock) UIInvoke(() =>
                {
                    Clients.Add(anonymous);
                    log.Append("|Client connected.|");
                });
            };

            server.Stopped += (result) =>
            {
                /* Wykonywane w wątku UI, więc Clients.Clear bez UIInvoke.
                Jednak handler ChangedState wykonywany w log.Append i tak
                wykonuje UIInvoke */
                lock (_uiLock)
                {
                    Clients.Clear();
                    log.Append("|Server stopped.|");
                }
            };
        }
    }
}
