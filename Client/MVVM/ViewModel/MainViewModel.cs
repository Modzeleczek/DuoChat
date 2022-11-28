using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using System.ComponentModel;
using System.Security;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class MainViewModel : ViewModel
    {
        #region Commands
        private RelayCommand send;
        public RelayCommand Send
        {
            get => send;
            private set { send = value; OnPropertyChanged(); }
        }

        private RelayCommand close;
        public RelayCommand Close
        {
            get => close;
            private set { close = value; OnPropertyChanged(); }
        }

        private RelayCommand openSettings;
        public RelayCommand OpenSettings
        {
            get => openSettings;
            private set { openSettings = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private Account account;
        public Account Account
        {
            get => account;
            private set { account = value; OnPropertyChanged(); }
        }

        private Server selectedServer;
        public Server SelectedServer
        {
            get { return selectedServer; }
            set { selectedServer = value; OnPropertyChanged(); }
        }

        private Friend selectedFriend;
        public Friend SelectedFriend
        {
            get { return selectedFriend; }
            set { selectedFriend = value; OnPropertyChanged(); }
        }

        private string messageContent;
        public string MessageContent
        {
            get { return messageContent; }
            set { messageContent = value; OnPropertyChanged(); }
        }
        #endregion

        private LoggedUser loggedUser = null;
        
        public MainViewModel()
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (Window)windowLoadedE;

                Send = new RelayCommand(o =>
                {
                    
                });

                Close = new RelayCommand(e =>
                {
                    // przed faktycznym zamknięciem MainWindow, co powoduje zakończenie programu
                });
                OpenSettings = new RelayCommand(_ =>
                {
                    var vm = new SettingsViewModel(loggedUser);
                    var win = new SettingsWindow(window, vm);
                    vm.RequestClose += (s, e) => win.Close();
                    win.ShowDialog();
                    if (vm.Status.Code == 2) // wylogowanie
                    {
                        /* var userRef = loggedUser;
                        loggedUser = null; // usunięcie użytkownika z UI */
                        var logSta = LocalLoginViewModel.Dialog(window, loggedUser, true);
                        if (logSta.Code != 0) return;
                        var curPas = (SecureString)((dynamic)logSta.Data).Password;

                        lus.SetLogged(false);

                        var pc = new PasswordCryptography();
                        var decSta = ProgressBarViewModel.ShowDialog(window,
                            d["Encryption"],
                            d["Encrypting user's database."],
                            (sender, args) =>
                            pc.EncryptDatabase((BackgroundWorker)sender, args, loggedUser, curPas));

                        curPas.Dispose();
                        loggedUser = null;
                        ShowLocalUsersDialog(lus);
                    }
                });

                var getLogSta = lus.GetLogged();
                if (getLogSta.Code == 0)
                {
                    var userName = (string)getLogSta.Data;
                    var getSta = lus.Get(userName);
                    if (getSta.Code == 0)
                    {
                        loggedUser = new LoggedUser((LocalUser)getSta.Data);
                        return;
                    }
                    Error(d["Logged user does not exist."]);
                }
                ShowLocalUsersDialog(lus);
            });
        }

        private Status ShowLocalUsersDialog(LocalUsersStorage lus)
        {
            var vm = new LocalUsersViewModel();
            var win = new LocalUsersWindow(window, vm);
            vm.RequestClose += (s, e) => win.Close();
            win.ShowDialog();
            var status = vm.Status;
            // jeżeli użytkownik się zalogował, to vm.Status.Code == 0
            if (status.Code != 0) // jeżeli użytkownik zamknął okno bez zalogowania się
                Application.Current.Shutdown();
            else
            {
                var dat = (dynamic)status.Data;
                var curPas = (SecureString)dat.Password;
                loggedUser = (LoggedUser)dat.LoggedUser;
                lus.SetLogged(true, loggedUser.Name);
                var dao = loggedUser.GetDataAccessObject();

                var pc = new PasswordCryptography();
                var decSta = ProgressBarViewModel.ShowDialog(window,
                        d["Decryption"],
                        d["Decrypting user's database."],
                        (sender, args) =>
                        pc.DecryptDatabase((BackgroundWorker)sender, args, loggedUser, curPas));

                if (!dao.DatabaseFileHealthy())
                {
                    // TODO: pytamy użytkownika, czy chce pusty plik z bazą, czy chce się wylogować
                    Error(d["User's database is corrupted. An empty database will be created."]);
                    dao.DeleteDatabaseFile();
                    dao.InitializeDatabaseFile();
                }
                curPas.Dispose();
            }
            return status;
        }
    }
}
