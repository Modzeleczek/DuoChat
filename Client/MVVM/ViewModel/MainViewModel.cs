using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model.JsonSerializables;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class MainViewModel : WindowViewModel
    {
        #region Commands
        private RelayCommand send;
        public RelayCommand Send
        {
            get => send;
            private set { send = value; OnPropertyChanged(); }
        }

        private RelayCommand openSettings;
        public RelayCommand OpenSettings
        {
            get => openSettings;
            private set { openSettings = value; OnPropertyChanged(); }
        }

        private RelayCommand addServer;
        public RelayCommand AddServer
        {
            get => addServer;
            private set { addServer = value; OnPropertyChanged(); }
        }

        private RelayCommand deleteServer;
        public RelayCommand DeleteServer
        {
            get => deleteServer;
            private set { deleteServer = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private ObservableCollection<Server> servers;
        public ObservableCollection<Server> Servers
        {
            get => servers;
            set { servers = value; OnPropertyChanged(); }
        }

        private Server selectedServer;
        public Server SelectedServer
        {
            get => selectedServer;
            set
            {
                /* nie sprawdzamy, czy value == SelectedServer, aby można było reconnectować
                poprzez kliknięcie na już zaznaczony serwer */
                if (_client.IsConnected) _client.Disconnect();
                selectedServer = value;
                SelectedAccount = null;
                Accounts.Clear();
                if (value != null)
                {
                    var status = _client.Connect(SelectedServer);
                    if (status.Code != 0)
                    {
                        selectedServer = null;
                        Alert(status.Message);
                    }
                    else
                    {
                        /* TODO: wczytujemy z lokalnej bazy danych klienta konta posiadane przez
                         * użytkownika na serwerze, z którym się połączyliśmy */
                        var accCnt = rng.Next(3, 8);
                        for (int i = 0; i < accCnt; ++i)
                            Accounts.Add(Account.Random(rng));
                    }
                }
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Account> accounts;
        public ObservableCollection<Account> Accounts
        {
            get => accounts;
            set { accounts = value; OnPropertyChanged(); }
        }

        private Account selectedAccount;
        public Account SelectedAccount
        {
            get => selectedAccount;
            set
            {
                selectedAccount = value;
                SelectedConversation = null;
                Conversations.Clear();
                if (value != null)
                {
                    var cnvCnt = rng.Next(0, 5);
                    for (int i = 0; i < cnvCnt; ++i)
                        Conversations.Add(Conversation.Random(rng));
                }
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Conversation> conversations;
        public ObservableCollection<Conversation> Conversations
        {
            get => conversations;
            set { conversations = value; OnPropertyChanged(); }
        }

        private Conversation selectedConversation;
        public Conversation SelectedConversation
        {
            get => selectedConversation;
            set { selectedConversation = value; OnPropertyChanged(); }
        }

        private string writtenMessage;
        public string WrittenMessage
        {
            get => writtenMessage;
            set { writtenMessage= value; OnPropertyChanged(); }
        }
        #endregion

        private LocalUser loggedUser = null;
        private Model.Client _client = new Model.Client();
        private Random rng = new Random();
        
        public MainViewModel()
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (Window)windowLoadedE;

                AddServer = new RelayCommand(_ =>
                {
                    var vm = new CreateServerViewModel(loggedUser);
                    var win = new FormWindow(window, vm, d["Add_server"], new FormWindow.Field[]
                    {
                        new FormWindow.Field(d["IP address"], "", false),
                        new FormWindow.Field(d["Port"], "", false)
                    }, d["Cancel"], d["Add"]);
                    vm.RequestClose += (s, e) => win.Close();
                    win.ShowDialog();
                    var status = vm.Status;
                    if (status.Code == 0)
                        Servers.Add((Server)status.Data);
                });
                DeleteServer = new RelayCommand(obj =>
                {
                    var server = (Server)obj;
                    var status = ConfirmationViewModel.ShowDialog(window,
                        d["Do you want to delete server '"] + server.Name + d["'?"],
                        d["Delete server"], d["No"], d["Yes"]);
                    if (status.Code == 0)
                    {
                        if (SelectedServer == server)
                        {
                            // rozłączamy z serwerem synchronicznie (czekając na zakończenie rozłączania)
                            _client.Disconnect();
                        }
                        Servers.Remove(server);
                        loggedUser.DeleteServer(server.Guid);
                    }
                });

                Send = new RelayCommand(o =>
                {
                    if (SelectedConversation == null ||
                        WrittenMessage.Length == 0) return;
                    var rnd = Message.Random(rng);
                    rnd.PlainContent = WrittenMessage;
                    SelectedConversation.Messages.Add(rnd);
                    WrittenMessage = "";
                });

                // zapobiega ALT + F4 w głównym oknie
                CancelEventHandler closingCancHandl = (_, e) => e.Cancel = true;
                window.Closing += closingCancHandl;
                Close = new RelayCommand(_ =>
                {
                    window.Closing -= closingCancHandl;
                    // zamknięcie MainWindow powoduje zakończenie programu
                    window.Close();
                });

                OpenSettings = new RelayCommand(_ =>
                {
                    var vm = new SettingsViewModel(loggedUser);
                    var win = new SettingsWindow(window, vm);
                    vm.RequestClose += (s, e) => win.Close();
                    win.ShowDialog();
                    if (vm.Status.Code == 2) // wylogowanie
                    {
                        ClearLists();
                        var logSta = LocalLoginViewModel.ShowDialog(window, loggedUser, true);
                        if (logSta.Code != 0)
                        {
                            ResetLists();
                            return;
                        }
                        var curPas = (SecureString)((dynamic)logSta.Data).Password;

                        lus.SetLogged(false);

                        var pc = new PasswordCryptography();
                        var encSta = ProgressBarViewModel.ShowDialog(window,
                            d["Encrypting user's database."], true,
                            (reporter) =>
                            pc.EncryptDirectory(reporter,
                                loggedUser.DirectoryPath,
                                pc.ComputeDigest(curPas, loggedUser.DbSalt),
                                loggedUser.DbInitializationVector));
                        curPas.Dispose();
                        if (encSta.Code == 1)
                        {
                            Alert(d["User's database encryption canceled. Not logging out."]);
                            return;
                        }
                        else if (encSta.Code != 0) return;
                        loggedUser = null;
                        while (ShowLocalUsersDialog(lus).Code != 0) ;
                    }
                });

                Servers = new ObservableCollection<Server>();
                Accounts = new ObservableCollection<Account>();
                Conversations = new ObservableCollection<Conversation>();
                d.ActiveLanguageId = (int)lus.GetActiveLanguage().Data;
                var getLogSta = lus.GetLogged();
                if (getLogSta.Code == 0) // jakiś użytkownik jest już zalogowany
                {
                    var userName = (string)getLogSta.Data;
                    var getSta = lus.Get(userName);
                    if (getSta.Code == 0) // zalogowany użytkownik istnieje w BSONie
                    {
                        loggedUser = ((LocalUserSerializable)getSta.Data).ToObservable();
                        ResetLists();
                        return;
                    }
                    Alert(d["Logged user does not exist."]);
                }
                // gdy < 0, to błąd; gdy == 1, to użytkownik anulował odszyfrowywanie bazy danych
                while (ShowLocalUsersDialog(lus).Code != 0) ;
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
                var user = (LocalUser)dat.LoggedUser;

                var pc = new PasswordCryptography();
                status = ProgressBarViewModel.ShowDialog(window,
                    d["Decrypting user's database."], true,
                    (reporter) =>
                    pc.DecryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(curPas, user.DbSalt),
                        user.DbInitializationVector));
                curPas.Dispose();
                if (status.Code == 1)
                    Alert(d["User's database decryption canceled. Logging out."]);
                /* jeżeli status.Code < 0, to alert z błędem został już wyświetlony w
                ProgressBarViewModel.Worker_RunWorkerCompleted */
                else if (status.Code == 0)
                {
                    lus.SetLogged(true, user.Name);
                    loggedUser = user;
                    ResetLists();
                }
            }
            return status;
        }
        
        private void ClearLists()
        {
            SelectedConversation = null;
            SelectedAccount = null;
            SelectedServer = null;
            Conversations.Clear();
            Accounts.Clear();
            Servers.Clear();
        }

        private void ResetLists()
        {
            Servers.Clear();
            Accounts.Clear();
            Conversations.Clear();
            var servers = loggedUser.GetAllServers();
            for (int i = 0; i < servers.Count; ++i)
                Servers.Add(servers[i]);
        }
    }
}
