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
using Shared.MVVM.Model.Networking.Packets.ClientToServer.Conversation;
using Shared.MVVM.Model.Networking.Packets.ServerToClient.Conversation;
using Shared.MVVM.Model.Networking.Packets.ClientToServer.Participation;
using Shared.MVVM.Model.Networking.Packets.ServerToClient.Participation;
using Shared.MVVM.Model.Networking.Packets.ClientToServer.Message;
using Shared.MVVM.Model.Networking.Packets.ServerToClient.Message;

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
        public delegate void Event(Client client);
        public delegate void Event<in ParT>(Client client, ParT parameter);

        public event Event? ClientConnected;
        public event Event? ClientHandshaken;
        public event Event<string>? ClientEndedConnection;
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
                    case Packet.Codes.EditConversation:
                        HandleReceivedEditConversation(client, pr);
                        break;
                    case Packet.Codes.DeleteConversation:
                        HandleReceivedDeleteConversation(client, pr);
                        break;
                    case Packet.Codes.SearchUsers:
                        HandleReceivedSearchUsers(client, pr);
                        break;
                    case Packet.Codes.AddParticipation:
                        HandleReceivedAddParticipation(client, pr);
                        break;
                    case Packet.Codes.EditParticipation:
                        HandleReceivedEditParticipation(client, pr);
                        break;
                    case Packet.Codes.DeleteParticipation:
                        HandleReceivedDeleteParticipation(client, pr);
                        break;
                    case Packet.Codes.SendMessage:
                        HandleReceivedSendMessage(client, pr);
                        break;
                    case Packet.Codes.GetMessages:
                        HandleReceivedGetMessages(client, pr);
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
            var dbClientOwnedConversations = _storage.Database.Conversations.GetByOwnerId(client.Id);
            /* Zbieramy wszystkie konwersacje, do których należy klient i których jest właścicielem
            (są to zbiory rozłączne) i porządkujemy je według Id, czyli będą w kolejności od
            najstarszej do najnowszej. */
            var conversationIds = dbClientParticipations.Select(cp => cp.ConversationId)
                .Concat(dbClientOwnedConversations.Select(coc => coc.Id)).Order();

            var dbClientConversations = _storage.Database.Conversations.GetByIds(conversationIds);
            var dbParticipations = _storage.Database.ConversationParticipations
                .GetByConversationIds(dbClientConversations.Select(c => c.Id));
            var dbParticipantAccounts = _storage.Database.AccountsById
                .GetByIds(dbParticipations.Select(p => p.ParticipantId).Distinct());

            /* Pobieramy oddzielnie konta właścicieli konwersacji, bo właściciel konwersacji
            nie jest jej zwyczajnym uczestnikiem (nie ma go w tabeli ConversationParticipation). */
            var dbConversationOwnerAccounts = _storage.Database.AccountsById
                .GetByIds(dbClientConversations.Select(c => c.OwnerId));

            // Grupujemy uczestnictwa według id konwersacji.
            var dbParticsByConvId = GroupBy(dbParticipations, p => p.ConversationId);

            var conversationParticipants = new GotConversationsAndUsersLists
                .ConversationParticipation[dbClientConversations.Count()];
            int c = 0;
            foreach (var dbConversation in dbClientConversations)
            {
                var dbMessages = _storage.Database.Messages.GetByConversationId(dbConversation.Id);
                var conversation = new GotConversationsAndUsersLists.Conversation
                {
                    Id = dbConversation.Id,
                    OwnerId = dbConversation.OwnerId,
                    Name = dbConversation.Name,
                    /* TODO: zoptymalizować, żeby nie pobierać wiadomości i ich
                    zaszyfrowanych kopii, tylko robić request z COUNT do SQLite */
                    UnreceivedMessagesCount = (uint)_storage.Database.EncryptedMessageCopies
                        .GetUnreceived(client.Id, dbMessages.Select(m => m.Id)).Count()
                };

                if (!dbParticsByConvId.TryGetValue(conversation.Id, out var dbParticipants))
                    dbParticipants = new LinkedList<ConversationParticipationDto>();

                /* Jeżeli konwersacja nie zawiera żadnych uczestników (tylko
                właściciela, który nie jest zwykłym uczestnikiem). */
                var participants = new GotConversationsAndUsersLists.Participant[dbParticipants.Count];
                int p = 0;
                foreach (var dbParticipant in dbParticipants)
                    participants[p++] = new GotConversationsAndUsersLists.Participant
                    {
                        ParticipantId = dbParticipant.ParticipantId,
                        JoinTime = dbParticipant.JoinTime,
                        IsAdministrator = dbParticipant.IsAdministrator
                    };

                conversationParticipants[c++] = new GotConversationsAndUsersLists.ConversationParticipation
                {
                    Conversation = conversation,
                    Participants = participants
                };
            }

            var accounts = new Dictionary<ulong, GotConversationsAndUsersLists.Account>();
            var concatenated = dbParticipantAccounts.Concat(dbConversationOwnerAccounts);
            foreach (var account in concatenated)
                if (!accounts.ContainsKey(account.Id))
                    accounts.Add(account.Id, new GotConversationsAndUsersLists.Account
                    {
                        Id = account.Id,
                        Login = account.Login,
                        PublicKey = account.PublicKey,
                        // IsBlocked = account.IsBlocked
                    });

            var lists = new GotConversationsAndUsersLists.Lists
            {
                ConversationParticipants = conversationParticipants,
                Accounts = accounts.Values.ToArray()
            };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            if (client.IsNotifiable)
                client.EnqueueToSend(GotConversationsAndUsersLists.Serialize(_privateKey!,
                    client.PublicKey!, client.GenerateToken(), lists), GotConversationsAndUsersLists.CODE);
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

            AddConversation.Deserialize(pr, out var conversationName);

            // Repository.Add ignoruje Id.
            var dto = new ConversationDto { OwnerId = client.Id, Name = conversationName };

            // Dodajemy konwersację. Repository.Add samo powinno ustawić dto.Id.
            dto.Id = _storage.Database.Conversations.Add(dto);

            var outConversation = new AddedConversation.Conversation { Id = dto.Id, Name = dto.Name };

            // Po (lub przed) obsłużeniu pakietu trzeba anulować jego timeout za pomocą SetExpectedPacket.
            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            // Powiadamiamy tylko samego autora konwersacji, o ile nie ma zablokowanego powiadamiania.
            if (client.IsNotifiable)
                client.EnqueueToSend(AddedConversation.Serialize(_privateKey!, client.PublicKey!,
                    client.GenerateToken(), outConversation), AddedConversation.CODE);
        }

        private void HandleReceivedEditConversation(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            EditConversation.Deserialize(pr, out var inConversation);

            if (!_storage.Database.Conversations.Exists(inConversation.Id))
            {
                EnqueueRequestError(client, EditConversation.CODE,
                    (byte)EditConversation.Errors.ConversationNotExists);
                return;
            }
            var oldConversation = _storage.Database.Conversations.Get(inConversation.Id);

            var participations = _storage.Database.ConversationParticipations
                .GetByConversationId(oldConversation.Id);
            if (client.Id != oldConversation.OwnerId)
            {
                // Użytkownik nie jest właścicielem konwersacji.
                EnqueueRequestError(client, EditConversation.CODE,
                    (byte)EditConversation.Errors.RequesterNotConversationOwner);
                return;
            }

            var newConversation = new ConversationDto
            { Id = oldConversation.Id, OwnerId = oldConversation.OwnerId, Name = inConversation.Name };

            // Edytujemy konwersację.
            _storage.Database.Conversations.Update(oldConversation.Id, newConversation);

            var outConversation = new EditedConversation.Conversation
            { Id = oldConversation.Id, Name = newConversation.Name };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);

            /* Powiadamiamy wszystkich uczestników konwersacji i właściciela, który wywołał
            edycję konwersacji. */
            var clientsById = SingleElementGroupBy(_clients.Values, c => c.Id);
            foreach (var recipientId in participations.Select(p => p.ParticipantId).Append(client.Id))
                // Czy uczestnik jest połączony z serwerem i możliwy do powiadomienia?
                if (clientsById.TryGetValue(recipientId, out var c) && c.IsNotifiable)
                    c.EnqueueToSend(EditedConversation.Serialize(_privateKey!, c.PublicKey!,
                        c.GenerateToken(), outConversation), EditedConversation.CODE);
        }

        private Dictionary<K, V> SingleElementGroupBy<K, V>(IEnumerable<V> list, Func<V, K> keySelector)
            where K : struct
        {
            // Zakładamy, że każdy klucz występuje tylko raz w list.
            var dict = new Dictionary<K, V>();
            foreach (var elem in list)
                dict[keySelector(elem)] = elem;
            return dict;
        }

        private void EnqueueRequestError(Client client, Packet.Codes faultyOperationCode, byte errorCode)
        {
            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            if (client.IsNotifiable)
                client.EnqueueToSend(RequestError.Serialize(_privateKey!, client.PublicKey!,
                    client.GenerateToken(), faultyOperationCode, errorCode), RequestError.CODE);
            // Nie rozłączamy, bo nie jest to błąd protokołu, tylko błąd "biznesowy".
        }

        private void HandleReceivedDeleteConversation(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            DeleteConversation.Deserialize(pr, out ulong conversationId);

            if (!_storage.Database.Conversations.Exists(conversationId))
            {
                EnqueueRequestError(client, DeleteConversation.CODE,
                    (byte)DeleteConversation.Errors.ConversationNotExists);
                return;
            }

            var participations = _storage.Database.ConversationParticipations
                .GetByConversationId(conversationId);

            var conversation = _storage.Database.Conversations.Get(conversationId);
            if (client.Id != conversation.OwnerId)
            {
                EnqueueRequestError(client, DeleteConversation.CODE,
                    (byte)DeleteConversation.Errors.AccountNotConversationOwner);
                return;
            }

            // Usuwamy konwersację.
            _storage.Database.Conversations.Delete(conversationId);

            var outConversationId = conversationId;

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);

            /* Powiadamiamy wszystkich uczestników konwersacji i właściciela, który wywołał
            usunięcie konwersacji. */
            var clientsById = SingleElementGroupBy(_clients.Values, c => c.Id);
            foreach (var recipientId in participations.Select(p => p.ParticipantId).Append(client.Id))
                if (clientsById.TryGetValue(recipientId, out var c) && c.IsNotifiable)
                    c.EnqueueToSend(DeletedConversation.Serialize(_privateKey!, c.PublicKey!,
                        c.GenerateToken(), outConversationId), DeletedConversation.CODE);
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

            // Powiadamiamy tylko użytkownika, który wywołał wyszukiwanie użytkowników.
            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            if (client.IsNotifiable)
                client.EnqueueToSend(FoundUsersList.Serialize(_privateKey!, client.PublicKey!,
                    client.GenerateToken(), users), FoundUsersList.CODE);
        }

        private void HandleReceivedAddParticipation(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            AddParticipation.Deserialize(pr, out var inParticipation);

            var conversation = _storage.Database.Conversations.Get(inParticipation.ConversationId);
            var participations = _storage.Database.ConversationParticipations
                .GetByConversationId(inParticipation.ConversationId);
            if (client.Id != conversation.OwnerId &&
                !participations.Any(p => p.ParticipantId == client.Id && p.IsAdministrator != 0))
            {
                // Użytkownik nie jest właścicielem ani administratorem konwersacji.
                EnqueueRequestError(client, AddParticipation.CODE,
                    (byte)AddParticipation.Errors.YouNeitherConversationOwnerNorAdmin);
                return;
            }

            if (!_storage.Database.AccountsById.Exists(inParticipation.ParticipantId))
            {
                EnqueueRequestError(client, AddParticipation.CODE,
                    (byte)AddParticipation.Errors.AccountNotExists);
                return;
            }

            if (conversation.OwnerId == inParticipation.ParticipantId)
            {
                EnqueueRequestError(client, AddParticipation.CODE,
                    (byte)AddParticipation.Errors.AccountIsConversationOwner);
                return;
            }

            if (participations.Any(p => p.ParticipantId == inParticipation.ParticipantId))
            {
                EnqueueRequestError(client, AddParticipation.CODE,
                    (byte)AddParticipation.Errors.ParticipationAlreadyExists);
                return;
            }

            // Dodajemy uczestnictwo w konwersacji.
            var dto = new ConversationParticipationDto
            {
                ConversationId = inParticipation.ConversationId,
                ParticipantId = inParticipation.ParticipantId,
                JoinTime = DateTime.UtcNow.ToUnixTimestamp(),
                /* Na początku nowy członek nie jest administratorem.
                Można go awansować poprzez pakiet EditParticipation. */
                IsAdministrator = 0
            };
            _storage.Database.ConversationParticipations.Add(dto);

            var participant = _storage.Database.AccountsById.GetById(dto.ParticipantId);
            var outParticipation = new AddedParticipation.Participation
            {
                ConversationId = dto.ConversationId,
                JoinTime = dto.JoinTime,
                IsAdministrator = dto.IsAdministrator,
                Participant = new AddedParticipation.Participant
                {
                    Id = participant.Id,
                    Login = participant.Login,
                    PublicKey = participant.PublicKey,
                    // IsBlocked = participant.IsBlocked
                }
            };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);

            // Powiadamiamy uczestników i właściciela konwersacji.
            var clientsById = SingleElementGroupBy(_clients.Values, c => c.Id);
            foreach (var recipientId in participations.Select(p => p.ParticipantId)
                .Append(conversation.OwnerId))
                if (clientsById.TryGetValue(recipientId, out var c) && c.IsNotifiable)
                    c.EnqueueToSend(AddedParticipation.Serialize(_privateKey!, c.PublicKey!,
                        c.GenerateToken(), outParticipation), AddedParticipation.CODE);

            // Powiadamiamy użytkownika dodanego do konwersacji.
            if (clientsById.TryGetValue(participant.Id, out var addedParticipantClient))
            {
                // Dodany użytkownik jest połączony.
                var dbOwner = _storage.Database.AccountsById.GetById(conversation.OwnerId);
                // Wysyłamy go razem z już istniejącymi uczestnikami.
                participations = participations.Append(dto);
                var dbParticipantAccounts = _storage.Database.AccountsById
                    .GetByIds(participations.Select(p => p.ParticipantId));
                var dictParticipantAccounts = SingleElementGroupBy(dbParticipantAccounts, a => a.Id);

                var outParticipations = new AddedYouAsParticipant.Participation[participations.Count()];
                int i = 0;
                foreach (var p in participations)
                {
                    var account = dictParticipantAccounts[p.ParticipantId];
                    outParticipations[i++] = new AddedYouAsParticipant.Participation
                    {
                        JoinTime = p.JoinTime,
                        IsAdministrator = p.IsAdministrator,
                        Participant = new AddedYouAsParticipant.User
                        {
                            Id = account.Id,
                            Login = account.Login,
                            PublicKey = account.PublicKey
                            // IsBlocked = account.IsBlocked
                        }
                    };
                }

                var outYourParticipation = new AddedYouAsParticipant.YourParticipation
                {
                    JoinTime = dto.JoinTime,
                    IsAdministrator = dto.IsAdministrator,
                    Conversation = new AddedYouAsParticipant.Conversation
                    {
                        Id = conversation.Id,
                        Name = conversation.Name,
                        Owner = new AddedYouAsParticipant.User
                        {
                            Id = dbOwner.Id,
                            Login = dbOwner.Login,
                            PublicKey = dbOwner.PublicKey
                        },
                        Participations = outParticipations
                    }
                };

                var c = addedParticipantClient;
                if (c.IsNotifiable)
                    c.EnqueueToSend(AddedYouAsParticipant.Serialize(_privateKey!, c.PublicKey!,
                        c.GenerateToken(), outYourParticipation), AddedYouAsParticipant.CODE);
            }
        }

        private void HandleReceivedEditParticipation(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            EditParticipation.Deserialize(pr, out var inParticipation);

            var conversation = _storage.Database.Conversations.Get(inParticipation.ConversationId);
            if (client.Id != conversation.OwnerId)
            {
                // Użytkownik nie jest właścicielem konwersacji.
                EnqueueRequestError(client, EditParticipation.CODE,
                    (byte)EditParticipation.Errors.YouNotConversationOwner);
                return;
            }

            var participations = _storage.Database.ConversationParticipations
                .GetByConversationId(inParticipation.ConversationId);
            var oldParticipation = participations.SingleOrDefault(
                p => p.ParticipantId == inParticipation.ParticipantId);
            if (oldParticipation is null)
            {
                EnqueueRequestError(client, EditParticipation.CODE,
                    (byte)EditParticipation.Errors.ParticipationNotExists);
                return;
            }

            // Edytujemy uczestnictwo w konwersacji.
            var newParticipation = new ConversationParticipationDto
            {
                ConversationId = oldParticipation.ConversationId,
                ParticipantId = oldParticipation.ParticipantId,
                IsAdministrator = inParticipation.IsAdministrator,
                JoinTime = oldParticipation.JoinTime
            };
            _storage.Database.ConversationParticipations.Update(
                (oldParticipation.ConversationId, oldParticipation.ParticipantId), newParticipation);

            var outParticipation = new EditedParticipation.Participation
            {
                ConversationId = newParticipation.ConversationId,
                ParticipantId = newParticipation.ParticipantId,
                IsAdministrator = newParticipation.IsAdministrator
            };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);

            // Powiadamiamy uczestników i właściciela konwersacji.
            var clientsById = SingleElementGroupBy(_clients.Values, c => c.Id);
            foreach (var recipientId in participations.Select(p => p.ParticipantId)
                .Append(conversation.OwnerId))
                if (clientsById.TryGetValue(recipientId, out var c) && c.IsNotifiable)
                    c.EnqueueToSend(EditedParticipation.Serialize(_privateKey!, c.PublicKey!,
                        c.GenerateToken(), outParticipation), EditedParticipation.CODE);
        }

        private void HandleReceivedDeleteParticipation(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            DeleteParticipation.Deserialize(pr, out var inParticipation);

            var conversation = _storage.Database.Conversations.Get(inParticipation.ConversationId);
            var participations = _storage.Database.ConversationParticipations
                .GetByConversationId(inParticipation.ConversationId);
            // Czy użytkownik nie chce opuścić konwersacji, czyli usunąć z niej samego siebie?
            if (inParticipation.ParticipantId != client.Id
                // Czy użytkownik nie jest właścicielem konwersacji?
                && client.Id != conversation.OwnerId
                // Czy użytkownik nie jest administratorem konwersacji?
                && !participations.Any(p => p.ParticipantId == client.Id && p.IsAdministrator != 0))
            {
                EnqueueRequestError(client, DeleteParticipation.CODE,
                    (byte)DeleteParticipation.Errors.YouNeitherConversationOwnerNorAdmin);
                return;
            }

            var oldParticipation = participations.SingleOrDefault(
                p => p.ParticipantId == inParticipation.ParticipantId);
            if (oldParticipation is null)
            {
                EnqueueRequestError(client, DeleteParticipation.CODE,
                    (byte)DeleteParticipation.Errors.ParticipationNotExists);
                return;
            }

            // Usuwamy uczestnictwo w konwersacji.
            _storage.Database.ConversationParticipations.Delete(
                (oldParticipation.ConversationId, oldParticipation.ParticipantId));

            var outParticipation = new DeletedParticipation.Participation
            {
                ConversationId = oldParticipation.ConversationId,
                ParticipantId = oldParticipation.ParticipantId,
            };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);

            // Powiadamiamy uczestników i właściciela konwersacji.
            var clientsById = SingleElementGroupBy(_clients.Values, c => c.Id);
            foreach (var recipientId in participations.Select(p => p.ParticipantId)
                .Append(conversation.OwnerId))
                if (clientsById.TryGetValue(recipientId, out var c) && c.IsNotifiable)
                    c.EnqueueToSend(DeletedParticipation.Serialize(_privateKey!, c.PublicKey!,
                        c.GenerateToken(), outParticipation), DeletedParticipation.CODE);
        }

        private void HandleReceivedSendMessage(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            SendMessage.Deserialize(pr, out var inMessage);

            if (!_storage.Database.Conversations.Exists(inMessage.ConversationId))
            {
                EnqueueRequestError(client, SendMessage.CODE,
                    (byte)SendMessage.Errors.ConversationNotExists);
                return;
            }

            var dbConversation = _storage.Database.Conversations.Get(inMessage.ConversationId);
            var dbParticipations = _storage.Database.ConversationParticipations
                .GetByConversationId(inMessage.ConversationId);
            if (!dbParticipations.Any(p => p.ParticipantId == client.Id)
                && dbConversation.OwnerId != client.Id)
            {
                EnqueueRequestError(client, SendMessage.CODE,
                    (byte)SendMessage.Errors.YouNotBelongToConversation);
                return;
            }

            // Zapisujemy wiadomość.
            var messageDto = new MessageDto
            {
                ConversationId = inMessage.ConversationId,
                SenderId = client.Id,
                SendTime = DateTime.UtcNow.ToUnixTimestamp()
            };
            messageDto.Id = _storage.Database.Messages.Add(messageDto);

            // Zapisujemy załączniki.
            var attachmentDtos = new AttachmentDto[inMessage.AttachmentMetadatas.Length];
            int a = 0;
            foreach (var attachmentMetadata in inMessage.AttachmentMetadatas)
            {
                var attachmentDto = new AttachmentDto
                {
                    MessageId = messageDto.Id,
                    Name = attachmentMetadata.Name
                };
                attachmentDto.Id = _storage.Database.Attachments.Add(attachmentDto);

                attachmentDtos[a++] = attachmentDto;
            }

            // Zapisujemy zaszyfrowane kopie wiadomości.
            foreach (var recipient in inMessage.Recipients)
            {
                var encryptedMessageCopyDto = new EncryptedMessageCopyDto
                {
                    MessageId = messageDto.Id,
                    RecipientId = recipient.ParticipantId,
                    Content = recipient.EncryptedContent,
                    ReceiveTime = null
                };
                _storage.Database.EncryptedMessageCopies.Add(encryptedMessageCopyDto);

                // Zapisujemy zaszyfrowane kopie załączników.
                a = 0;
                foreach (var attachment in recipient.Attachments)
                {
                    var encryptedAttachmentCopyDto = new EncryptedAttachmentCopyDto
                    {
                        AttachmentId = attachmentDtos[a].Id,
                        RecipientId = recipient.ParticipantId,
                        Content = attachment.EncryptedContent
                    };
                    _storage.Database.EncryptedAttachmentCopies.Add(encryptedAttachmentCopyDto);
                }
            }

            var outMessageMetadata = new SentMessage.MessageMetadata
            { ConversationId = messageDto.ConversationId, MessageId = messageDto.Id };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);

            // Powiadamiamy uczestników i właściciela konwersacji.
            var clientsById = SingleElementGroupBy(_clients.Values, c => c.Id);
            foreach (var recipientId in dbParticipations.Select(p => p.ParticipantId)
                .Append(dbConversation.OwnerId))
                if (clientsById.TryGetValue(recipientId, out var c) && c.IsNotifiable)
                    c.EnqueueToSend(SentMessage.Serialize(_privateKey!, c.PublicKey!,
                        c.GenerateToken(), outMessageMetadata), SentMessage.CODE);

            // w odpowiedzi na request getmessage wysylamy wiadomosc i metadane zalacznikow (id, nazwa, rozmiar)
            // w odpowiedzi na request getattachment wysylamy dane zalacznika
        }

        private void HandleReceivedGetMessages(Client client, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {client}");

            GetMessages.Deserialize(pr, out var inFilter);

            if (!_storage.Database.Conversations.Exists(inFilter.ConversationId))
            {
                EnqueueRequestError(client, GetMessages.CODE,
                    (byte)GetMessages.Errors.ConversationNotExists);
                return;
            }

            var dbConversation = _storage.Database.Conversations.Get(inFilter.ConversationId);
            var dbParticipations = _storage.Database.ConversationParticipations
                .GetByConversationId(inFilter.ConversationId);
            if (!dbParticipations.Any(p => p.ParticipantId == client.Id)
                && dbConversation.OwnerId != client.Id)
            {
                EnqueueRequestError(client, GetMessages.CODE,
                    (byte)GetMessages.Errors.YouNotBelongToConversation);
                return;
            }

            var dbMessages = inFilter.FindNewest == 1 ?
                _storage.Database.Messages.GetNewest(inFilter.ConversationId, 10) :
                _storage.Database.Messages.GetOlderThan(inFilter.ConversationId, inFilter.MessageId, 10);
            var dbMessageIds = dbMessages.Select(m => m.Id);
            var dbEncMsgCps = SingleElementGroupBy(_storage.Database.EncryptedMessageCopies
                .GetByRecipientAndMessageIds(client.Id, dbMessageIds), emc => emc.MessageId);
            var dbAttachments = GroupBy(_storage.Database.Attachments
                .GetByMessageIds(dbMessageIds), att => att.MessageId);

            var outList = new MessagesList.List
            {
                ConversationId = inFilter.ConversationId,
                Messages = new MessagesList.Message[dbMessages.Count()]
            };

            int i = 0;
            foreach (var dbMessage in dbMessages)
                outList.Messages[i++] = new MessagesList.Message
                {
                    Id = dbMessage.Id,
                    SenderId = dbMessage.SenderId,
                    SendTime = dbMessage.SendTime,
                    /* Wiadomość zawsze musi mieć zaszyfrowaną kopię treści przeznaczoną
                    dla użytkownika, który wysłał żądanie GetMessages. */
                    EncryptedContent = dbEncMsgCps[dbMessage.Id].Content,
                    // Obsługujemy sytuację, w której wiadomość nie ma załączników.
                    AttachmentMetadatas = dbAttachments.ContainsKey(dbMessage.Id)
                        ? dbAttachments[dbMessage.Id].Select(att =>
                            new MessagesList.AttachmentMetadata { Id = att.Id, Name = att.Name }).ToArray()
                        : new MessagesList.AttachmentMetadata[0]
                };

            client.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Request);
            if (client.IsNotifiable)
                client.EnqueueToSend(MessagesList.Serialize(_privateKey!, client.PublicKey!,
                    client.GenerateToken(), outList), MessagesList.CODE);
        }
        #endregion

        private void OnReceiveTimeout(ClientEvent @event)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}");

            Client client = @event.Sender;
            var order = (ReceivePacketOrder)@event.Data!;

            DisconnectThenNotify(client, "|timed out receiving packet| " +
                $"{order.ExpectedPacket}.");
        }

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
