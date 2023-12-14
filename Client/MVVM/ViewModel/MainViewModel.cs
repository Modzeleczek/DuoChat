using Client.MVVM.Model;
using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.AccountActions;
using Client.MVVM.ViewModel.LocalUsers;
using Client.MVVM.ViewModel.Observables;
using Client.MVVM.ViewModel.ServerActions;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.View.Localization;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Collections.ObjectModel;
using System.Security;
using System.Text;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class MainViewModel : WindowViewModel
    {
        #region Commands
        private RelayCommand _openSettings = null!;
        public RelayCommand OpenSettings
        {
            get => _openSettings;
            private set { _openSettings = value; OnPropertyChanged(); }
        }

        private RelayCommand _addServer = null!;
        public RelayCommand AddServer
        {
            get => _addServer;
            private set { _addServer = value; OnPropertyChanged(); }
        }

        private RelayCommand _editServer = null!;
        public RelayCommand EditServer
        {
            get => _editServer;
            private set { _editServer = value; OnPropertyChanged(); }
        }

        private RelayCommand _deleteServer = null!;
        public RelayCommand DeleteServer
        {
            get => _deleteServer;
            private set { _deleteServer = value; OnPropertyChanged(); }
        }

        private RelayCommand _addAccount = null!;
        public RelayCommand AddAccount
        {
            get => _addAccount;
            private set { _addAccount = value; OnPropertyChanged(); }
        }

        private RelayCommand _editAccount = null!;
        public RelayCommand EditAccount
        {
            get => _editAccount;
            private set { _editAccount = value; OnPropertyChanged(); }
        }

        private RelayCommand _deleteAccount = null!;
        public RelayCommand DeleteAccount
        {
            get => _deleteAccount;
            private set { _deleteAccount = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private ObservableCollection<Observables.Server> _servers =
            new ObservableCollection<Observables.Server>();
        // Potencjalnie można usunąć _servers i zostawić samą właściwość Servers.
        public ObservableCollection<Observables.Server> Servers
        {
            get => _servers;
            set { _servers = value; OnPropertyChanged(); }
        }

        private Observables.Server? _selectedServer = null;
        public Observables.Server? SelectedServer
        {
            get => _selectedServer;
            set
            {
                window!.SetEnabled(false);

                if (!(SelectedServer is null) && !(SelectedAccount is null))
                    _client.Request(new Disconnect(SelectedServer.GetPrimaryKey(), () => UIInvoke(() =>
                        // Wątek UI na zlecenie (poprzez UIInvoke) wątku Client.Process
                        /* Przed wywołaniem niniejszego callbacka w OnServerEndedConnection
                        czyścimy konwersacje i zaznaczone konto. */
                        FinishSettingSelectedServer(value))));
                else
                    // Na pewno SelectedAccount is null.
                    FinishSettingSelectedServer(value);
            }
        }

        private void FinishSettingSelectedServer(Observables.Server? value)
        {
            _selectedServer = value;
            OnPropertyChanged(nameof(SelectedServer));

            // Czyścimy i odświeżamy listę kont.
            Accounts.Clear();
            if (!(value is null))
            {
                try
                {
                    var accounts = _storage.GetAllAccounts(_loggedUserKey,
                        value.GetPrimaryKey());
                    foreach (var acc in accounts)
                        Accounts.Add(acc);
                }
                catch (Error e)
                {
                    e.Prepend("|Could not| |read user's account list|.");
                    Alert(e.Message);
                    throw;
                }
            }

            window!.SetEnabled(true);
        }

        private ObservableCollection<Account> _accounts =
            new ObservableCollection<Account>();
        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set { _accounts = value; OnPropertyChanged(); }
        }

        private Account? _selectedAccount = null;
        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                /* Na pewno !(SelectedServer is null), w przeciwnym przypadku użytkownik nie
                widzi nic na liście kont. */

                // Wyłączamy wszelkie interakcje z oknem.
                window!.SetEnabled(false);

                if (!(SelectedAccount is null))
                {
                    // Rozłączamy aktualne konto, bo jesteśmy połączeni.
                    if (!(value is null))
                        // Chcemy się połączyć.
                        DisconnectAndConnectWithAccount(value);
                    else
                        /* Nie chcemy się połączyć. Obsługujemy rozłączenie w OnServerEndedConnection,
                        a następnie w callbacku UIRequesta przywracamy interakcje. */
                        _client.Request(new Disconnect(SelectedServer!.GetPrimaryKey(),
                            () => UIInvoke(() => window.SetEnabled(true))));
                }
                else
                {
                    // Nie jesteśmy połączeni.
                    if (!(value is null))
                        // Chcemy się połączyć.
                        DisconnectAndConnectWithAccount(value);

                    // else (nieprawdopodobne): nie chcemy się połączyć.
                    window.SetEnabled(true);
                }
            }
        }

        private void DisconnectAndConnectWithAccount(Account value)
        {
            // Wątek UI
            _client.Request(new Disconnect(SelectedServer!.GetPrimaryKey(), () =>
            {
                // Wątek Client.Process
                _client.Request(new Connect(SelectedServer!.GetPrimaryKey(),
                    value.Login, value.PrivateKey, errorMsg =>
                    {
                        // Wątek Client.Process
                        // Można też dać UIInvoke na całą lambdę.
                        Account? newValue = value;
                        if (!(errorMsg is null))
                            // Nie połączyliśmy się.
                            newValue = null;

                        _selectedAccount = newValue;
                        UIInvoke(() =>
                        {
                            // Wątek UI
                            OnPropertyChanged(nameof(SelectedAccount));

                            /* Przed callbackiem UIRequesta Disconnect zostanie
                            wykonane OnServerEndedConnection. */
                            if (!(errorMsg is null))
                                // Nie połączyliśmy się.
                                Alert(errorMsg);

                            // Przywracamy interakcje z oknem.
                            window!.SetEnabled(true);
                        });
                    }));
            }));
        }

        private ObservableCollection<Conversation> _conversations =
            new ObservableCollection<Conversation>();
        public ObservableCollection<Conversation> Conversations
        {
            get => _conversations;
            set { _conversations = value; OnPropertyChanged(); }
        }

        private ConversationViewModel _conversationVM = null!;
        public ConversationViewModel ConversationVM
        {
            get => _conversationVM;
            private set { _conversationVM = value; OnPropertyChanged(); }
        }
        #endregion

        #region Fields
        private LocalUserPrimaryKey _loggedUserKey;
        private readonly ClientMonolith _client;
        private readonly Storage _storage;
        #endregion

        public MainViewModel()
        {
            // Do potomnych okien przekazujemy na zasadzie dependency injection.
            try { _storage = new Storage(); }
            catch (Error e)
            {
                Alert(e.Message);
                throw;
            }

            _client = new ClientMonolith();

            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (DialogWindow)windowLoadedE!;
                // window.SetEnabled(false)
                // zapobiega ALT + F4 w głównym oknie
                window.Closable = false;

                ConversationVM = new ConversationViewModel(window);

                try { d.ActiveLanguage = (Translator.Language)_storage.GetActiveLanguage(); }
                catch (Error e)
                {
                    Alert(e.Message);
                    // przekazanie dalej wyjątku spowoduje zamknięcie programu
                    throw;
                }

                try { ((App)Application.Current).ActiveTheme = (App.Theme)_storage.GetActiveTheme(); }
                catch (Error e)
                {
                    Alert(e.Message);
                    throw;
                }

                var loggedLocalUserKey = _storage.GetLoggedLocalUserKey();
                if (!(loggedLocalUserKey is null)) // jakiś użytkownik jest już zalogowany
                {
                    if (_storage.LocalUserExists(loggedLocalUserKey.Value))
                    {
                        // zalogowany użytkownik istnieje w BSONie
                        _loggedUserKey = loggedLocalUserKey.Value;
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
                var vm = new CreateServerViewModel(_storage, _loggedUserKey)
                {
                    Title = "|Add_server|",
                    ConfirmButtonText = "|Add|"
                };
                new FormWindow(window!, vm).ShowDialog();
                var result = vm.Result;
                if (result is Success success)
                    Servers.Add((Observables.Server)success.Data!);
            });
            EditServer = new RelayCommand(obj =>
            {
                var server = (Observables.Server)obj!;
                /* Asynchronicznie rozłączamy i odznaczamy aktualnie wybrany
                serwer. Na UI zostanie wyświetlony dialog (okno) edycji serwera,
                a w oknie pod nim użytkownik nie może w tym czasie nic kliknąć,
                dzięki czemu możemy w tle asynchronicznie rozłączyć. */
                if (SelectedServer == server)
                    SelectedServer = null;

                var vm = new EditServerViewModel(_storage,
                    _loggedUserKey, server.GetPrimaryKey())
                {
                    Title = "|Edit server|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window!, vm).ShowDialog();
                if (vm.Result is Success success)
                {
                    var updatedServer = (Observables.Server)success.Data!;
                    updatedServer.CopyTo(server);
                }
            });
            DeleteServer = new RelayCommand(obj =>
            {
                var server = (Observables.Server)obj!;

                var serverKey = server.GetPrimaryKey();
                var confirmRes = ConfirmationViewModel.ShowDialog(window!,
                    "|Do you want to delete| |server|" +
                    $" {serverKey.ToString("{0}:{1}")}?",
                    "|Delete server|", "|No|", "|Yes|");
                if (!(confirmRes is Success)) return;

                if (SelectedServer == server)
                    SelectedServer = null;

                try { _storage.DeleteServer(_loggedUserKey, serverKey); }
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
                var vm = new CreateAccountViewModel(_storage, _loggedUserKey,
                    SelectedServer!.GetPrimaryKey())
                {
                    Title = "|Add_account|",
                    ConfirmButtonText = "|Add|"
                };
                new FormWindow(window!, vm).ShowDialog();
                if (vm.Result is Success success)
                    Accounts.Add((Account)success.Data!);
            });
            EditAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj!;
                if (SelectedAccount == account)
                    SelectedAccount = null;

                var vm = new EditAccountViewModel(_storage, _loggedUserKey,
                    SelectedServer!.GetPrimaryKey(), account.Login)
                {
                    Title = "|Edit account|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window!, vm).ShowDialog();
                if (vm.Result is Success success)
                {
                    var updatedAccount = (Account)success.Data!;
                    updatedAccount.CopyTo(account);
                }
            });
            DeleteAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj!;
                var confirmRes = ConfirmationViewModel.ShowDialog(window!,
                    "|Do you want to delete| |account|" + $" {account.Login}?",
                    "|Delete account|", "|No|", "|Yes|");
                if (!(confirmRes is Success)) return;

                if (SelectedAccount == account)
                    SelectedAccount = null;

                try
                {
                    _storage.DeleteAccount(_loggedUserKey,
                        SelectedServer!.GetPrimaryKey(), account.Login);
                }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |deleting| " +
                        "|account;D| |from user's database.|");
                    Alert(e.Message);
                    throw;
                }
                Accounts.Remove(account);
            });

            Close = new RelayCommand(_ =>
            {
                var stopProcessRequest = new StopProcess(() => UIInvoke(() =>
                {
                    // Wątek Client.Process
                    window!.Closable = true;
                    // zamknięcie MainWindow powoduje zakończenie programu
                    window.Close();
                }));

                if (!(SelectedAccount is null))
                    _client.Request(new Disconnect(SelectedServer!.GetPrimaryKey(),
                        // Wątek Client.Process
                        () => _client.Request(stopProcessRequest)));
                else
                    _client.Request(stopProcessRequest);
            });

            OpenSettings = new RelayCommand(_ =>
            {
                var vm = new SettingsViewModel(_storage);
                var win = new SettingsWindow(window!, vm);
                vm.RequestClose += () => win.Close();
                win.ShowDialog();
                if (vm.Result is Success settingsSuc &&
                    (SettingsViewModel.Operations)settingsSuc.Data! ==
                    SettingsViewModel.Operations.LocalLogout) // wylogowanie
                {
                    SelectedServer = null;
                    Servers.Clear();
                    var loginRes = LocalLoginViewModel.ShowDialog(window!, _storage, _loggedUserKey, true);
                    if (!(loginRes is Success loginSuc))
                    {
                        ResetLists();
                        return;
                    }
                    var currentPassword = (SecureString)loginSuc.Data!;

                    _storage.SetLoggedLocalUser(false);

                    var encryptRes = ProgressBarViewModel.ShowDialog(window!,
                        "|Encrypting user's database.|", true,
                        (reporter) => _storage.EncryptLocalUser(ref reporter, _loggedUserKey,
                        currentPassword));
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
                    _loggedUserKey = default;
                    ShowLocalUsersDialog();
                }
            });

            _client.ServerIntroduced += OnServerIntroduced;
            _client.ServerHandshaken += OnServerHandshaken;
            _client.ReceivedConversationsAndUsersList += OnReceivedConversationsAndUsersList;
            _client.ServerEndedConnection += OnServerEndedConnection;
        }

        private void ShowLocalUsersDialog()
        {
            while (true)
            {
                var loginRes = LocalUsersViewModel.ShowDialog(window!, _storage);
                /* jeżeli użytkownik zamknął okno bez zalogowania się
                (nie ma innych możliwych wyników niż Success i Cancellation) */
                if (!(loginRes is Success loginSuc))
                {
                    Application.Current.Shutdown();
                    return;
                }
                // jeżeli użytkownik się zalogował, to vm.Result is Success
                var dat = (dynamic)loginSuc.Data!;
                var curPas = (SecureString)dat.Password;
                var user = (LocalUser)dat.LoggedUser;
                var userKey = user.GetPrimaryKey();

                var decryptRes = ProgressBarViewModel.ShowDialog(window!,
                    "|Decrypting user's database.|", true,
                    (reporter) => _storage.DecryptLocalUser(ref reporter, userKey, curPas));
                curPas.Dispose();
                if (decryptRes is Cancellation)
                    Alert("|User's database decryption canceled. Logging out.|");
                /* jeżeli decryptRes is Failure, to alert z błędem został już wyświetlony w
                ProgressBarViewModel.Worker_RunWorkerCompleted */
                else if (decryptRes is Success)
                {
                    _storage.SetLoggedLocalUser(true, userKey);
                    _loggedUserKey = userKey;
                    ResetLists();
                    return;
                }
            }
        }

        private void ResetLists()
        {
            Servers.Clear();
            try
            {
                var servers = _storage.GetAllServers(_loggedUserKey);
                for (int i = 0; i < servers.Count; ++i)
                    Servers.Add(servers[i]);
            }
            catch (Error e)
            {
                e.Prepend("|Could not| |read user's server list|.");
                Alert(e.Message);
                throw;
            }
        }

        #region Client events
        private void OnServerIntroduced(RemoteServer server)
        {
            // Wątek Client.Process
            /* Nie synchronizujemy, bo użytkownik może edytować tylko serwer,
            z którym nie jest połączony. Jeżeli chce edytować zaznaczony serwer,
            to jest z nim rozłączany przed otwarciem okna edycji serwera. */
            if ((!SelectedServer!.Guid.Equals(server.Guid)
                || !(SelectedServer.PublicKey is null))
                /* Już wcześniej ustawiono GUID lub klucz publiczny, więc aktualizujemy
                zgodnie z danymi od serwera, o ile użytkownik się zgodzi. Użytkownik
                musi szybko klikać, bo serwer liczy timeout, podczas którego czeka
                na przedstawienie się klienta. */
                && !AskIfServerTrusted(server.Guid!.Value, server.PublicKey!))
            {
                /* Serwer rozłączy klienta przez timeout, ale możemy
                też sami się rozłączyć za pomocą UIRequesta. */
                _client.Request(new Disconnect(server.GetPrimaryKey(), null));
                return;
            }

            /* Jeszcze nie ustawiono GUIDu i klucza publicznego lub
            były już ustawione, ale użytkownik zgodził się na
            aktualizację. */
            SelectedServer.Guid = server.Guid.Value;
            SelectedServer.PublicKey = server.PublicKey;
            _storage.UpdateServer(_loggedUserKey,
                SelectedServer.GetPrimaryKey(), SelectedServer);

            _client.Request(new IntroduceClient(server.GetPrimaryKey()));
        }

        private bool AskIfServerTrusted(Guid guid, PublicKey publicKey)
        {
            bool guidChanged = false;
            if (!guid.Equals(SelectedServer!.Guid))
                guidChanged = true;
            bool publicKeyChanged = false;
            if (!publicKey.Equals(SelectedServer.PublicKey))
                publicKeyChanged = true;

            // Nic się nie zmieniło.
            if (!guidChanged && !publicKeyChanged)
                return true;

            var sb = new StringBuilder("|Server changed its| ");
            if (guidChanged && publicKeyChanged)
                sb.Append("GUID |and| |public key|");
            else if (guidChanged)
                sb.Append("GUID");
            else // publicKeyChanged
                sb.Append("|public key|");

            return UIInvoke<bool>(() =>
            {
                var confirmRes = ConfirmationViewModel.ShowDialog(window!,
                    $"{sb}. |Do you still want to connect to the server|?",
                    "|Connect|", "|No|", "|Yes|");
                return confirmRes is Success;
            });
        }

        private void OnServerHandshaken(RemoteServer server)
        {
            // Wątek Client.Process
            // Jesteśmy po uścisku dłoni, więc pobieramy konwersacje z serwera.
            _client.Request(new GetConversations(server.GetPrimaryKey()));
            // Odpowiedź zamierzamy dostać w OnReceivedConversationsAndUsersList.
        }

        private void OnReceivedConversationsAndUsersList(RemoteServer server,
            Conversation[] conversations)
        {
            UIInvoke(() =>
            {
                ConversationVM.Conversation = null;
                Conversations.Clear();

                foreach (var c in conversations)
                    Conversations.Add(c);
            });
        }

        private void OnServerEndedConnection(RemoteServer server, string statusMsg)
        {
            // Wątek Client.Process
            UIInvoke(() =>
            {
                _selectedAccount = null;
                OnPropertyChanged(nameof(SelectedAccount));

                ConversationVM.Conversation = null;
                Conversations.Clear();

                Alert($"{server} {statusMsg}");
            });
        }
        #endregion
    }
}
