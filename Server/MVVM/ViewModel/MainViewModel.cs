using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using BaseViewModel = Shared.MVVM.ViewModel.ViewModel;

namespace Server.MVVM.ViewModel
{
    public class MainViewModel : WindowViewModel
    {
        #region Commands
        // setujemy te właściwości w callbacku (RelayCommandzie) zdarzenia WindowLoaded, a nie w konstruktorze MainViewModel, więc potrzebne są settery z wywołaniami OnPropertyChanged
        private RelayCommand _selectTab;
        public RelayCommand SelectTab
        {
            get => _selectTab;
            private set { _selectTab = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private BaseViewModel selectedTab;
        public BaseViewModel SelectedTab
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

                var server = new Model.Server();

                // zapobiega ALT + F4 w głównym oknie
                window.Closable = false;
                Callback closeApplication = (_) =>
                {
                    window.Closable = true;
                    // zamknięcie MainWindow powoduje zakończenie programu
                    UIInvoke(() => window.Close());
                };
                Close = new RelayCommand(_ =>
                {
                    if (server.IsRunning)
                    {
                        server.Stopped += closeApplication;
                        server.RequestStop();
                    }
                    else closeApplication(null);
                });

                var setVM = new SettingsViewModel(window, server);
                var conCliVM = new ConnectedClientsViewModel(window, server);
                var accVM = new AccountsViewModel(window, server);
                var tabs = new BaseViewModel[] { setVM, conCliVM, accVM };
                SelectTab = new RelayCommand(e =>
                {
                    int index = int.Parse((string)e);
                    if (SelectedTab == tabs[index]) return;
                    SelectedTab = tabs[index];
                });

                server.Started += (status) =>
                {
                    string message = null;
                    if (status.Code == 0) message = "|Server was started.|";
                    else message = status.Prepend("|Server was not started.|").Message;
                    UIInvoke(() => Alert(message));
                };
                server.Stopped += (status) =>
                {
                    string message = null;
                    if (status.Code == 0) message = "|Server was safely stopped.|";
                    else message = status.Prepend("|Server was suddenly stopped.|").Message;
                    UIInvoke(() => Alert(message));
                };

                SelectTab.Execute("0");
            });
        }

        private void Alert(string description) => AlertViewModel.ShowDialog(window, description);
    }
}
