using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.AccountActions;
using Client.MVVM.ViewModel.Observables;
using Client.MVVM.ViewModel.ServerActions;
using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System;
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
                    try
                    {
                        var accounts = loggedUser.GetAllAccounts(value.IpAddress, value.Port);
                        for (int i = 0; i < accounts.Count; ++i)
                            Accounts.Add(accounts[i]);
                    }
                    catch (Error e)
                    {
                        e.Prepend("|Error occured while| " +
                            "|reading user's account list.|");
                        Alert(e.Message);
                        throw;
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
                if (_client.IsConnected)
                    /* Synchroniczne rozłączenie - blokuje UI do momentu powrotu
                    z metody Disconnect. */
                    _client.Disconnect();

                selectedAccount = value;
                SelectedConversation = null;
                Conversations.Clear();
                if (value != null)
                {
                    try
                    {
                        var ser = SelectedServer;
                        // Synchroniczne łączenie.
                        _client.Connect(ser.IpAddress.ToIPAddress(), ser.Port.Value);
                        var cnvCnt = rng.Next(0, 5);
                        for (int i = 0; i < cnvCnt; ++i)
                            Conversations.Add(Conversation.Random(rng));
                    }
                    catch (Error e)
                    {
                        selectedAccount = null;
                        Alert(e.Message);
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

        #region Fields
        private LocalUser loggedUser = null;
        private Model.Client _client = new Model.Client();
        private Random rng = new Random();
        #endregion

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

                try
                {
                    d.ActiveLanguage = (Translator.Language)lus.GetActiveLanguage();
                }
                catch (Error e)
                {
                    Alert(e.Message);
                    // przekazanie dalej wyjątku spowoduje zamknięcie programu
                    throw;
                }

                try
                {
                    ((App)Application.Current).ActiveTheme = (App.Theme)lus.GetActiveTheme();
                }
                catch (Error e)
                {
                    Alert(e.Message);
                    throw;
                }

                var loggedUserName = lus.GetLogged();
                if (!(loggedUserName is null)) // jakiś użytkownik jest już zalogowany
                {
                    var user = lus.Get(loggedUserName);
                    if (!(user is null)) // zalogowany użytkownik istnieje w BSONie
                    {
                        loggedUser = user;
                        ResetLists();
                        return;
                    }
                    Alert("|User set as logged does not exist.|");
                }
                else
                    Alert("|No user is logged.|");
                ShowLocalUsersDialog();
            });

            AddServer = new RelayCommand(_ =>
            {
                var vm = new CreateServerViewModel(loggedUser)
                {
                    Title = "|Add_server|",
                    ConfirmButtonText = "|Add|"
                };
                new FormWindow(window, vm).ShowDialog();
                var result = vm.Result;
                if (result is Success success)
                    Servers.Add((Server)success.Data);
            });
            EditServer = new RelayCommand(obj =>
            {
                var server = (Server)obj;
                if (SelectedServer == server)
                    SelectedServer = null;
                var vm = new EditServerViewModel(loggedUser, server)
                {
                    Title = "|Edit server|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
            });
            DeleteServer = new RelayCommand(obj =>
            {
                var server = (Server)obj;
                var confirmRes = ConfirmationViewModel.ShowDialog(window,
                    "|Do you want to delete| |server|" +
                    $" {server.IpAddress}:{server.Port}?",
                    "|Delete server|", "|No|", "|Yes|");
                if (!(confirmRes is Success)) return;
                if (SelectedServer == server)
                    // setter rozłącza, jeżeli jesteśmy połączeni, bo ustawia SelectedAccount na null
                    SelectedServer = null;
                try { loggedUser.DeleteServer(server.IpAddress, server.Port); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |deleting| " +
                        "|server;D| |from user's database.|");
                    Alert(e.Message);
                    throw;
                }
                Servers.Remove(server);
            });

            AddAccount = new RelayCommand(_ =>
            {
                var vm = new CreateAccountViewModel(loggedUser, SelectedServer)
                {
                    Title = "|Add_account|",
                    ConfirmButtonText = "|Add|"
                };
                new FormWindow(window, vm).ShowDialog();
                var result = vm.Result;
                if (result is Success success)
                    Accounts.Add((Account)success.Data);
            });
            EditAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj;
                if (SelectedAccount == account)
                    SelectedAccount = null;
                var vm = new EditAccountViewModel(loggedUser, SelectedServer, account)
                {
                    Title = "|Edit account|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
            });
            DeleteAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj;
                var confirmRes = ConfirmationViewModel.ShowDialog(window,
                    "|Do you want to delete| |account|" + $" {account.Login}?",
                    "|Delete account|", "|No|", "|Yes|");
                if (!(confirmRes is Success)) return;
                if (SelectedAccount == account)
                    // setter rozłącza, jeżeli jesteśmy połączeni
                    SelectedAccount = null;
                var server = SelectedServer;
                try { loggedUser.DeleteAccount(server.IpAddress, server.Port,
                    account.Login); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |deleting| " +
                        "|account;D| |from user's database.|");
                    Alert(e.Message);
                    throw;
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
                var vm = new SettingsViewModel(loggedUser);
                var win = new SettingsWindow(window, vm);
                vm.RequestClose += () => win.Close();
                win.ShowDialog();
                if (vm.Result is Success settingsSuc &&
                    (SettingsViewModel.Operations)settingsSuc.Data ==
                    SettingsViewModel.Operations.LocalLogout) // wylogowanie
                {
                    ClearLists();
                    var loginRes = LocalLoginViewModel.ShowDialog(window, loggedUser, true);
                    if (!(loginRes is Success loginSuc))
                    {
                        ResetLists();
                        return;
                    }
                    var currentPassword = (SecureString)loginSuc.Data;

                    lus.SetLogged(false);

                    var pc = new PasswordCryptography();
                    var encryptRes = ProgressBarViewModel.ShowDialog(window,
                        "|Encrypting user's database.|", true,
                        (reporter) =>
                        pc.EncryptDirectory(reporter,
                            loggedUser.DirectoryPath,
                            pc.ComputeDigest(currentPassword, loggedUser.DbSalt),
                            loggedUser.DbInitializationVector));
                    currentPassword.Dispose();
                    if (encryptRes is Cancellation)
                        return;
                    else if (encryptRes is Failure failure)
                    {
                        // nie udało się zaszyfrować katalogu użytkownika, więc crashujemy program
                        var e = failure.Reason;
                        e.Prepend("|Error occured while| " +
                            "|encrypting user's database.| |Database may have been corrupted.|");
                        Alert(e.Message);
                        throw e;
                    }
                    loggedUser = null;
                    ShowLocalUsersDialog();
                }
            });

            SetClientEventHandlers();
        }

        private void ShowLocalUsersDialog()
        {
            var lus = new LocalUsersStorage();
            while (true)
            {
                var loginRes = LocalUsersViewModel.ShowDialog(window);
                /* jeżeli użytkownik zamknął okno bez zalogowania się
                (nie ma innych możliwych wyników niż Success i Cancellation) */
                if (!(loginRes is Success loginSuc))
                {
                    Application.Current.Shutdown();
                    return;
                }
                // jeżeli użytkownik się zalogował, to vm.Result is Success
                var dat = (dynamic)loginSuc.Data;
                var curPas = (SecureString)dat.Password;
                var user = (LocalUser)dat.LoggedUser;

                var pc = new PasswordCryptography();
                var decryptRes = ProgressBarViewModel.ShowDialog(window,
                    "|Decrypting user's database.|", true,
                    (reporter) =>
                    pc.DecryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(curPas, user.DbSalt),
                        user.DbInitializationVector));
                curPas.Dispose();
                if (decryptRes is Cancellation)
                    Alert("|User's database decryption canceled. Logging out.|");
                /* jeżeli decryptRes is Failure, to alert z błędem został już wyświetlony w
                ProgressBarViewModel.Worker_RunWorkerCompleted */
                else if (decryptRes is Success)
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
            try
            {
                var servers = loggedUser.GetAllServers();
                for (int i = 0; i < servers.Count; ++i)
                    Servers.Add(servers[i]);
            }
            catch (Error e)
            {
                e.Prepend("|Error occured while| |reading user's server list.|");
                Alert(e.Message);
                throw;
            }
        }

        private void SetClientEventHandlers()
        {
            _client.LostConnection += (result) =>
            {
                /* Jeżeli my się rozłączamy, czyli
                _disconnectRequested == true, to w
                Client.Process po Task.WaitAll nie wykona się
                LostConnection?.Invoke. */
                string message;
                if (result is Success)
                    message = "|Disconnected by server.|";
                else if (result is Failure failure)
                    message = failure.Reason.Prepend("|Server crashed.|").Message;
                else // result is Cancellation
                    message = "|Disconnected.|";
                UIInvoke(() =>
                {
                    SelectedAccount = null;
                    Alert(message);
                });
            };

            _client.ReceivedPacket += (result) =>
            {
                var packet = (byte[])((Success)result).Data;
                var operationCode = packet[0];
                switch (operationCode)
                {
                    case 1: HandleNoSlots(); break;
                }
            };
        }

        private void HandleNoSlots()
        {
            UIInvoke(() => Alert("No slots."));
        }
    }
}
