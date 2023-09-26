using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using Server.MVVM.ViewModel.Observables;
using System;
using Shared.MVVM.Model.Cryptography;
using Server.MVVM.Model.Persistence;
using Shared.MVVM.Model.Networking;
using static Server.MVVM.Model.Client;
using Server.MVVM.Model.Persistence.DTO;

namespace Server.MVVM.Model
{
    /* Centralna klasa, która synchronizuje stan serwera,
    czyli klientów, Storage i viewmodeli. */
    public class Server
    {
        #region Properties
        public bool IsRunning { get; private set; } = false;
        #endregion

        #region Fields
        private TcpListener _listener = null;
        private Task<Result> _runner = null;
        private volatile bool _stopRequested = false;

        private Guid _guid = Guid.Empty;
        private PrivateKey _privateKey = null;
        private byte[] _publicKeyBytes = null;
        private int _capacity = 0;
        private List<Client> _clients = new List<Client>();

        private readonly Storage _storage;
        private readonly Log _log;
        private readonly ReaderWriterLockSlim _syncRoot =
            new ReaderWriterLockSlim();
        #endregion

        #region Events
        public event Action<Client> ClientConnected;
        public event Action<Client> ClientAuthenticated;
        public event Action<Client, Result> ClientEndedConnection;
        public event Action<Result> Stopped;
        #endregion

        public Server(Log log)
        {
            _storage = new Storage();
            _log = log;
        }

        public void Start(Guid guid, PrivateKey privateKey,
            IPv4Address ipAddress, Port port, int capacity)
        {
            try
            {
                var localEndPoint = new IPEndPoint(ipAddress.ToIPAddress(), port.Value);
                _listener = new TcpListener(localEndPoint);
                _listener.Start();

                _guid = guid;
                _privateKey = privateKey;
                /* Memoizujemy, bo obliczanie klucza publicznego jest kosztowne.
                TODO: memoizacja bajtowych reprezentacji, np. zrobić obiekt w
                klasie ProtocolDispatcher o nazwie Memoized i w nim trzymać zmienne
                obliczone przy starcie serwera. */
                _publicKeyBytes = privateKey.ToPublicKey().ToBytes();
                _capacity = capacity;

                _stopRequested = false;
                IsRunning = true;
                // Uruchamiamy wątek Server.Process
                _runner = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
            }
            catch (SocketException se)
            {
                _listener.Stop();
                IsRunning = false;
                throw new Error(se, "|Error occured while| |starting the server|.");
            }
        }

        private Result Process()
        {
            Result result = null;
            try
            {
                while (true)
                {
                    if (_stopRequested) break;
                    // https://stackoverflow.com/a/365533
                    if (!_listener.Pending())
                    {
                        Thread.Sleep(500); // choose a number (in milliseconds) that makes sense
                        continue; // skip to next iteration of loop
                    }

                    AcceptClient();
                }
                DisconnectAllClients();
                result = new Success();
            }
            /* nie łapiemy InvalidOperationException, bo _listener.AcceptTcpClient()
            może je wyrzucić tylko jeżeli nie wywołaliśmy wcześniej _listener.Start() */
            catch (SocketException se)
            {
                /* według dokumentacji funkcji TcpListener.AcceptTcpClient,
                se.ErrorCode jest kodem błędu, którego opis można zobaczyć
                w "Windows Sockets version 2 API error code documentation" */
                result = new Failure(se, $"|{se.Message}|");
            }
            finally
            {
                _clients.Clear();
                _listener.Stop();
                IsRunning = false;
                /* jeżeli nie ma żadnych obserwatorów (nikt nie ustawił callbacków
                (handlerów)) i Stopped == null, to Invoke się nie wykona */
            }
            return result;
        }

        public void Log(string text) => _log.Append(text);

        #region Errors
        private Error UnexpectedReceptionError() =>
            new Error("|Received an unexpected packet| |from client|.");

        private Error ReceptionTimedOut() =>
            new Error("|Receiving a packet from the server timed out|.");
        #endregion

        private void AcceptClient()
        {
            // Wątek Server.Process
            var client = new Client(_listener.AcceptTcpClient());
            client.EndedConnection += (result) =>
            {
                // Wątek Client.Process
                try
                {
                    _syncRoot.EnterWriteLock();
                    /* Wchodzimy w write locka i kaskadowo wykonujemy event
                    Client.EndedConnection. */
                    // TODO: trzymać klientów w mapie, żeby przyspieszyć usuwanie
                    _clients.Remove(client);
                    ClientEndedConnection(client, result);
                }
                finally { _syncRoot.ExitWriteLock(); }
            };
            StartProcessingClient(client);
        }
        
        private void StartProcessingClient(Client client)
        {
            // Wątek Server.Process
            /* StartProcessing jest potrzebne, bo jeżeli startujemy wątek
            Client.Process przed ustawieniem handlera EndedConnection, to
            klient może rozłączyć się przed ustawieniem tego handlera i
            wtedy na liście _clients pozostanie klient zombie, który nie
            zostanie usunięty. */
            client.StartProcessing(() =>
            {
                try
                {
                    // Wątek Client.ProcessProtocol
                    bool shouldSendNoSlots = false;
                    try
                    {
                        _syncRoot.EnterWriteLock();
                        /* Klient tu dodany zostanie usunięty z listy _clients
                        w handlerze EndedConnection. */
                        _clients.Add(client);

                        // Nie ma wolnych slotów.
                        if (_clients.Count >= _capacity + 1)
                            /* Tylko ustawiamy flagę i jak najszybciej zwalniamy write locka.
                            Aktualny wątek (Client.ProcessProtocol na razie nie oczekuje
                            na pakiety ani nie dispatchuje żądań od wątku UI, więc nie będzie
                            żadnego wyścigu i if (shouldSendNoSlots) wykina się bez problemów. */
                            shouldSendNoSlots = true;
                        else // Są wolne sloty.
                            ClientConnected(client);
                    }
                    finally { _syncRoot.ExitWriteLock(); }

                    if (shouldSendNoSlots)
                    {
                        SendNoSlots(client);
                        // TODO: log
                        client.DisconnectAsync();
                        // Nie wystąpił błąd, więc zwracamy Success.
                        return new Success();
                    }

                    /* Można tu użyć client.StopRequested, aby przedwcześnie przerwać protokół
                    lub client.DisconnectAsync, aby rozłączyć klienta. */
                    SendServerIntroduction(client);
                    ReceiveClientIntroduction(client);

                    while (true)
                    {
                        if (client.StopRequested)
                            break;
                    }

                    return new Success();
                    /* Return z tej funkcji anonimowej powoduje rozłączenie klienta, bo wątek
                    Client.ProcessProtocol przy wychodzeniu z niej wywoła Client.StopProcessing. */
                }
                catch (IndexOutOfRangeException e)
                {
                    return new Failure(e, "|Received an incomplete packet|.");
                }
                catch (Error e)
                {
                    return new Failure(e, e.Message);
                }
            });
        }

        public void SendNoSlots(Client client)
        {
            // Wątek Client.ProcessProtocol; write lock
            // 0 Brak wolnych połączeń (slotów) (00)
            var pb = new PacketBuilder();
            // Kod operacji
            pb.Prepend(0, 1);
            // Pakiet nieszyfrowany i nieautentykowany
            /* Client.ProcessSend blokuje aktualny wątek i odblokowuje go
            dopiero po wysłaniu pakietu. */
            client.Send(pb.Build());
        }

        public void SendServerIntroduction(Client client)
        {
            // Wątek Server.ProcessProtocol; write lock
            byte[] token = RandomGenerator.Generate(256);
            client.TokenCache = token;

            // 1 Przedstawienie się serwera (00)
            var pb = new PacketBuilder();
            pb.Append(_guid.ToByteArray());
            pb.Append(_publicKeyBytes);
            pb.Append(token);
            pb.Prepend(1, 1);
            // Pakiet nieszyfrowany i nieautentykowany
            client.Send(pb.Build());
        }

        private void ReceiveClientIntroduction(Client client)
        {
            // Wątek Client.ProcessProtocol
            if (!client.Receive(out byte[] packet, 1000))
                throw ReceptionTimedOut();

            var reader = new PacketReader(packet);
            if (reader.ReadUInt8() != 255)
                throw UnexpectedReceptionError();

            reader.Decrypt(_privateKey);

            ushort loginLength = reader.ReadUInt8();
            string login = reader.ReadUtf8String(loginLength);
            PublicKey publicKey = PublicKey.FromPacketReader(reader);
            bool publicKeyBelongsToUser = reader.VerifySignature(publicKey, client.TokenCache);
            byte[] nextPacketToken = reader.ReadBytes(8);

            Action outsideLock;
            try
            {
                _syncRoot.EnterWriteLock();
                var usersDb = _storage.Database.Users;
                if (usersDb.UserExists(login))
                {
                    // Login już istnieje.
                    var user = usersDb.GetUser(login);
                    if (!publicKey.Equals(user.PublicKey))
                        // Klient wysłał inny klucz publiczny niż serwer ma zapisany w bazie.
                        outsideLock = () =>
                        {
                            // TODO: log
                            SendNoAuthentication(client, nextPacketToken);
                            throw new Error("|Client| |not authenticated| |because| " +
                                "|it sent a public key different from the one saved in the database|.");
                        };

                    if (!publicKeyBelongsToUser)
                        // Klient nie zna klucza prywatnego.
                        outsideLock = () =>
                        {
                            SendNoAuthentication(client, nextPacketToken);
                            throw new Error("|Client| |not authenticated| |because| " +
                                "|it does not own the public key sent by it|.");
                        };
                }
                else
                    // Login jeszcze nie istnieje, więc zapisujemy go w bazie.
                    usersDb.AddUser(new UserDTO { Login = login, PublicKey = publicKey });

                /* Login nie istnieje lub istnieje i klient podpisał token swoim kluczem
                prywatnym powiązanym z kluczem publicznym publicKey. Zapisujemy dane
                (kredki) klienta na potrzeby dalszego kontynuowania jego sesji. */
                client.Authenticate(login, publicKey);
                ClientAuthenticated(client);
                outsideLock = () => SendAuthentication(client, nextPacketToken);
            }
            finally { _syncRoot.ExitWriteLock(); }
            outsideLock();
        }

        private void SendAuthentication(Client client, byte[] nextPacketToken)
        {
            // Wątek Client.ProcessProtocol; write lock
            client.TokenCache = RandomGenerator.Generate(8);

            // 2 Rejestracja konta i/lub autentykacja klienta (11)
            var pb = new PacketBuilder();
            pb.Append(nextPacketToken);
            pb.Append(client.TokenCache);
            // Pakiet szyfrowany i autentykowany
            pb.Sign(_privateKey);
            pb.Encrypt(client.PublicKey);
            pb.Prepend(2, 1);
            client.Send(pb.Build());
        }

        private void SendNoAuthentication(Client client, byte[] nextPacketToken)
        {
            // Wątek Client.ProcessProtocol; write lock
            var pb = new PacketBuilder();
            pb.AppendSignature(_privateKey, nextPacketToken);
            // pb.Sign(_privateKey); - nie podpisujemy, bo wysyłamy sam podpis bez danych
            pb.Prepend(3, 1);
            client.Send(pb.Build());
        }

        public void DisconnectClientAsync(ClientPrimaryKey key)
        {
            // Wątek UI
            try
            {
                _syncRoot.EnterWriteLock();
                var index = _clients.FindIndex(c => c.GetPrimaryKey().Equals(key));

                /* Jeżeli już nie ma klienta, to nic nie robimy, bo już się rozłączył
                z własnej inicjatywy - wykonany został handler EndedConnection. */
                if (index == -1)
                    return;

                /* Gdy klient jest, to requestujemy jego rozłączenie, a wątek
                Client.Process wykona handler EndedConnection. */
                _clients[index].DisconnectAsync();
            }
            finally { _syncRoot.ExitWriteLock(); }
        }

        public void RequestStop()
        {
            _stopRequested = true;
        }

        /* Synchroniczne zatrzymanie z wykonaniem kodu obsługi zatrzymania serwera
        (event Stopped). */
        public void Stop()
        {
            // Wątek UI
            if (!IsRunning)
                throw new Error("|Server is not running.|");

            RequestStop();
            // czekamy na zakończenie wątku (taska) serwera
            _runner.Wait();

            Stopped(_runner.Result);
            /* Nie trzeba wchodzić w locka, bo klienci zostali już rozłączeni
            i serwer już nie działa. */
            Log("|Server stopped|.");
        }

        private void DisconnectAllClients()
        {
            // zbieramy wątki (taski) obsługujące wszystkich klientów
            var clientRunners = new LinkedList<Task>();
            foreach (Client c in _clients)
                clientRunners.AddLast(c.DisconnectAsync());

            // czekamy na zakończenie wątków obsługujących wszystkich klientów
            Task.WhenAll(clientRunners);
        }
    }
}
