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
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using Server.MVVM.Model.Networking.PacketOrders;
using Shared.MVVM.Model.Networking.Packets.ServerToClient;
using Shared.MVVM.Model.Networking.Packets;
using Shared.MVVM.Model.Networking.Packets.ClientToServer;
using Server.MVVM.Model.Networking.UIRequests;
using Shared.MVVM.Model;

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

            /* Do klienta jeszcze lub już nie możemy wysyłać powiadomień, bo jest
            w trakcie uścisku dłoni, rozłączania lub już został rozłączony. */
            client.IsNotifiable = false;
            client.IgnoreEvents = false;

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
                client.EnqueueToSend(IPAlreadyBlocked.Serialize(), IPAlreadyBlocked.CODE);
                return;
            }

            // Są wolne miejsca.
            _clients.Add(client.GetPrimaryKey(), client);
            ClientConnected?.Invoke(client);
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

            var receiveOrder = client.ReceiveOrder;
            if (receiveOrder is null)
            {
                // Nie oczekujemy żadnego pakietu.
                DisconnectThenNotify(client, "|sent a packet although| |server| |did not expect any|.");
                return;
            }

            var expectedPacket = receiveOrder.ExpectedPacket;
            if (packet.Length == 0)
            {
                // Odebraliśmy pakiet keep alive.
                ++client.ContiguousKeepAlivesCounter;
                
                // Oczekujemy keep alive lub żądania od klienta.
                if ((expectedPacket == ReceivePacketOrder.ExpectedPackets.KeepAlive
                    || expectedPacket == ReceivePacketOrder.ExpectedPackets.Request)
                    /* Resetujemy timeout oczekiwanego keep alive lub żądania
                    i ponawiamy oczekiwanie. */
                    // Jeżeli false, to wystąpił timeout.
                    && !client.SetExpectedPacket(expectedPacket))
                    return;
                
                // Oczekujemy pakietu innego niż keep alive lub żądanie.
                if (client.ContiguousKeepAlivesCounter >= Client.CONTIGUOUS_KEEP_ALIVES_LIMIT)
                {
                    DisconnectThenNotify(client, $"|sent| {client.ContiguousKeepAlivesCounter} " +
                        "|'keep alive' packets in a row|.");
                }
                // Nie resetujemy timeoutu.
                return;
            }

            // Odebraliśmy pakiet nie keep alive.
            if (expectedPacket == ReceivePacketOrder.ExpectedPackets.Request)
            {
                // Oczekujemy żądania od klienta.
                HandleExpectedRequest(client, packet);
                return;
            }

            // Oczekujemy pakietu innego niż żądanie.
            switch (expectedPacket)
            {
                case ReceivePacketOrder.ExpectedPackets.KeepAlive:
                    // Nie odebraliśmy keep alive, więc rozłączamy.
                    DisconnectThenNotify(client, UnexpectedPacketErrorMsg);
                    break;
                case ReceivePacketOrder.ExpectedPackets.ClientIntroduction:
                    HandleExpectedClientIntroduction(client, packet);
                    break;
            }
        }

        private const string UnexpectedPacketErrorMsg = "|sent| |an unexpected packet|.";

        #region Strict packets
        private void HandleExpectedClientIntroduction(Client client, byte[] packet)
        {
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
            
            var repo = _storage.Database.AccountsByLogin;
            if (repo.Exists(login))
            {
                var existingAccount = repo.Get(login);
                // Login już istnieje.

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
                repo.Add(new AccountDto { Login = login, PublicKey = publicKey, IsBlocked = 0 });
            }

            /* Login jeszcze nie istniał i został dodany lub już istniał i klient
            podpisał token swoim kluczem prywatnym powiązanym z kluczem publicznym
            publicKey. */

            ClientHandshaken?.Invoke(client);
            client.IsNotifiable = true;

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            // Wątek receiver klienta zaczyna nasłuchiwać żądań.
            client.EnqueueToSend(Authentication.Serialize(_privateKey!, client.PublicKey!,
                client.GenerateToken(), localSeed), Authentication.CODE);
        }

        private void InterruptHandshake(Client client, string errorMsg)
        {
            // Wciąż client.IsNotifiable == false, więc nie ustawiamy tego.

            client.EnqueueToSend(NoAuthentication.Serialize(_privateKey!,
                client.PublicKey!, client.GenerateToken()), NoAuthentication.CODE,
                $"{client} {errorMsg}");
        }
        #endregion

        private Packet.Codes? ReadOperationCodeFromSignedEncryptedPacket(
            Client client, PacketReader pr)
        {
            pr.Decrypt(_privateKey!);
            pr.VerifySignature(client.PublicKey!);

            var operationCode = (Packet.Codes)pr.ReadUInt8();

            if (client.VerifyReceivedToken(pr.ReadUInt64()))
                return operationCode;

            DisconnectThenNotify(client, "|sent| |an unrecognized token|.");
            return null;
        }

        #region Random requests
        private void HandleExpectedRequest(Client client, byte[] packet)
        {
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
                    default:
                        DisconnectThenNotify(client, UnexpectedPacketErrorMsg);
                        break;
                }
            }
            catch (Error e) { DisconnectThenNotify(client, e.Message); }
        }

        private readonly Random _rng = new Random(123);
        private void HandleReceivedGetConversationsAndUsers(Client client)
        {
            int conversationsCount = _rng.Next(2, 5);
            // _storage.Database.Conversations
            var conversationParticipants = new ConversationsAndUsersLists
                .ConversationParticipationModel[conversationsCount];

            var accounts = new Dictionary<ulong, ConversationsAndUsersLists.AccountModel>();
            for (int c = 0; c < conversationsCount; ++c)
            {
                var conversation = new ConversationsAndUsersLists.ConversationModel
                {
                    Id = (ulong)_rng.Next(),
                    OwnerId = (ulong)_rng.Next(),
                    Name = _rng.Next().ToString()
                };

                if (!accounts.ContainsKey(conversation.OwnerId))
                    accounts.Add(conversation.OwnerId, new ConversationsAndUsersLists.AccountModel
                    {
                        Id = conversation.OwnerId,
                        Login = RandomString(_rng.Next(15)),
                        PublicKey = new PublicKey(new byte[] { 0xAB }),
                        IsBlocked = (byte)_rng.Next(2)
                    });

                int participantsCount = _rng.Next(1, 4);
                var participants = new ConversationsAndUsersLists
                    .ParticipantModel[participantsCount];
                for (int p = 0; p < participantsCount; ++p)
                {
                    ulong id = (ulong)_rng.NextInt64(10);
                    participants[p] = new ConversationsAndUsersLists.ParticipantModel
                    {
                        ParticipantId = id,
                        /* DateTimeOffset.FromUnixTimeMilliseconds(p.JoinTime).UtcDateTime
                        "Valid values are between -62135596800000 and 253402300799999, inclusive. */
                        JoinTime = new DateTimeOffset(DateTime.UtcNow)
                            .AddMilliseconds(_rng.Next(1 << 20)).ToUnixTimeMilliseconds(),
                        IsAdministrator = (byte)_rng.Next(2)
                    };
                    new DateTimeOffset().ToUnixTimeMilliseconds();

                    if (!accounts.ContainsKey(id))
                        accounts.Add(id, new ConversationsAndUsersLists.AccountModel
                        {
                            Id = id,
                            Login = RandomString(_rng.Next(15)),
                            PublicKey = new PublicKey(new byte[] { 0xAB }),
                            IsBlocked = (byte)_rng.Next(2)
                        });
                }

                conversationParticipants[c] = new ConversationsAndUsersLists
                    .ConversationParticipationModel
                {
                    Conversation = conversation,
                    Participants = participants
                };
            }

            var model = new ConversationsAndUsersLists.ConversationsAndUsersListModel
            {
                ConversationParticipants = conversationParticipants,
                Accounts = accounts.Select(x => x.Value).ToArray()
            };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            client.EnqueueToSend(ConversationsAndUsersLists.Serialize(_privateKey!, client.PublicKey!,
                client.GenerateToken(), model), ConversationsAndUsersLists.CODE);
        }

        private string RandomString(int length)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; ++i)
            {
                int random = _rng.Next(26);
                sb.Append((char)('a' + random));
            }
            return sb.ToString();
        }

        private void HandleReceivedAddConversation(Client client, PacketReader pr)
        {
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
        }

        private void OnReceiveTimeout(ClientEvent @event)
        {
            Client client = @event.Sender;
            var order = (ReceivePacketOrder)@event.Data!;

            DisconnectThenNotify(client, "|timed out receiving packet| " +
                $"{order.ExpectedPacket}.");
        }
        #endregion

        #region UI requests
        public void Request(UIRequest uiRequest)
        {
            // Wątek UI
            _uiRequest = uiRequest;
            _eventQueueWaitBreaker.Cancel(false);
        }

        private void HandleUIRequest()
        {
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

        public void StartServerUIRequest(StartServer request)
        {
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
            // Wątek Server.Process
            ClientPrimaryKey clientKey = request.ClientKey;

            if (!_clients.TryGetValue(clientKey, out Client? client))
                // Jeżeli nie znaleźliśmy klienta, to zakładamy, że żądanie zostało wykonane.
                return;

            DisconnectThenNotify(client, "|was disconnected|.");

            request.Callback?.Invoke();
        }

        private void DisconnectAccountUIRequest(DisconnectAccount request)
        {
            // Wątek Server.Process
            string login = request.Login;

            var clientsWithLogin = _clients.Values.Where(c => login.Equals(c.Login));
            // Jeżeli nie znaleźliśmy żadnych klientów, to zakładamy, że żądanie zostało wykonane.
            foreach (var client in clientsWithLogin)
                DisconnectThenNotify(client, "|was disconnected|.");

            request.Callback?.Invoke();
        }

        private void BlockClientIPUIRequest(BlockClientIP request)
        {
            // Wątek Server.Process
            IPv4Address ipAddress = request.IpAddress;

            var repo = _storage.Database.ClientIPBlocks;
            if (!repo.Exists(ipAddress.BinaryRepresentation))
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

            request.Callback?.Invoke();
        }

        private void UnblockClientIPUIRequest(UnblockClientIP request)
        {
            // Wątek Server.Process
            IPv4Address ipAddress = request.IpAddress;

            var repo = _storage.Database.ClientIPBlocks;
            if (repo.Exists(ipAddress.BinaryRepresentation))
                repo.Delete(ipAddress.BinaryRepresentation);

            request.Callback?.Invoke();
        }

        private void BlockAccountUIRequest(BlockAccount request)
        {
            // Wątek Server.Process
            SetAccountBlock(request.Login, 1);

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
            // Wątek Server.Process
            SetAccountBlock(request.Login, 0);

            request.Callback?.Invoke();
        }

        private void StopProcessUIRequest(StopProcess request)
        {
            // Wątek Server.Process
            /* Pętla w Process zakończy się natychmiast po ustawieniu
            tego i powrocie ze StopProcessUIRequest. */
            _stopRequested = true;

            request.Callback?.Invoke();
        }
        #endregion
    }
}
