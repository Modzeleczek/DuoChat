using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.AccountActions;
using Client.MVVM.ViewModel.ServerActions;
using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;
using Shared.MVVM.View.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private RelayCommand editServer;
        public RelayCommand EditServer
        {
            get => editServer;
            private set { editServer = value; OnPropertyChanged(); }
        }

        private RelayCommand deleteServer;
        public RelayCommand DeleteServer
        {
            get => deleteServer;
            private set { deleteServer = value; OnPropertyChanged(); }
        }

        private RelayCommand addAccount;
        public RelayCommand AddAccount
        {
            get => addAccount;
            private set { addAccount = value; OnPropertyChanged(); }
        }

        private RelayCommand editAccount;
        public RelayCommand EditAccount
        {
            get => editAccount;
            private set { editAccount = value; OnPropertyChanged(); }
        }

        private RelayCommand deleteAccount;
        public RelayCommand DeleteAccount
        {
            get => deleteAccount;
            private set { deleteAccount = value; OnPropertyChanged(); }
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
                selectedServer = value;
                SelectedAccount = null;
                Accounts.Clear();
                if (value != null)
                {
                    var getAllStatus = loggedUser.GetAllAccounts(value.IpAddress, value.Port);
                    if (getAllStatus.Code != 0)
                    {
                        getAllStatus.Prepend(d["Error occured while"],
                            d["reading user's account list."]);
                        Alert(getAllStatus.Message);
                    }
                    else
                    {
                        var accounts = (List<Account>)getAllStatus.Data;
                        for (int i = 0; i < accounts.Count; ++i)
                            Accounts.Add(accounts[i]);
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
                /* nie sprawdzamy, czy value == SelectedServer, aby można było reconnectować
                poprzez kliknięcie na już zaznaczony serwer */
                if (_client.IsConnected) _client.Disconnect();

                selectedAccount = value;
                SelectedConversation = null;
                Conversations.Clear();
                if (value != null)
                {
                    var status = _client.Connect(SelectedServer);
                    if (status.Code != 0)
                    {
                        selectedAccount = null;
                        Alert(status.Message);
                    }
                    else
                    {
                        var cnvCnt = rng.Next(0, 5);
                        for (int i = 0; i < cnvCnt; ++i)
                            Conversations.Add(Conversation.Random(rng));
                    }
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
                window = (DialogWindow)windowLoadedE;
                // zapobiega ALT + F4 w głównym oknie
                window.Closable = false;

                Servers = new ObservableCollection<Server>();
                Accounts = new ObservableCollection<Account>();
                Conversations = new ObservableCollection<Conversation>();

                var getActLangSta = lus.GetActiveLanguage();
                if (getActLangSta.Code != 0)
                    Alert(getActLangSta.Message);
                d.ActiveLanguage = (Translator.Language)getActLangSta.Data;

                var getActThmSta = lus.GetActiveTheme();
                if (getActThmSta.Code != 0)
                    Alert(getActThmSta.Message);
                ((App)Application.Current).ActiveTheme = (App.Theme)getActThmSta.Data;

                var getLogSta = lus.GetLogged();
                if (getLogSta.Code == 0) // jakiś użytkownik jest już zalogowany
                {
                    var userName = (string)getLogSta.Data;
                    var getSta = lus.Get(userName);
                    if (getSta.Code == 0) // zalogowany użytkownik istnieje w BSONie
                    {
                        loggedUser = (LocalUser)getSta.Data;
                        ResetLists();
                        return;
                    }
                    Alert(d["User set as logged does not exist."]);
                }
                ShowLocalUsersDialog();
            });

            AddServer = new RelayCommand(_ =>
            {
                var vm = new CreateServerViewModel(loggedUser)
                {
                    Title = d["Add_server"],
                    ConfirmButtonText = d["Add"]
                };
                new FormWindow(window, vm).ShowDialog();
                var status = vm.Status;
                if (status.Code == 0)
                    Servers.Add((Server)status.Data);
            });
            EditServer = new RelayCommand(obj =>
            {
                var server = (Server)obj;
                if (SelectedServer == server)
                    SelectedServer = null;
                var vm = new EditServerViewModel(loggedUser, server)
                {
                    Title = d["Edit server"],
                    ConfirmButtonText = d["Save"]
                };
                new FormWindow(window, vm).ShowDialog();
            });
            DeleteServer = new RelayCommand(obj =>
            {
                var server = (Server)obj;
                var confirmStatus = ConfirmationViewModel.ShowDialog(window,
                    d["Do you want to delete"] + " " + d["server"] +
                    $" {server.IpAddress}:{server.Port}?",
                    d["Delete server"], d["No"], d["Yes"]);
                if (confirmStatus.Code != 0)
                    return; 
                if (SelectedServer == server)
                    // setter rozłącza, jeżeli jesteśmy połączeni, bo ustawia SelectedAccount na null
                    SelectedServer = null;
                var deleteStatus = loggedUser.DeleteServer(server.IpAddress, server.Port);
                if (deleteStatus.Code != 0)
                {
                    deleteStatus.Prepend(d["Error occured while"], d["deleting"],
                        d["server;D"], d["from user's database."]);
                    Alert(deleteStatus.Message);
                    return;
                }
                Servers.Remove(server);
            });

            AddAccount = new RelayCommand(_ =>
            {
                var vm = new CreateAccountViewModel(loggedUser, SelectedServer)
                {
                    Title = d["Add_account"],
                    ConfirmButtonText = d["Add"]
                };
                new FormWindow(window, vm).ShowDialog();
                var status = vm.Status;
                if (status.Code == 0)
                    Accounts.Add((Account)status.Data);
            });
            EditAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj;
                if (SelectedAccount == account)
                    SelectedAccount = null;
                var vm = new EditAccountViewModel(loggedUser, SelectedServer, account)
                {
                    Title = d["Edit account"],
                    ConfirmButtonText = d["Save"]
                };
                new FormWindow(window, vm).ShowDialog();
            });
            DeleteAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj;
                var confirmStatus = ConfirmationViewModel.ShowDialog(window,
                    d["Do you want to delete"] + " " + d["account"] + $" {account.Login}?",
                    d["Delete account"], d["No"], d["Yes"]);
                if (confirmStatus.Code != 0)
                    return;
                if (SelectedAccount == account)
                    // setter rozłącza, jeżeli jesteśmy połączeni
                    SelectedAccount = null;
                var server = SelectedServer;
                var deleteStatus = loggedUser.DeleteAccount(server.IpAddress, server.Port,
                    account.Login);
                if (deleteStatus.Code != 0)
                {
                    deleteStatus.Prepend(d["Error occured while"], d["deleting"],
                        d["account;D"], d["from user's database."]);
                    Alert(deleteStatus.Message);
                    return;
                }
                Accounts.Remove(account);
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

            Close = new RelayCommand(_ =>
            {
                window.Closable = true;
                // zamknięcie MainWindow powoduje zakończenie programu
                window.Close();
            });

            OpenSettings = new RelayCommand(_ =>
            {
                SelectedAccount = null;
                var vm = new SettingsViewModel(loggedUser);
                var win = new SettingsWindow(window, vm);
                vm.RequestClose += () => win.Close();
                win.ShowDialog();
                if (vm.Status.Code == 2) // wylogowanie
                {
                    ClearLists();
                    var loginStatus = LocalLoginViewModel.ShowDialog(window, loggedUser, true);
                    if (loginStatus.Code != 0)
                    {
                        ResetLists();
                        return;
                    }
                    var currentPassword = (SecureString)loginStatus.Data;

                    lus.SetLogged(false);

                    var pc = new PasswordCryptography();
                    var encSta = ProgressBarViewModel.ShowDialog(window,
                        d["Encrypting user's database."], true,
                        (reporter) =>
                        pc.EncryptDirectory(reporter,
                            loggedUser.DirectoryPath,
                            pc.ComputeDigest(currentPassword, loggedUser.DbSalt),
                            loggedUser.DbInitializationVector));
                    currentPassword.Dispose();
                    if (encSta.Code == 1)
                        return;
                    // nie udało się zaszyfrować katalogu użytkownika, więc też nie wylogowujemy
                    else if (encSta.Code != 0) return;
                    loggedUser = null;
                    ShowLocalUsersDialog();
                }
            });
        }

        private void ShowLocalUsersDialog()
        {
            var lus = new LocalUsersStorage();
            while (true)
            {
                var loginStatus = LocalUsersViewModel.ShowDialog(window);
                /* jeżeli użytkownik zamknął okno bez zalogowania się
                (nie ma innych możliwych kodów niż 0 i 1) */
                if (loginStatus.Code == 1)
                {
                    Application.Current.Shutdown();
                    return;
                }
                // jeżeli użytkownik się zalogował, to vm.Status.Code == 0
                var dat = (dynamic)loginStatus.Data;
                var curPas = (SecureString)dat.Password;
                var user = (LocalUser)dat.LoggedUser;

                var pc = new PasswordCryptography();
                var decryptionStatus = ProgressBarViewModel.ShowDialog(window,
                    d["Decrypting user's database."], true,
                    (reporter) =>
                    pc.DecryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(curPas, user.DbSalt),
                        user.DbInitializationVector));
                curPas.Dispose();
                if (decryptionStatus.Code == 1)
                    Alert(d["User's database decryption canceled. Logging out."]);
                /* jeżeli status.Code < 0, to alert z błędem został już wyświetlony w
                ProgressBarViewModel.Worker_RunWorkerCompleted */
                else if (decryptionStatus.Code == 0)
                {
                    lus.SetLogged(true, user.Name);
                    loggedUser = user;
                    ResetLists();
                    return;
                }
            }
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
            var getAllStatus = loggedUser.GetAllServers();
            if (getAllStatus.Code != 0)
            {
                getAllStatus.Prepend(d["Error occured while"], d["reading user's server list."]);
                Alert(getAllStatus.Message);
                return;
            }
            var servers = (List<Server>)getAllStatus.Data;
            for (int i = 0; i < servers.Count; ++i)
                Servers.Add(servers[i]);
        }
    }
}
