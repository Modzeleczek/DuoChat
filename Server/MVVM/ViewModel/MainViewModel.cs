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

                    /* long id = -1;
                    switch (index)
                    {
                        case 0:
                            database.EnterWriteLock();
                            var user = new UserDTO
                            {
                                Login = "elo",
                                PublicKey = new PublicKey(new byte[] { 5 * 31 })
                            };
                            database.Users.AddUser(ref user);
                            id = user.Id;
                            database.ExitWriteLock();
                            break;
                        case 1:
                            database.EnterReadLock();
                            var users = database.Users.GetAllUsers();
                            if (users.Count > 0)
                            {
                                var first = users.First;
                            }
                            database.ExitReadLock();
                            break;
                        case 2:
                            database.EnterWriteLock();
                            database.Users.DeleteUser(id);
                            database.ExitWriteLock();
                            break;
                    } */
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
