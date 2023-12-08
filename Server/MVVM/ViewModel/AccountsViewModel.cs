using Server.MVVM.Model;
using Server.MVVM.Model.Networking;
using Server.MVVM.Model.Networking.UIRequests;
using Server.MVVM.Model.Persistence.DTO;
using Server.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Server.MVVM.ViewModel
{
    public class AccountsViewModel : UserControlViewModel
    {
        #region Commands
        public RelayCommand ToggleBlock { get; }
        public RelayCommand Disconnect { get; }
        #endregion

        #region Properties
        private ObservableCollection<AccountObservable> _accounts =
            new ObservableCollection<AccountObservable>();
        public ObservableCollection<AccountObservable> Accounts
        {
            get => _accounts;
            set { _accounts = value; OnPropertyChanged(); }
        }
        #endregion

        public AccountsViewModel(DialogWindow owner, List<AccountDto> accounts, ServerMonolith server)
            : base(owner)
        {
            ToggleBlock = new RelayCommand(obj =>
            {
                // Wątek UI
                var accountObs = (AccountObservable)obj!;

                window!.SetEnabled(false);
                server.Request(accountObs.IsBlocked ?
                    new UnblockAccount(accountObs.Login, () => UIInvoke(() =>
                    {
                        accountObs.IsBlocked = false;
                        window!.SetEnabled(true);
                    })) :
                    new BlockAccount(accountObs.Login, () => UIInvoke(() =>
                    {
                        accountObs.IsBlocked = true;
                        window!.SetEnabled(true);
                    })));
            });

            Disconnect = new RelayCommand(obj =>
            {
                // Wątek UI
                var accountObs = (AccountObservable)obj!;

                window!.SetEnabled(false);
                server.Request(new DisconnectAccount(accountObs.Login, () => UIInvoke(() =>
                {
                    accountObs.IsConnected = false;
                    window.SetEnabled(true);
                })));
            });

            server.ClientHandshaken += OnClientHandshaken;
            server.ClientEndedConnection += OnClientEndedConnection;

            // Inicjujemy listę kont.
            foreach (var a in accounts)
                Accounts.Add(new AccountObservable
                {
                    Login = a.Login,
                    IsBlocked = a.IsBlocked == 1,
                    IsConnected = false
                });
        }

        private void OnClientHandshaken(Client client)
        {
            // Wątek Server.Process
            var accountObs = Accounts.SingleOrDefault(a => a.Login.Equals(client.Login));
            if (accountObs is null)
            {
                // Serwer zarejestrował nowe konto.
                UIInvoke(() => Accounts.Add(new AccountObservable
                {
                    Login = client.Login!,
                    IsBlocked = false,
                    IsConnected = true
                }));
                return;
            }

            // Serwer autentykował istniejące konto.
            UIInvoke(() => accountObs.IsConnected = true);
        }

        private void OnClientEndedConnection(Client client, string statusMsg)
        {
            _ = statusMsg;

            // Wątek Server.Process
            var accountObs = Accounts.SingleOrDefault(a => a.Login.Equals(client.Login));
            if (accountObs is null)
                return;

            UIInvoke(() => accountObs.IsConnected = false);
        }

        /* private void AccountToggledBlock(string login)
        {
            // Wątek Client.ProcessProtocol; write lock
            var accountObs = Accounts.SingleOrDefault(x => x.Login.Equals(login));
            // if (accountObs is null) nieprawdopodobne

            UIInvoke(() => accountObs.IsBlocked = !accountObs.IsBlocked);
        } */
    }
}
