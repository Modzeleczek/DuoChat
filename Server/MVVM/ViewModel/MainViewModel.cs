using Server.MVVM.Model.Networking;
using Server.MVVM.Model.Networking.UIRequests;
using Server.MVVM.Model.Persistence;
using Server.MVVM.Model.Persistence.DTO;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using System.Collections.Generic;

namespace Server.MVVM.ViewModel
{
    public class MainViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand SelectTab { get; }
        #endregion

        #region Properties
        private UserControlViewModel? selectedTab = null;
        public UserControlViewModel? SelectedTab
        {
            get => selectedTab;
            private set { selectedTab = value; OnPropertyChanged(); }
        }
        #endregion

        public MainViewModel()
        {
            UserControlViewModel[] tabs = null!;

            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (DialogWindow)windowLoadedE!;

                // klasy, które mogą mieć tylko 1 instancję, ale nie używamy singletona
                var logVM = new LogViewModel(window);
                Storage storage;
                ServerMonolith server;
                IEnumerable<AccountDto> allAccounts = null!;
                IEnumerable<ClientIPBlockDto> allClientIPBlocks = null!;

                try
                {
                    storage = new Storage();
                    allAccounts = storage.Database.AccountsById.GetAll();
                    allClientIPBlocks = storage.Database.ClientIPBlocks.GetAll();
                    server = new ServerMonolith(storage);
                }
                catch (Error e)
                {
                    Alert(e.Message);
                    throw;
                }

                // zapobiega ALT + F4 w głównym oknie
                window.Closable = false;
                Close = new RelayCommand(_ =>
                {
                    // Wątek UI
                    // Asynchroniczne zatrzymanie.
                    server.Request(new StopServer(() =>
                    {
                        // Wątek Server.Process
                        server.Request(new StopProcess(() => UIInvoke(() =>
                        {
                            // Wątek UI na zlecenie (poprzez UIInvoke) wątku Server.Process
                            window.Closable = true;
                            // Zamknięcie MainWindow powoduje zakończenie programu.
                            window.Close();
                        })));
                    }));
                });

                var setVM = new SettingsViewModel(window, server, logVM);
                var conCliVM = new ConnectedClientsViewModel(window, server, logVM);
                var accVM = new AccountsViewModel(window, allAccounts, server);
                var clIpBlVM = new ClientIPBlocksViewModel(window, allClientIPBlocks, server);
                tabs = new UserControlViewModel[] { setVM, conCliVM, logVM, accVM, clIpBlVM };
                SelectTab!.Execute(0.ToString());
            });

            SelectTab = new RelayCommand(e =>
            {
                int index = int.Parse((string)e!);
                if (SelectedTab == tabs[index]) return;
                SelectedTab = tabs[index];
            });
        }
    }
}
