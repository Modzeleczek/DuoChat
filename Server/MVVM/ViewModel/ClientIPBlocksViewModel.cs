using Server.MVVM.Model.Networking;
using Server.MVVM.Model.Networking.UIRequests;
using Server.MVVM.Model.Persistence.DTO;
using Server.MVVM.ViewModel.ClientIPBlockActions;
using Server.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Server.MVVM.ViewModel
{
    public class ClientIPBlocksViewModel : UserControlViewModel
    {
        #region Commands
        public RelayCommand Add { get; }
        public RelayCommand Delete { get; }
        #endregion

        #region Properties
        private ObservableCollection<ClientIPBlockObservable> _clientIPBlocks =
            new ObservableCollection<ClientIPBlockObservable>();
        public ObservableCollection<ClientIPBlockObservable> ClientIPBlocks
        {
            get => _clientIPBlocks;
            set { _clientIPBlocks = value; OnPropertyChanged(); }
        }
        #endregion

        public ClientIPBlocksViewModel(DialogWindow owner, IEnumerable<ClientIPBlockDto> clientIPBlocks,
            ServerMonolith server)
            : base(owner)
        {
            Add = new RelayCommand(_ =>
            {
                // Wątek UI
                var vm = new CreateClientIPBlockViewModel()
                {
                    Title = "|Add client IP block|",
                    ConfirmButtonText = "|Add|"
                };
                new FormWindow(window!, vm).ShowDialog();
                var result = vm.Result;
                // Anulowanie
                if (!(result is Success success))
                    return;
                IPv4Address ipAddress = (IPv4Address)success.Data!;

                server.Request(new BlockClientIP(ipAddress, (errorMsg) => UIInvoke(() =>
                {
                    // Wątek UI na zlecenie wątku Server.Process
                    if (!(errorMsg is null))
                        Alert(errorMsg);
                })));
            });

            Delete = new RelayCommand(obj =>
            {
                // Wątek UI
                var clientIPBlockObs = (ClientIPBlockObservable)obj!;

                server.Request(new UnblockClientIP(clientIPBlockObs.IpAddress));
            });

            // Inicjujemy listę zablokowanych adresów IP.
            foreach (var b in clientIPBlocks)
                ClientIPBlocks.Add(new ClientIPBlockObservable
                {
                    IpAddress = new IPv4Address(b.IpAddress)
                });

            server.IPBlocked += OnIPBlocked;
            server.IPUnblocked += OnIPUnblocked;
        }

        private void OnIPBlocked(IPv4Address ipAddress)
        {
            // Wątek Server.Process
            UIInvoke(() => ClientIPBlocks.Add(
                new ClientIPBlockObservable { IpAddress = ipAddress }));
        }

        private void OnIPUnblocked(IPv4Address ipAddress)
        {
            // Wątek Server.Process
            var ipBlock = ClientIPBlocks.Single(b => b.IpAddress.Equals(ipAddress));

            UIInvoke(() => ClientIPBlocks.Remove(ipBlock));
        }
    }
}
