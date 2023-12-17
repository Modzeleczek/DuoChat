using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Shared.MVVM.Core;
using Server.MVVM.Model.Persistence;
using System.Threading;
using Server.MVVM.Model.Persistence.DTO;
using System.Linq;
using System.Collections.Concurrent;
using Server.MVVM.Model.Networking.PacketOrders;
using Shared.MVVM.Model.Networking.Packets.ServerToClient;
using Shared.MVVM.Model.Networking.Packets;
using Shared.MVVM.Model.Networking.Packets.ClientToServer;
using Server.MVVM.Model.Networking.UIRequests;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using System.Reflection;
using System.Diagnostics;

namespace Server.MVVM.Model.Networking
{
    // Klasa odpowiada za cały stan serwera i zarządzanie połączonymi klientami.
    public class ServerMonolith : IEventProcessor
    {
        #region Fields
        // Usługi
        private readonly Storage _storage;

        // Dane lokalnego hosta
        private Guid _guid = Guid.Empty;
        private PrivateKey? _privateKey = null;
        private byte[]? _publicKeyBytes = null;
        private int _capacity = 0;

        // Zdalne hosty
        private readonly Dictionary<ClientPrimaryKey, Client> _clients =
            new Dictionary<ClientPrimaryKey, Client>();

        // Do przebiegu sterowania
        private TcpListener? _listener = null;
        private readonly Task _serverProcessTask;
        private bool _stopRequested = false;
        private bool _isAcceptingClients = false;
        private readonly BlockingCollection<ClientEvent> _eventQueue =
            new BlockingCollection<ClientEvent>();
        // Wczesna (eager) inicjalizacja
        private CancellationTokenSource _eventQueueWaitBreaker = new CancellationTokenSource();
        private UIRequest? _uiRequest = null;
        #endregion

        #region Events
        public event Action<Client>? ClientConnected;
        public event Action<Client>? ClientHandshaken;
        public event Action<Client, string>? ClientEndedConnection;
        public event Action<IPv4Address>? IPBlocked;
        public event Action<IPv4Address>? IPUnblocked;
        #endregion

        public ServerMonolith(Storage storage)
        {
            _storage = storage;

            _serverProcessTask = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
        }

        private void Process()
        {
            // Wątek Server.Process
            while (true)
            {
                /* Nie używamy monitor locka, bo _stopRequested może zmienić
                wartość tylko w HandleUIRequest w tym samym wątku (Server.Process). */
                if (_stopRequested)
                    break;

                // Akceptujemy jednego klienta lub odrzucamy, jeżeli nie ma miejsca.
                if (_isAcceptingClients)
                    AcceptPendingClientIfAny();

                /* Obsługujemy jedno zdarzenie klienta. Jeżeli w kolejce jest więcej
                zdarzeń, to pętla kręci się i bez spania po kolei je obsługuje. */
                try
                {
                    /* 1. Kiedy osiągniemy timeout, to TryTake zwraca sterowanie
                    i zwraca false.
                    2. Kiedy do kolejki ktoś (inny wątek) doda zdarzenie, to
                    TryTake zwraca sterowanie i zwraca true.
                    3. Kiedy ktoś zcanceluje CancellationToken, to TryTake
                    zwraca sterowanie i wyrzuca OperationCanceledException. */
                    if (_eventQueue.TryTake(out ClientEvent? @event, 500,
                        _eventQueueWaitBreaker.Token))
                        HandleClientEvent(@event);
                }
                catch (OperationCanceledException)
                {
                    /* Wątek UI canceluje _eventQueueWaitBreaker.Token, kiedy
                    chce wysłać żądanie do wątku Server.Process. */

                    /* Jeżeli token został zcancelowany przez wątek UI zlecający żądanie,
                    to tworzymy nowy token, bo próba wywołania _receiveQueue.TryTake z już
                    zcancelowanym tokenem od razu wyrzuci OperationCanceledException. */
                    _eventQueueWaitBreaker = new CancellationTokenSource();

                    HandleUIRequest();
                }
            }
        }

        public void Enqueue(ClientEvent @event)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}");

            _eventQueue.Add(@event);
        }

        private void AcceptPendingClientIfAny()
        {
            // Wątek Server.Process
            if (!_listener!.Pending())
                return;

            // Akceptujemy nowego klienta.
            var client = new Client(_listener.AcceptTcpClient(), this);
            client.StartSenderAndReceiver();

            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            /* Do klienta jeszcze lub już nie możemy wysyłać powiadomień, bo jest
            w trakcie uścisku dłoni, rozłączania lub już został rozłączony. */
            client.IsNotifiable = false;
            client.IgnoreEvents = false;

            _clients.Add(client.GetPrimaryKey(), client);
            ClientConnected?.Invoke(client);

            if (_clients.Count >= _capacity)
            {
                // Nie ma wolnych miejsc.
                /* EnqueueToSend nie zablokuje wywołującego wątku, bo do konstruktora
                _sendQueue nie przekazaliśmy BoundedCapacity, czyli kolejka
                może mieć nieograniczoną liczbę elementów. */
                client.EnqueueToSend(NoSlots.Serialize(), NoSlots.CODE);
                return;
            }

            if (_storage.Database.ClientIPBlocks.Exists(
                client.GetPrimaryKey().IpAddress.BinaryRepresentation))
            {
                // Klient ma zablokowany adres IP.
                client.EnqueueToSend(IPAlreadyBlocked.Serialize(), IPAlreadyBlocked.CODE,
                    "|tried to connect from blocked IP address|.");
                return;
            }

            // Są wolne miejsca.
            client.VerificationToken = RandomUInt64();
            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.ClientIntroduction, 10000);
            client.EnqueueToSend(ServerIntroduction.Serialize(_guid, _publicKeyBytes!,
                client.VerificationToken), ServerIntroduction.CODE);
        }

        private ulong RandomUInt64()
        {
            return BitConverter.ToUInt64(RandomGenerator.Generate(Packet.TOKEN_SIZE));
        }

        private void HandleClientEvent(ClientEvent @event)
        {
            Client client = @event.Sender;
            Debug.WriteLine($"{nameof(HandleClientEvent)}, {@event.ToDebugString()}");

            if (client.IgnoreEvents)
                return;

            switch (@event.Type)
            {
                case ClientEvent.Types.SendSuccess:
                    OnSendSuccess(@event);
                    break;
                case ClientEvent.Types.SendError:
                    DisconnectThenNotify(client,
                        $"|sending error|; {((Exception)@event.Data!).Message}.");
                    break;
                case ClientEvent.Types.SendTimeout:
                    DisconnectThenNotify(client, "|timed out sending packet|.");
                    break;

                case ClientEvent.Types.ClientClosedSocket:
                    DisconnectThenNotify(client, "|disconnected (closed its socket)|.");
                    break;
                case ClientEvent.Types.ReceiveSuccess:
                    OnReceiveSuccess(@event);
                    break;
                case ClientEvent.Types.ReceiveError:
                    DisconnectThenNotify(client,
                        $"|reception error|; {((Exception)@event.Data!).Message}.");
                    break;
                case ClientEvent.Types.ReceiveTimeout:
                    OnReceiveTimeout(@event);
                    break;
            }
        }

        private void DisconnectThenNotify(Client client, string errorMsg)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}, {errorMsg}");

            // DisconnectThenRemoveClient
            client.IsNotifiable = false;
            client.IgnoreEvents = true;
            var clientKey = client.GetPrimaryKey();
            client.Disconnect();
            /* Klient już nie musi znajdować się na liście,
            ale jeżeli jest, to go usuwamy. */
            _clients.Remove(clientKey);

            ClientEndedConnection?.Invoke(client, errorMsg);
        }

        private void OnSendSuccess(ClientEvent @event)
        {
            Client client = @event.Sender;
            // var clientKey = client.GetPrimaryKey()

            var (code, reason) = ((Packet.Codes, string))@event.Data!;
            // Log($"Sent {operation} to client {clientKey}.")

            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}, {code}, {reason}");

            switch (code)
            {
                case Packet.Codes.NoSlots:
                case Packet.Codes.IPAlreadyBlocked:
                case Packet.Codes.NoAuthentication:
                case Packet.Codes.AccountAlreadyBlocked:
                    DisconnectThenNotify(client, $"{code}: {reason}");
                    break;
                case Packet.Codes.IPNowBlocked:
                    DisconnectThenNotify(client, "|IP address is now blocked|.");
                    break;
            }
        }

        private void OnReceiveSuccess(ClientEvent @event)
        {
            Client client = @event.Sender;
            byte[] packet = (byte[])@event.Data!;

            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}, {packet.ToHexString()}");

            var receiveOrder = client.ReceiveOrder;
            if (receiveOrder is null)
            {
                // Nie oczekujemy żadnego pakietu.
                DisconnectThenNotify(client, "|sent a packet although| |server| |did not expect any|.");
                return;
            }

            var expectedPacket = receiveOrder.ExpectedPacket;

            Debug.WriteLine($"{expectedPacket}, {packet.Length}, {packet.ToHexString()}");

            if (packet.Length == 0)
            {
                // Odebraliśmy pakiet keep alive.
                
                // Oczekujemy keep alive lub żądania od klienta.
                if (expectedPacket == ReceivePacketOrder.ExpectedPackets.KeepAlive
                    || expectedPacket == ReceivePacketOrder.ExpectedPackets.Request)
                {
                    /* Resetujemy timeout oczekiwanego keep alive lub żądania
                    i ponawiamy oczekiwanie. */
                    // Jeżeli false, to wystąpił timeout - zdarzenie o nim jest już w kolejce.
                    Debug.WriteLine($"{nameof(OnReceiveSuccess)}, received keep alive and keep alive or request expected");
                    client.SetExpectedPacket(expectedPacket);
                    return;
                }

                // Oczekujemy pakietu innego niż keep alive lub żądanie.
                // Nie resetujemy timeoutu.
                return;
            }

            // Odebraliśmy pakiet nie keep alive.
            // Musimy zresetować timeout gdzieś w (bez)pośrednio wywołanej tu metodzie.
            if (expectedPacket == ReceivePacketOrder.ExpectedPackets.Request)
            {
                // Oczekujemy żądania od klienta.
                HandleExpectedRequest(client, packet);
                return;
            }

            // Oczekujemy pakietu innego niż żądanie.
            switch (expectedPacket)
            {
                case ReceivePacketOrder.ExpectedPackets.ClientIntroduction:
                    HandleExpectedClientIntroduction(client, packet);
                    break;
                default: // Oczekujemy np. KeepAlive, ale go nie odebraliśmy.
                    DisconnectThenNotify(client, UnexpectedPacketErrorMsg);
                    break;
            }
        }

        private const string UnexpectedPacketErrorMsg = "|sent| |an unexpected packet|.";

        #region Strict packets
        private void HandleExpectedClientIntroduction(Client client, byte[] packet)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}, {packet.ToHexString()}");

            // Pakiet już bez prefiksu, bo PacketReceiveBuffer go ucina.
            var pr = new PacketReader(packet);
            if ((Packet.Codes)pr.ReadUInt8() != ClientIntroduction.CODE)
            {
                InterruptHandshake(client, UnexpectedPacketErrorMsg);
                return;
            }

            ClientIntroduction.Deserialize(pr, _privateKey!,
                out PublicKey publicKey, out bool senderSignatureValid, out string login,
                out ulong verificationToken, out ulong remoteSeed);

            /* Zapisujemy dane (kredki) klienta na potrzeby dalszego kontynuowania jego sesji.
            Zapamiętujemy seed, aby móc weryfikować pakiety wysłane przez klienta do serwera. */
            ulong localSeed = RandomUInt64();
            client.Introduce(login, publicKey, remoteSeed, localSeed);

            if (verificationToken != client.VerificationToken)
            {
                // Klient wysłał inny token niż otrzymał od serwera.
                InterruptHandshake(client,
                    "|sent token different than the one that it received from the server|.");
                return;
            }

            if (!senderSignatureValid)
            {
                // Klient nie dysponuje kluczem prywatnym, czyli po prostu go nie zna.
                InterruptHandshake(client, "|does not own the public key sent by it|.");
                return;
            }

            ulong accountId;
            if (_storage.Database.AccountsByLogin.Exists(login))
            {
                var existingAccount = _storage.Database.AccountsByLogin.Get(login);
                // Login już istnieje.
                accountId = existingAccount.Id;

                if (!publicKey.Equals(existingAccount.PublicKey))
                {
                    // Klient wysłał inny klucz publiczny niż serwer ma zapisany w bazie.
                    InterruptHandshake(client,
                        "|sent a public key different from the one saved in the database|.");
                    return;
                }
                
                if (existingAccount.IsBlocked == 1)
                {
                    client.EnqueueToSend(AccountAlreadyBlocked.Serialize(_privateKey!,
                        client.PublicKey!, client.GenerateToken()), AccountAlreadyBlocked.CODE,
                        $"{client} |tried to authenticate using blocked account| " +
                        $"'{existingAccount.Login}'.");
                    return;
                }
            }
            else
            {
                // Login jeszcze nie istnieje, więc zapisujemy konto w bazie.
                (accountId, _) = _storage.Database.AccountsByLogin.Add(
                    new AccountDto { Login = login, PublicKey = publicKey, IsBlocked = 0 });
            }

            /* Login jeszcze nie istniał i został dodany lub już istniał i klient
            podpisał token swoim kluczem prywatnym powiązanym z kluczem publicznym
            publicKey. */

            client.Authenticate(accountId);
            ClientHandshaken?.Invoke(client);
            client.IsNotifiable = true;

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            // Wątek receiver klienta zaczyna nasłuchiwać żądań.
            client.EnqueueToSend(Authentication.Serialize(_privateKey!, client.PublicKey!,
                client.GenerateToken(), localSeed, accountId), Authentication.CODE);
        }

        private void InterruptHandshake(Client client, string errorMsg)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}, {errorMsg}");

            // Wciąż client.IsNotifiable == false, więc nie ustawiamy tego.

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.KeepAlive);
            client.EnqueueToSend(NoAuthentication.Serialize(_privateKey!,
                client.PublicKey!, client.GenerateToken()), NoAuthentication.CODE,
                $"{client} {errorMsg}");
        }
        #endregion

        private Packet.Codes? ReadOperationCodeFromSignedEncryptedPacket(
            Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            pr.Decrypt(_privateKey!);
            if (!pr.VerifySignature(client.PublicKey!))
            {
                DisconnectThenNotify(client, "|sent| |invalid packet signature|.");
                return null;
            }

            var operationCode = (Packet.Codes)pr.ReadUInt8();

            if (client.VerifyReceivedToken(pr.ReadUInt64()))
                return operationCode;

            DisconnectThenNotify(client, "|sent| |an unrecognized token|.");
            return null;
        }

        #region Random requests
        private void HandleExpectedRequest(Client client, byte[] packet)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}, {packet.ToHexString()}");

            var pr = new PacketReader(packet);
            try
            {
                var operationCode = ReadOperationCodeFromSignedEncryptedPacket(client, pr);
                if (operationCode is null)
                    return;

                switch (operationCode)
                {
                    case Packet.Codes.GetConversationsAndUsers:
                        HandleReceivedGetConversationsAndUsers(client);
                        break;
                    case Packet.Codes.AddConversation:
                        HandleReceivedAddConversation(client, pr);
                        break;
                    case Packet.Codes.SearchUsers:
                        HandleReceivedSearchUsers(client, pr);
                        break;
                    default:
                        DisconnectThenNotify(client, UnexpectedPacketErrorMsg);
                        break;
                }
            }
            catch (Error e) { DisconnectThenNotify(client, e.Message); }
        }

        private void HandleReceivedGetConversationsAndUsers(Client client)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            var dbClientParticipations = _storage.Database.ConversationParticipations
                .GetByParticipantId(client.Id);
            var dbClientConversations = _storage.Database.Conversations
                .GetByIds(dbClientParticipations.Select(cp => cp.ConversationId));
            var dbParticipations = _storage.Database.ConversationParticipations
                .GetByConversationIds(dbClientConversations.Select(c => c.Id));
            var dbParticipantAccounts = _storage.Database.AccountsById
                .GetByIds(dbParticipations.Select(p => p.ParticipantId).Distinct());

            /* Pobieramy oddzielnie konta właścicieli konwersacji, bo
            właściciel konwersacji może nie być jej uczestnikiem. */
            var dbConversationOwnerAccounts = _storage.Database.AccountsById
                .GetByIds(dbClientConversations.Select(c => c.OwnerId));

            // Grupujemy uczestnictwa według id konwersacji.
            var dbParticsByConvId = GroupBy(dbParticipations, p => p.ConversationId);

            var conversationParticipants = new ConversationsAndUsersLists
                .ConversationParticipation[dbClientConversations.Count()];
            int c = 0;
            foreach (var dbConversation in dbClientConversations)
            {
                var conversation = new ConversationsAndUsersLists.Conversation
                {
                    Id = dbConversation.Id,
                    OwnerId = dbConversation.OwnerId,
                    Name = dbConversation.Name
                };

                var dbParticipants = dbParticsByConvId[conversation.Id];
                var participants = new ConversationsAndUsersLists.Participant[dbParticipants.Count];
                int p = 0;
                foreach (var dbParticipant in dbParticipants)
                {
                    participants[p++] = new ConversationsAndUsersLists.Participant
                    {
                        ParticipantId = dbParticipant.ParticipantId,
                        /* DateTimeOffset.FromUnixTimeMilliseconds(p.JoinTime).UtcDateTime
                        "Valid values are between -62135596800000 and 253402300799999, inclusive. */
                        JoinTime = dbParticipant.JoinTime,
                        IsAdministrator = dbParticipant.IsAdministrator
                    };
                }

                conversationParticipants[c++] = new ConversationsAndUsersLists.ConversationParticipation
                {
                    Conversation = conversation,
                    Participants = participants
                };
            }

            var accounts = new Dictionary<ulong, ConversationsAndUsersLists.Account>();
            var concatenated = dbParticipantAccounts.Concat(dbConversationOwnerAccounts);
            foreach (var account in concatenated)
                if (!accounts.ContainsKey(account.Id))
                    accounts.Add(account.Id, new ConversationsAndUsersLists.Account
                    {
                        Id = account.Id,
                        Login = account.Login,
                        PublicKey = account.PublicKey,
                        IsBlocked = account.IsBlocked
                    });

            var lists = new ConversationsAndUsersLists.Lists
            {
                ConversationParticipants = conversationParticipants,
                Accounts = accounts.Values.ToArray()
            };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            if (client.IsNotifiable)
                client.EnqueueToSend(ConversationsAndUsersLists.Serialize(_privateKey!,
                    client.PublicKey!, client.GenerateToken(), lists), ConversationsAndUsersLists.CODE);
        }

        private Dictionary<K, LinkedList<V>> GroupBy<K, V>(IEnumerable<V> list, Func<V, K> keySelector)
            where K : struct
        {
            var dict = new Dictionary<K, LinkedList<V>>();
            foreach (var elem in list)
            {
                var key = keySelector(elem);
                if (!dict.ContainsKey(key))
                    dict.Add(key, new LinkedList<V>());
                dict[key].AddLast(elem);
            }
            return dict;
        }

        private void HandleReceivedAddConversation(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            AddConversation.Deserialize(pr, out ulong ownerId, out string name);
            
            if (!_storage.Database.AccountsById.Exists(ownerId))
            {
                client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
                client.EnqueueToSend(RequestError.Serialize(_privateKey!, client.PublicKey!,
                    client.GenerateToken(), AddConversation.CODE,
                    (byte)AddConversation.Errors.AccountDoesNotExist), RequestError.CODE);
                // Nie rozłączamy, bo nie jest to błąd protokołu, tylko błąd "biznesowy".
                return;
            }

            _storage.Database.Conversations.Add(new ConversationDto { OwnerId = ownerId, Name = name });

            // Po (lub przed) obsłużeniu pakietu trzeba anulować jego timeout za pomocą SetExpectedPacket.
            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
        }

        private void OnReceiveTimeout(ClientEvent @event)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}");

            Client client = @event.Sender;
            var order = (ReceivePacketOrder)@event.Data!;

            DisconnectThenNotify(client, "|timed out receiving packet| " +
                $"{order.ExpectedPacket}.");
        }

        private void HandleReceivedSearchUsers(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            SearchUsers.Deserialize(pr, out string loginFragment);

            // TODO: https://www.nuget.org/packages/Fastenshtein
            var users = _storage.Database.AccountsById.GetAll()
                .Where(u => u.Login.Contains(loginFragment))
                .Select(u => new FoundUsersList.User { Id = u.Id, Login = u.Login })
                .ToArray();

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            if (client.IsNotifiable)
                client.EnqueueToSend(FoundUsersList.Serialize(_privateKey!, client.PublicKey!,
                    client.GenerateToken(), users), FoundUsersList.CODE);
        }
        #endregion

        #region UI requests
        public void Request(UIRequest uiRequest)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {uiRequest.GetType().Name}");

            // Wątek UI
            _uiRequest = uiRequest;
            _eventQueueWaitBreaker.Cancel(false);
        }

        private void HandleUIRequest()
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {_uiRequest?.GetType().Name ?? "null UIRequest"}");

            // Wątek Server.Process
            /* Nie używamy monitor locka do uzyskiwania wyłącznego dostępu do
            _uiRequest, bo HandleUIRequest jest wykonywane tylko po wywołaniu
            przez wątek UI _eventQueueWaitBreaker.Cancel i wątek UI jest
            blokowany do momentu zakończenia obsługi żądania przez wątek
            Server.Process. */

            /* Nie ma żadnego żądania do obsłużenia - nieprawdopodobne, bo
            HandleUIRequest wykonuje się tylko po wykonaniu
            _eventQueueWaitBreaker.Cancel w RequestAndWait, które ustawia
            _uiRequest. */
            if (_uiRequest is null)
                return;

            if (!_uiRequest.TryMarkAsDone())
                /* Jeżeli nie uda się oznaczyć jako wykonane, to znaczy, że
                nastąpił timeout. Wówczas nie wykonujemy żądania. */
                return;

            switch (_uiRequest)
            {
                case StartServer startServer:
                    StartServerUIRequest(startServer);
                    break;
                case StopServer stopServer:
                    StopServerUIRequest(stopServer);
                    break;
                case DisconnectClient disconnectClient:
                    DisconnectClientUIRequest(disconnectClient);
                    break;
                case DisconnectAccount disconnectAccount:
                    DisconnectAccountUIRequest(disconnectAccount);
                    break;
                case BlockClientIP blockClientIP:
                    BlockClientIPUIRequest(blockClientIP);
                    break;
                case UnblockClientIP unblockClientIP:
                    UnblockClientIPUIRequest(unblockClientIP);
                    break;
                case BlockAccount blockAccount:
                    BlockAccountUIRequest(blockAccount);
                    break;
                case UnblockAccount unblockAccount:
                    UnblockAccountUIRequest(unblockAccount);
                    break;
                case StopProcess stopProcess:
                    StopProcessUIRequest(stopProcess);
                    break;
            }
        }

        private void StartServerUIRequest(StartServer request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.Guid}," +
                $"{request.PrivateKey}, {request.IpAddress}, {request.Port}, {request.Capacity}");

            // Wątek UI
            _guid = request.Guid;
            _privateKey = request.PrivateKey;
            // Memoizujemy, bo obliczanie klucza publicznego jest kosztowne.
            _publicKeyBytes = request.PrivateKey.ToPublicKey().ToBytes();
            _capacity = request.Capacity;
            _clients.Clear();
            _isAcceptingClients = true;

            string? errorMsg = null;
            // Tworzymy socket.
            var localEndPoint = new IPEndPoint(request.IpAddress.ToIPAddress(), request.Port.Value);
            _listener = new TcpListener(localEndPoint);
            try { _listener.Start(); }
            catch (SocketException se)
            {
                _listener.Stop();
                errorMsg = new Error(se, "|Error occured while| |starting the server|.").Message;
            }

            request.Callback?.Invoke(errorMsg);
        }

        private void StopServerUIRequest(StopServer request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}");

            _isAcceptingClients = false;
            // Rozłączamy wszystkich klientów.
            var clients = _clients.Select(c => c.Value).ToArray();
            foreach (var client in clients)
                DisconnectThenNotify(client, "|Server is stopping|.");
            _listener?.Stop();
            
            request.Callback?.Invoke();
        }

        private void DisconnectClientUIRequest(DisconnectClient request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.ClientKey}");

            // Wątek Server.Process
            ClientPrimaryKey clientKey = request.ClientKey;

            if (!_clients.TryGetValue(clientKey, out Client? client))
                // Jeżeli nie znaleźliśmy klienta, to zakładamy, że żądanie zostało wykonane.
                return;

            DisconnectThenNotify(client, "|was disconnected|.");
        }

        private void DisconnectAccountUIRequest(DisconnectAccount request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.Login}");

            // Wątek Server.Process
            DisconnectClientsWithLogin(request.Login);
        }

        private void DisconnectClientsWithLogin(string login)
        {
            var clientsWithLogin = _clients.Values.Where(c => login.Equals(c.Login));
            // Jeżeli nie znaleźliśmy żadnych klientów, to zakładamy, że żądanie zostało wykonane.
            foreach (var client in clientsWithLogin)
                DisconnectThenNotify(client, "|was disconnected|.");
        }

        private void BlockClientIPUIRequest(BlockClientIP request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.IpAddress}");

            // Wątek Server.Process
            IPv4Address ipAddress = request.IpAddress;

            var repo = _storage.Database.ClientIPBlocks;
            if (repo.Exists(ipAddress.BinaryRepresentation))
            {
                request.Callback($"|Client IP block with IP address| {ipAddress} " +
                    $"|already exists.|");
                return;
            }
            repo.Add(new ClientIPBlockDto { IpAddress = ipAddress.BinaryRepresentation });

            // Może być wielu klientów z tym samym IP, ale różnymi portami.
            var foundClients = _clients.Values.Where(
                client => client.GetPrimaryKey().IpAddress.Equals(ipAddress));

            foreach (var client in foundClients)
            {
                /* Usuwamy klienta z listy, aby już nie brać go pod uwagę
                przy obsługiwaniu żądań UI i zdarzeń innych klientów. */
                client.IsNotifiable = false;
                _clients.Remove(client.GetPrimaryKey());
                client.EnqueueToSend(IPNowBlocked.Serialize(_privateKey!, client.PublicKey!,
                    client.GenerateToken()), IPNowBlocked.CODE);
            }

            IPBlocked?.Invoke(ipAddress);

            request.Callback(null);
        }

        private void UnblockClientIPUIRequest(UnblockClientIP request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.IpAddress}");

            // Wątek Server.Process
            IPv4Address ipAddress = request.IpAddress;

            var repo = _storage.Database.ClientIPBlocks;
            if (repo.Exists(ipAddress.BinaryRepresentation))
                repo.Delete(ipAddress.BinaryRepresentation);

            IPUnblocked?.Invoke(ipAddress);
        }

        private void BlockAccountUIRequest(BlockAccount request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.Login}");

            // Wątek Server.Process
            SetAccountBlock(request.Login, 1);

            DisconnectClientsWithLogin(request.Login);

            request.Callback?.Invoke();
        }

        private void SetAccountBlock(string login, byte isBlocked)
        {
            var repo = _storage.Database.AccountsByLogin;
            if (repo.Exists(login))
            {
                var accountDto = repo.Get(login);
                accountDto.IsBlocked = isBlocked;
                repo.Update(login, accountDto);
            }
        }

        private void UnblockAccountUIRequest(UnblockAccount request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.Login}");

            // Wątek Server.Process
            SetAccountBlock(request.Login, 0);

            request.Callback?.Invoke();
        }

        private void StopProcessUIRequest(StopProcess request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}");

            // Wątek Server.Process
            /* Pętla w Process zakończy się natychmiast po ustawieniu
            tego i powrocie ze StopProcessUIRequest. */
            _stopRequested = true;

            request.Callback?.Invoke();
        }
        #endregion
    }
}
