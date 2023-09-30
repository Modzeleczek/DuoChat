using Server.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;

namespace Server.MVVM.ViewModel
{
    public class MainViewModel : WindowViewModel
    {
        #region Commands
        /* setujemy te właściwości w callbacku (RelayCommandzie) zdarzenia
        WindowLoaded, a nie w konstruktorze MainViewModel, więc potrzebne
        są settery z wywołaniami OnPropertyChanged */
        private RelayCommand _selectTab;
        public RelayCommand SelectTab
        {
            get => _selectTab;
            private set { _selectTab = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private UserControlViewModel selectedTab;
        public UserControlViewModel SelectedTab
        {
            get => selectedTab;
            private set { selectedTab = value; OnPropertyChanged(); }
        }
        #endregion

        public MainViewModel()
        {
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (DialogWindow)windowLoadedE;

                // klasy, które mogą mieć tylko 1 instancję, ale nie używamy singletona
                var log = new Log();
                Model.Server server;
                try { server = new Model.Server(log); }
                catch (Error e)
                {
                    Alert(e.Message);
                    throw;
                }

                // zapobiega ALT + F4 w głównym oknie
                window.Closable = false;
                Close = new RelayCommand(_ =>
                {
                    if (server.IsRunning)
                    {
                        // Synchroniczne zatrzymanie.
                        server.Stop();
                        CloseApplication();
                    }
                    else CloseApplication();
                });

                var setVM = new SettingsViewModel(window, server);
                var conCliVM = new ConnectedClientsViewModel(window, server);
                var logVM = new LogViewModel(window, log);
                var accVM = new AccountsViewModel(window, server);
                var tabs = new UserControlViewModel[] { setVM, conCliVM, logVM, accVM };
                SelectTab = new RelayCommand(e =>
                {
                    int index = int.Parse((string)e);
                    if (SelectedTab == tabs[index]) return;
                    SelectedTab = tabs[index];
                });

                SelectTab.Execute("0");
            });
        }

        private void CloseApplication()
        {
            window.Closable = true;
            // zamknięcie MainWindow powoduje zakończenie programu
            window.Close();
        }
    }
}
