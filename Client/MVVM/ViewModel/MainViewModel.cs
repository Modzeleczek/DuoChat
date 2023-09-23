using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.AccountActions;
using Client.MVVM.ViewModel.LocalUsers;
using Client.MVVM.ViewModel.Observables;
using Client.MVVM.ViewModel.ServerActions;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.View.Localization;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Client.MVVM.Model.Client;

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
            /* Asynchronicznie rozłączamy z serwerem (poprzez
            SelectedAccount = null). */
            set { SelectServerAsync(value); }
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
            set { SelectAccountAsync(value); }
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
        private LocalUserPrimaryKey _loggedUserKey;
        private readonly Model.Client _client = new Model.Client();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Random rng = new Random();
        private Storage _storage;
        #endregion

        public MainViewModel()
        {
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (DialogWindow)windowLoadedE;
                // zapobiega ALT + F4 w głównym oknie
                window.Closable = false;

                // Do potomnych okien przekazujemy na zasadzie dependency injection.
                try { _storage = new Storage(); }
                catch (Error e)
                {
                    Alert(e.Message);
                    throw;
                }

                Servers = new ObservableCollection<Server>();
                Accounts = new ObservableCollection<Account>();
                Conversations = new ObservableCollection<Conversation>();

                try
                {
                    d.ActiveLanguage = (Translator.Language)_storage.GetActiveLanguage();
                }
                catch (Error e)
                {
                    Alert(e.Message);
                    // przekazanie dalej wyjątku spowoduje zamknięcie programu
                    throw;
                }

                try
                {
                    ((App)Application.Current).ActiveTheme = (App.Theme)_storage.GetActiveTheme();
                }
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
                new FormWindow(window, vm).ShowDialog();
                var result = vm.Result;
                if (result is Success success)
                    Servers.Add((Server)success.Data);
            });
            EditServer = new RelayCommand(obj =>
            {
                var server = (Server)obj;
                /* Synchronicznie rozłączamy i odznaczamy
                aktualnie wybrany serwer. */
                if (SelectedServer == server)
                    /* Nie można waitować wątkiem UI, bo czeka on na zakończenie kodu z
                    wewnątrz await _client.DisconnectAsync().ContinueWith, a jednocześnie
                    ten kod chce wywołać UIInvoke, co powoduje deadlock. */
                    SelectServerAsync(null);

                var vm = new EditServerViewModel(_storage,
                    _loggedUserKey, server.GetPrimaryKey())
                {
                    Title = "|Edit server|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
                if (vm.Result is Success success)
                {
                    var updatedServer = (Server)success.Data;
                    updatedServer.CopyTo(server);
                }
            });
            DeleteServer = new RelayCommand(obj =>
            {
                var server = (Server)obj;

                var serverKey = server.GetPrimaryKey();
                var confirmRes = ConfirmationViewModel.ShowDialog(window,
                    "|Do you want to delete| |server|" +
                    $" {serverKey.ToString("{0}:{1}")}?",
                    "|Delete server|", "|No|", "|Yes|");
                if (!(confirmRes is Success)) return;

                if (SelectedServer == server)
                    SelectServerAsync(null);

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
                    SelectedServer.GetPrimaryKey())
                {
                    Title = "|Add_account|",
                    ConfirmButtonText = "|Add|"
                };
                new FormWindow(window, vm).ShowDialog();
                if (vm.Result is Success success)
                    Accounts.Add((Account)success.Data);
            });
            EditAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj;
                if (SelectedAccount == account)
                    SelectAccountAsync(null);

                var vm = new EditAccountViewModel(_storage, _loggedUserKey,
                    SelectedServer.GetPrimaryKey(), account.Login)
                {
                    Title = "|Edit account|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
                if (vm.Result is Success success)
                {
                    var updatedAccount = (Account)success.Data;
                    updatedAccount.CopyTo(account);
                }
            });
            DeleteAccount = new RelayCommand(obj =>
            {
                var account = (Account)obj;
                var confirmRes = ConfirmationViewModel.ShowDialog(window,
                    "|Do you want to delete| |account|" + $" {account.Login}?",
                    "|Delete account|", "|No|", "|Yes|");
                if (!(confirmRes is Success)) return;

                if (SelectedAccount == account)
                    SelectAccountAsync(null);
                
                try
                {
                    _storage.DeleteAccount(_loggedUserKey,
                        SelectedServer.GetPrimaryKey(), account.Login);
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

            Send = new RelayCommand(o =>
            {
                if (SelectedConversation == null ||
                    WrittenMessage.Length == 0) return;
                var rnd = Message.Random(rng);
                rnd.PlainContent = WrittenMessage;
                SelectedConversation.Messages.Add(rnd);
                WrittenMessage = "";
                /* TODO: pisanie do wybranej konwersacji
                na wybranym koncie na wybranym serwerze */
            });

            Close = new RelayCommand(_ =>
            {
                window.Closable = true;
                // zamknięcie MainWindow powoduje zakończenie programu
                window.Close();
            });

            OpenSettings = new RelayCommand(_ =>
            {
                var vm = new SettingsViewModel(_storage);
                var win = new SettingsWindow(window, vm);
                vm.RequestClose += () => win.Close();
                win.ShowDialog();
                if (vm.Result is Success settingsSuc &&
                    (SettingsViewModel.Operations)settingsSuc.Data ==
                    SettingsViewModel.Operations.LocalLogout) // wylogowanie
                {
                    ClearLists();
                    var loginRes = LocalLoginViewModel.ShowDialog(window, _storage, _loggedUserKey, true);
                    if (!(loginRes is Success loginSuc))
                    {
                        ResetLists();
                        return;
                    }
                    var currentPassword = (SecureString)loginSuc.Data;

                    _storage.SetLoggedLocalUser(false);

                    var encryptRes = ProgressBarViewModel.ShowDialog(window,
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

            SetClientEventHandlers();
        }

        private void ShowLocalUsersDialog()
        {
            while (true)
            {
                var loginRes = LocalUsersViewModel.ShowDialog(window, _storage);
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
                var userKey = user.GetPrimaryKey();

                var decryptRes = ProgressBarViewModel.ShowDialog(window,
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

        private void ClearLists()
        {
            SelectServerAsync(null);
            Servers.Clear();
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

        private async Task SelectServerAsync(Server server)
        {
            selectedServer = server;
            await SelectAccountAsync(null);
            Accounts.Clear();
            if (server != null)
            {
                try
                {
                    var accounts = _storage.GetAllAccounts(
                        _loggedUserKey, server.GetPrimaryKey());
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
            OnPropertyChanged(nameof(SelectedServer));
        }

        private async Task SelectAccountAsync(Account account)
        {
            /* Asynchronicznie rozłączamy z serwerem. Wyłączamy tymczasowo
            interakcje z głównym oknem, ale nie blokujemy wątku UI,
            dzięki czemu, wątek Client.ProcessHandle może jeszcze przed
            rozłączeniem obsłużyć jakiś pakiet i w handlerach eventu
            Client.ReceivedPacket wykonywać UIInvoke i edytować GUI. */

            // Wyłączamy wszelkie interakcje z oknem.
            window.SetEnabled(false);

            /*  - Jeżeli już jesteśmy połączeni, to żądamy rozłączenia.
            - Jeżeli jeszcze się nie łączyliśmy, to kod z ContinueWith zostanie
            natychmiast wykonany, bo implementacja Client klienta w konstruktorze
            ustawia _runner = Task.CompletedTask.
            - Jeżeli byliśmy połączeni, ale nie jesteśmy, to też kod z ContinueWith
            zostanie natychmiast wykonany. */
            await _client.DisconnectAsync().ContinueWith((_) =>
            {
                /* Ten kod jest wykonywany przez jakiś anonimowy wątek
                (nie jest to wątek Client.Process) po zakończeniu metody
                Client.Process przez wątek Client.Process. Anonimowy wątek
                jest zablokowany do czasu zakończenia UIInvoke. */
                UIInvoke(() =>
                {
                    /* Wątek UI na zlecenie anonimowego wątku
                    Nie sprawdzamy, czy value == SelectedServer, aby można było
                    reconnectować poprzez kliknięcie na już zaznaczony serwer. */
                    selectedAccount = account;
                    SelectedConversation = null;
                    Conversations.Clear();
                    if (account != null)
                    {
                        try
                        {
                            // Synchroniczne łączenie.
                            var serverKey = SelectedServer.GetPrimaryKey();
                            _client.Connect(serverKey);
                            _client.State = ClientState.Connected;

                            // RANDOM: TODO: pobieranie konwersacji z serwera
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
                    OnPropertyChanged(nameof(SelectedAccount));

                    // Przywracamy interakcje z oknem.
                    window.SetEnabled(true);
                });
            });
        }

        private void SetClientEventHandlers()
        {
            _client.EndedConnection += EndedConnection;
            _client.ReceivedPacket += ReceivedOrder;
            /* Wątek Client.Process. Bez locków, bo w momencie
            wywołania OnDisconnected, wątki obsługujące serwer
            (Client.ProcessSend, -Receive, -Handle) są już zakończone
            i jedyny, który może zmienić stan aplikacji (za
            pomocą metod Storage), to wątek UI. */
        }

        #region Errors
        private Error UnexpectedReceptionError() =>
            new Error("|Received an unexpected packet| |from server|.");

        private Error UnexpectedSendingError() =>
            new Error("|Tried to send an unexpected packet|.");

        private Error UnrecognizedTokenError() =>
            new Error("|Server sent an unrecognized token|.");
        #endregion

        private void ReceivedOrder(byte[] packet)
        {
            try
            {
                var reader = new PacketReader(packet);
                switch (reader.ReadUInt8())
                {
                    case 0: HandleNoSlots(); break;
                    case 1: HandleServerIntroduction(reader); break;
                    case 2: HandleAuthentication(reader); break;
                    case 3: HandleNoAuthentication(reader); break;
                    /* Client.ProcessHandle łapie wyjątek i kończy się ze
                    statusem Failure, który w EndedConnection zostanie
                    zinterpretowany jako błąd klienta. */
                    default: throw UnexpectedReceptionError();
                }
            }
            catch (IndexOutOfRangeException)
            {
                /* Nie ma sensu robić bardzo szczegółowego opisu
                błędów spowodowanych niekompletnym (za krótkim)
                pakietem, bo zakładamy, że użytkownicy nie będą
                za często fabrykować pakietów. Wystarczy prosty
                opis błędu. */
                throw new Error("|Received an incomplete packet|.");
            }
        }

        private void HandleNoSlots()
        {
            if (_client.State != ClientState.Connected)
                throw UnexpectedReceptionError();

            /* Wykonywane przez wątek Client.ProcessHandle - ten wątek jest
            zablokowany przez wywołanie UIInvoke do czasu zamknięcia
            alertu przyciskiem OK przez użytkownika. Zatem również
            wątek Client.Process czeka na zakończenie wątku Client.Handle,
            czyli event LostConnection nie zostanie wykonany przed
            zamknięciem alertu. */
            UIInvoke(() => Alert("|Server is full|."));
            // Serwer rozłączy klienta.
        }

        private void HandleServerIntroduction(PacketReader reader)
        {
            // Wątek Client.ProcessHandle
            if (_client.State != ClientState.Connected)
                throw UnexpectedReceptionError();

            Guid guid = reader.ReadGuid();
            PublicKey publicKey = PublicKey.FromPacketReader(reader);
            byte[] token = reader.ReadBytes(256);

            /* Nie synchronizujemy, bo użytkownik może edytować tylko serwer,
            z którym nie jest połączony. Jeżeli chce edytować zaznaczony serwer,
            to jest z nim rozłączany przed otwarciem okna edycji serwera. */
            if (!SelectedServer.Guid.Equals(Guid.Empty)
                || !(SelectedServer.PublicKey is null))
            {
                /* Już wcześniej ustawiono GUID lub klucz publiczny,
                więc aktualizujemy zgodnie z danymi od serwera, o ile
                użytkownik się zgodzi. */
                if (!AskIfServerTrusted(guid, publicKey))
                    // W AskIfServerTrusted rozłączyliśmy się z serwerem.
                    return;
            }
            
            /* Jeszcze nie ustawiono GUIDu i klucza publicznego lub
            były już ustawione, ale użytkownik zgodził się na
            aktualizację. */
            SelectedServer.Guid = guid;
            SelectedServer.PublicKey = publicKey;
            _storage.UpdateServer(_loggedUserKey,
                SelectedServer.GetPrimaryKey(), SelectedServer);

            _client.State = ClientState.ServerIntroduced;
            SendClientIntroduction(token);
        }

        private bool AskIfServerTrusted(Guid guid, PublicKey publicKey)
        {
            bool guidChanged = false;
            if (!guid.Equals(SelectedServer.Guid))
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
                var confirmRes = ConfirmationViewModel.ShowDialog(window,
                    $"{sb}. |Do you still want to connect to the server|?",
                    "|Connect|", "|No|", "|Yes|");
                if (!(confirmRes is Success))
                {
                    // Rozłączamy z serwerem.
                    SelectAccountAsync(null);
                    return false;
                }
                return true;
            });
        }

        private void SendClientIntroduction(byte[] tokenToSign)
        {
            // Wątek Client.ProcessHandle
            if (_client.State != ClientState.ServerIntroduced)
                throw UnexpectedSendingError();

            // 255 Przedstawienie się klienta (10)
            byte[] loginBytes = Encoding.UTF8.GetBytes(SelectedAccount.Login);
            // Długość zserializowanego loginu musi mieścić się na 1 bajcie.
            if (loginBytes.Length > 255)
                throw new Error("|UTF-8 encoded login can be at most 255 bytes long|.");

            PublicKey publicKey = SelectedAccount.PrivateKey.ToPublicKey();
            if (publicKey.Length > 256)
                throw new Error("|Public key can be at most 256 bytes long|.");
            byte[] publicKeyBytes = publicKey.ToBytes();

            _client.TokenCache = RandomGenerator.Generate(8);

            var pb = new PacketBuilder();
            // pb.AppendTimestamp();
            pb.Append(loginBytes.Length, 1);
            pb.Append(loginBytes);
            /* Długość klucza znajduje się już w bajtach
            zwróconych przez PublicKey.ToBytes(). */
            pb.Append(publicKeyBytes);
            pb.AppendSignature(SelectedAccount.PrivateKey, tokenToSign);
            pb.Append(_client.TokenCache);
            pb.Encrypt(SelectedServer.PublicKey);
            pb.Prepend(255, 1);
            _client.Send(pb.Build());

            _client.State = ClientState.ClientIntroduced;
        }

        private void HandleAuthentication(PacketReader reader)
        {
            // Wątek Client.ProcessHandle
            if (_client.State != ClientState.ClientIntroduced)
                throw UnexpectedReceptionError();

            reader.Decrypt(SelectedAccount.PrivateKey);
            if (!reader.VerifySignature(selectedServer.PublicKey))
                throw new Error("|Could not| |verify server's signature|.");

            byte[] receivedToken = reader.ReadBytes(8);
            if (!receivedToken.SequenceEqual(_client.TokenCache))
                throw UnrecognizedTokenError();

            _client.TokenCache = reader.ReadBytes(8);

            _client.State = ClientState.ClientAuthenticated;
        }

        private void HandleNoAuthentication(PacketReader reader)
        {
            // Wątek Client.ProcessHandle
            if (_client.State != ClientState.ClientIntroduced)
                throw UnexpectedReceptionError();

            if (!reader.VerifySignature(SelectedServer.PublicKey, _client.TokenCache))
                throw UnrecognizedTokenError();

            // Serwer rozłączy klienta.
        }

        private void EndedConnection(Result result)
        {
            // Wątek Client.Process
            _client.State = ClientState.EndedConnection;

            /* Jeżeli my (klient) się rozłączamy, czyli
            _disconnectRequested == true, to w Client.Process
            po Task.WaitAll nie wykona się SpecifyConnectionEnding
            i result jest typu Cancellation. */
            string message;
            if (result is Success)
                message = "|Disconnected by server|.";
            else if (result is Failure fail)
            {
                var reason = fail.Reason;
                string failMsg;
                if (fail is InterlocutorFailure)
                    failMsg = "|Server| |crashed|.";
                else // result is Failure
                    failMsg = "|Disconnected due to a client error|.";
                message = reason.Prepend(failMsg).Message;
            }
            else // result is Cancellation
                message = "|Disconnected|.";

            UIInvoke(() =>
            {
                /* Task zwrócony przez _client.DisconnectAsync
                jest już zakończony, więc natychmiast wykonywane
                jest ContinueWith. Nie czekamy na zakończenie kodu
                z ContinueWith, bo będzie deadlock. Zanim użytkownik
                kliknie alert, operacje z SelectAccountAsync się
                zakończą. */
                SelectAccountAsync(null);
                Alert(message);
            });
        }
    }
}
