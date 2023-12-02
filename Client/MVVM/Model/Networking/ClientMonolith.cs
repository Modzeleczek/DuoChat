using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Shared.MVVM.Core;
using System.Threading;
using System.Collections.Concurrent;
using Client.MVVM.Model.Networking.PacketOrders;
using Client.MVVM.ViewModel.Observables;
using System.Collections.Generic;
using Shared.MVVM.Model.Networking.Packets;
using Shared.MVVM.Model.Networking.Packets.ServerToClient;
using Server.MVVM.Model.Networking.Packets.ServerToClient;
using Shared.MVVM.Model.Networking.Packets.ClientToServer;

namespace Client.MVVM.Model.Networking
{
    // Klasa odpowiada za cały stan klienta i zarządzanie połączonym serwerem.
    public class ClientMonolith : IEventProcessor
    {
        #region Fields
        // Stałe
        private const int CONNECT_TIMEOUT_SECONDS = 2;

        // Usługi
        private readonly Storage _storage;

        // Dane lokalnego hosta
        private string? _login = null;
        private PrivateKey? _privateKey = null;

        // Zdalny host
        private RemoteServer? _remoteServer = null;

        // Do przebiegu sterowania
        public Task ClientProcessTask { get; private set; } = Task.CompletedTask;
        private bool _stopRequested = false;
        private readonly BlockingCollection<ServerEvent> _eventQueue =
            new BlockingCollection<ServerEvent>();
        // Wczesna (eager) inicjalizacja
        private CancellationTokenSource _eventQueueWaitBreaker = new CancellationTokenSource();
        private UIRequest? _uiRequest = null;
        #endregion

        #region Events
        public event Action<RemoteServer>? ServerIntroduced;
        public event Action<RemoteServer>? ServerHandshaken;
        public event Action<RemoteServer, Conversation[]>? ReceivedConversationsAndUsersList;
        public event Action<RemoteServer, string>? ReceivedRequestError;
        public event Action<RemoteServer, string>? ServerEndedConnection;
        public event Action? ClientStopped;
        #endregion

        public ClientMonolith(Storage storage)
        {
            _storage = storage;
        }

        public void Connect(IPv4Address ipAddress, Port port, string login, PrivateKey privateKey)
        {
            // Wątek UI
            _login = login;
            _privateKey = privateKey;
            _stopRequested = false;

            // Tworzymy socket.
            /* TODO: fejkowe "factory" zwracające ciągle ten sam obiekt RemoteServer,
            tylko ze zresetowanym stanem (wyczyszczone bufory itp.). */
            _remoteServer = new RemoteServer(CreateConnectedTcpClient(
                ipAddress.ToIPAddress(), port.Value), this);

            // Uruchamiamy wątek Client.Process.
            ClientProcessTask = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
        }

        private TcpClient CreateConnectedTcpClient(IPAddress ipAddress, int port)
        {
            Error? error = null;
            // https://stackoverflow.com/a/43237063
            TcpClient tcpClient = new TcpClient();
            var timeOut = TimeSpan.FromSeconds(CONNECT_TIMEOUT_SECONDS);
            var cancellationCompletionSource = new TaskCompletionSource<bool>();
            try
            {
                /* W obiekcie CancellationTokenSource tworzy się task "anulujący",
                który zostanie anulowany po czasie timeOut. */
                using (var cts = new CancellationTokenSource(timeOut))
                {
                    // Rozpoczynamy taska "łączącego", który łączy TcpClienta z serwerem.
                    var connectingTask = tcpClient.ConnectAsync(ipAddress, port);
                    /* Ustawiamy funkcję, która zostanie wykonana w momencie anulowania taska
                    obiektu CancellationTokenSource (czyli po czasie timeOut). */
                    using (cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true)))
                    {
                        /* Blokując, czekamy na zakończenie pierwszego z dwóch tasków:
                        łączącego lub anulującego; jeżeli pierwszy zakończy się nie task łączący,
                        ale anulujący, to wyrzucamy wyjątek. */
                        var whenAny = Task.WhenAny(connectingTask, cancellationCompletionSource.Task);
                        whenAny.Wait();
                        if (whenAny.Result != connectingTask)
                            throw new OperationCanceledException(cts.Token);
                        /* Jeżeli w tasku łączącym został wyrzucony wyjątek, to wyrzucamy
                        go w aktualnej metodzie, aby został obsłużony w catchach na dole. */
                        // throw exception inside 'task' (if any)
                        if (connectingTask.Exception?.InnerException != null)
                            throw connectingTask.Exception.InnerException;
                    }
                }
                return tcpClient;
            }
            catch (OperationCanceledException e)
            { error = new Error(e, "|Server connection timed out.|"); }
            catch (SocketException e)
            // Dokładna przyczyna braku połączenia jest w SocketException.Message.
            { error = new Error(e, "|No response from the server.|"); }
            catch (Exception e)
            { error = new Error(e, "|Error occured while| |connecting to the server.|"); }
            /* System.ArgumentNullException - nie może wystąpić, bo walidujemy adres IP
            System.ArgumentOutOfRangeException - nie może wystąpić, bo walidujemy port
            System.ObjectDisposedException - nie może wystąpić, bo tworzymy nowy,
            niezdisposowany obiekt TcpClient */
            // Wykonuje się, jeżeli złapiemy jakikolwiek wyjątek.
            tcpClient?.Close();
            throw error;
        }

        private void Process()
        {
            // Wątek Client.Process
            StartHandshake();

            while (true)
            {
                /* Nie używamy monitor locka, bo _stopRequested może zmienić
                wartość tylko w HandleUIRequest w tym samym wątku (Client.Process). */
                if (_stopRequested)
                    break;

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
                    if (_eventQueue.TryTake(out ServerEvent? @event, 500,
                        _eventQueueWaitBreaker.Token))
                        HandleServerEvent(@event);
                }
                catch (OperationCanceledException)
                {
                    /* Wątek UI canceluje _eventQueueWaitBreaker.Token, kiedy
                    chce wysłać żądanie do wątku Server.Process. */

                    HandleUIRequest();

                    /* Jeżeli token został zcancelowany przez wątek UI zlecający żądanie,
                    to tworzymy nowy token, bo próba wywołania _receiveQueue.TryTake z już
                    zcancelowanym tokenem od razu wyrzuci OperationCanceledException. */
                    _eventQueueWaitBreaker = new CancellationTokenSource();
                }
            }
            ClientStopped?.Invoke();
        }

        public void Enqueue(ServerEvent @event)
        {
            _eventQueue.Add(@event);
        }

        private void StartHandshake()
        {
            // Wątek Client.Process
            _remoteServer!.StartSenderAndReceiver();
            _remoteServer.IsRequestable = false;
            _remoteServer.IgnoreEvents = false;
            _remoteServer.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets
                .NoSlots_Or_IPAlreadyBlocked_Or_ServerIntroduction);
        }

        private ulong RandomUInt64()
        {
            return BitConverter.ToUInt64(RandomGenerator.Generate(Packet.TOKEN_SIZE));
        }

        private void HandleServerEvent(ServerEvent @event)
        {
            RemoteServer server = @event.Sender;
            if (server.IgnoreEvents)
                return;

            switch (@event.Type)
            {
                case ServerEvent.Types.SendSuccess:
                    OnSendSuccess(@event);
                    break;
                case ServerEvent.Types.SendError:
                    DisconnectThenNotify(server,
                        $"|sending error|; {((Exception)@event.Data!).Message}.");
                    break;
                case ServerEvent.Types.SendTimeout:
                    DisconnectThenNotify(server, "|timed out sending packet|.");
                    break;

                case ServerEvent.Types.ServerClosedSocket:
                    DisconnectThenNotify(server, "|disconnected (closed its socket)|.");
                    break;
                case ServerEvent.Types.ReceiveSuccess:
                    OnReceiveSuccess(@event);
                    break;
                case ServerEvent.Types.ReceiveError:
                    DisconnectThenNotify(server,
                        $"|reception error|; {((Exception)@event.Data!).Message}.");
                    break;
                case ServerEvent.Types.ReceiveTimeout:
                    OnReceiveTimeout(@event);
                    break;
            }
        }

        private void DisconnectThenNotify(RemoteServer server, string errorMsg)
        {
            // DisconnectThenRemoveServer
            server.IsRequestable = false;
            server.IgnoreEvents = true;
            // Synchroniczne rozłączanie
            server.Disconnect();
            // Możemy już nie mieć referencji do serwera, ale jeżeli jest, to ją usuwamy.
            _remoteServer = null;

            ServerEndedConnection?.Invoke(server, errorMsg);

            _stopRequested = true;
        }

        private void OnSendSuccess(ServerEvent @event)
        {
            RemoteServer server = @event.Sender;

            var code = (Packet.Codes)@event.Data!;

            switch (code)
            {
                case Packet.Codes.ClientIntroduction:
                case Packet.Codes.GetConversationsAndUsers:
                    // DisconnectThenNotify(server, operation.ToString());
                    break;
            }
        }

        private void OnReceiveSuccess(ServerEvent @event)
        {
            RemoteServer server = @event.Sender;
            byte[] packet = (byte[])@event.Data!;

            var receiveOrder = server.ReceiveOrder;
            if (receiveOrder is null)
            {
                // Nie oczekujemy żadnego pakietu.
                DisconnectThenNotify(server, "|sent a packet although| |client| |did not expect any|.");
                return;
            }

            var expectedPacket = receiveOrder.ExpectedPacket;
            if (packet.Length == 0)
            {
                // Odebraliśmy pakiet keep alive.
                ++server.ContiguousKeepAlivesCounter;

                // Oczekujemy keep alive lub powiadomienia od serwera.
                if ((expectedPacket == ReceivePacketOrder.ExpectedPackets.KeepAlive
                    || expectedPacket == ReceivePacketOrder.ExpectedPackets.Notification)
                    /* Resetujemy timeout oczekiwanego keep alive lub powiadomienia
                    i ponawiamy oczekiwanie. */
                    // Jeżeli false, to wystąpił timeout.
                    && !server.SetExpectedPacket(expectedPacket))
                    return;
                
                // Oczekujemy pakietu innego niż keep alive lub powiadomienie.
                if (server.ContiguousKeepAlivesCounter >= RemoteServer.CONTIGUOUS_KEEP_ALIVES_LIMIT)
                {
                    DisconnectThenNotify(server, $"|sent| {server.ContiguousKeepAlivesCounter} " +
                        "|'keep alive' packets in a row|.");
                }
                // Nie resetujemy timeoutu.
                return;
            }

            // Odebraliśmy pakiet nie keep alive.
            if (expectedPacket == ReceivePacketOrder.ExpectedPackets.Notification)
            {
                // Oczekujemy powiadomienia od serwera.
                HandleExpectedNotification(server, packet);
                return;
            }

            // Oczekujemy pakietu innego niż powiadomienie.
            switch (expectedPacket)
            {
                case ReceivePacketOrder.ExpectedPackets.KeepAlive:
                    // Nie odebraliśmy keep alive, więc rozłączamy.
                    DisconnectThenNotify(server, UnexpectedPacketErrorMsg);
                    break;
                case ReceivePacketOrder.ExpectedPackets
                    .NoSlots_Or_IPAlreadyBlocked_Or_ServerIntroduction:
                    if (!(HandleExpectedNoSlots_Or_IPAlreadyBlocked(server, packet)
                        || HandleExpectedServerIntroduction(server, packet)))
                        DisconnectThenNotify(server, UnexpectedPacketErrorMsg);
                    break;
                case ReceivePacketOrder.ExpectedPackets
                    .Authentication_Or_NoAuthentication_Or_AccountAlreadyBlocked:
                    HandleExpectedAuthentication_Or_NoAuthentication_Or_AccountAlreadyBlocked(
                        server, packet);
                    break;
            }
        }

        private const string UnexpectedPacketErrorMsg = "|sent| |an unexpected packet|.";

        #region Strict packets
        private bool HandleExpectedNoSlots_Or_IPAlreadyBlocked(RemoteServer server,
            byte[] packet)
        {
            // Pakiet już bez prefiksu, bo PacketReceiveBuffer go ucina.
            var pr = new PacketReader(packet);
            var operationCode = (Packet.Codes)pr.ReadUInt8();
            if (operationCode == Packet.Codes.NoSlots)
            {
                DisconnectThenNotify(server, "|has no free slots|.");
                return true;
            }

            if (operationCode == Packet.Codes.IPAlreadyBlocked)
            {
                DisconnectThenNotify(server, "|has blocked| |client's current IP address|.");
                return true;
            }
            /* Jeżeli w tej metodzie nie obsłużyliśmy pakietu, to zwracamy false, aby
            wykonać następną w kolejności metodę, która być może obsłuży pakiet. */
            return false;
        }

        private bool HandleExpectedServerIntroduction(RemoteServer server, byte[] packet)
        {
            var pr = new PacketReader(packet);
            var operationCode = (Packet.Codes)pr.ReadUInt8();
            if (operationCode != Packet.Codes.ServerIntroduction)
                return false;

            ServerIntroduction.Deserialize(pr, out Guid guid, out PublicKey publicKey,
                out ulong verificationToken);
            
            server.VerificationToken = verificationToken;

            /* Zapisujemy dane (kredki) serwera na potrzeby dalszego kontynuowania jego sesji.
            Zapamiętujemy seed, aby móc weryfikować pakiety wysłane przez serwer do klienta. */
            ulong localSeed = RandomUInt64();
            server.Introduce(guid, publicKey, localSeed);

            ServerIntroduced?.Invoke(server);
            /* Jeżeli zwróci false, to zdarzenie o timeoucie już jest w kolejce.
            Sygnalizujemy, że obsłużyliśmy pakiet. */
            server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.KeepAlive);
            return true;
        }

        private void HandleExpectedAuthentication_Or_NoAuthentication_Or_AccountAlreadyBlocked(
            RemoteServer server, byte[] packet)
        {
            var pr = new PacketReader(packet);
            try
            {
                var operationCode = ReadOperationCodeFromSignedEncryptedPacket(server, pr);
                if (operationCode is null)
                    return;

                switch (operationCode)
                {
                    case Packet.Codes.Authentication:
                        ulong remoteSeed = pr.ReadUInt64();
                        server.Authenticate(remoteSeed);
                        ServerHandshaken?.Invoke(server);
                        server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Notification);
                        break;
                    case Packet.Codes.NoAuthentication:
                        DisconnectThenNotify(server, "|refused to authenticate client|.");
                        break;
                    case Packet.Codes.AccountAlreadyBlocked:
                        DisconnectThenNotify(server, "|has blocked| |client's account|.");
                        break;
                    default:
                        DisconnectThenNotify(server, UnexpectedPacketErrorMsg);
                        break;
                }
            }
            catch (Error e) { DisconnectThenNotify(server, e.Message); }
        }
        #endregion

        private Packet.Codes? ReadOperationCodeFromSignedEncryptedPacket(
            RemoteServer server, PacketReader pr)
        {
            pr.Decrypt(_privateKey!);
            pr.VerifySignature(server.PublicKey!);

            var operationCode = (Packet.Codes)pr.ReadUInt8();

            if (server.VerifyReceivedToken(pr.ReadUInt64()))
                return operationCode;

            DisconnectThenNotify(server, "|sent| |an unrecognized token|.");
            return null;
        }

        #region Random notifications
        private void HandleExpectedNotification(RemoteServer server, byte[] packet)
        {
            var pr = new PacketReader(packet);
            try
            {
                var operationCode = ReadOperationCodeFromSignedEncryptedPacket(server, pr);
                if (operationCode is null)
                    return;

                switch (operationCode)
                {
                    case Packet.Codes.IPNowBlocked:
                        DisconnectThenNotify(server, "|now blocks| |client's current IP address|.");
                        break;
                    case Packet.Codes.ConversationsAndUsersList:
                        HandleReceivedConversationsAndUsersList(server, pr);
                        break;
                    case Packet.Codes.RequestError:
                        HandleReceivedRequestError(server, pr);
                        break;
                    default:
                        DisconnectThenNotify(server, UnexpectedPacketErrorMsg);
                        break;
                }
            }
            catch (Error e) { DisconnectThenNotify(server, e.Message); }
        }

        private void OnReceiveTimeout(ServerEvent @event)
        {
            RemoteServer server = @event.Sender;
            var order = (ReceivePacketOrder)@event.Data!;

            DisconnectThenNotify(server, "|timed out receiving packet| " +
                $"{order.ExpectedPacket}.");
        }
        
        private void HandleReceivedConversationsAndUsersList(RemoteServer server, PacketReader pr)
        {
            ConversationsAndUsersLists.Deserialize(pr, out var model);

            // Przypisujemy użytkowników jako właścicieli i uczestników konwersacji.
            var users = new Dictionary<ulong, User>();
            foreach (var user in model.Accounts)
            {
                // Nieprawdopodobne, że serwer wysłał zduplikowane Id użytkownika.
                users[user.Id] = new User
                {
                    Id = user.Id,
                    Login = user.Login,
                    PublicKey = user.PublicKey,
                    IsBlocked = user.IsBlocked != 0
                };
            }

            var conversations = new Conversation[model.ConversationParticipants.Length];
            for (int cp = 0; cp < model.ConversationParticipants.Length; ++cp)
            {
                var conversationParticipantModel = model.ConversationParticipants[cp];
                var conversationModel = conversationParticipantModel.Conversation;
                /* Nawet jeżeli właściciel nie znajduje się w żadnej konwersacji wysłanej przez serwer,
                to i tak powinien zostać przesłany. */
                var conversation = new Conversation
                {
                    Id = conversationModel.Id,
                    Owner = users[conversationModel.OwnerId],
                    Name = conversationModel.Name
                };
                
                foreach (var p in conversationParticipantModel.Participants)
                {
                    if (!users.ContainsKey(p.ParticipantId))
                        // Nieprawdopodobne: serwer wysłał id uczestnika, ale nie wysłał jego szczegółów.
                        throw new Error(
                            "|Server sent participant's id but did not send their details|.");
                    conversations[cp].Participations.Add(new ConversationParticipation
                    {
                        ConversationId = conversation.Id,
                        Conversation = conversation,
                        ParticipantId = p.ParticipantId,
                        Participant = users[p.ParticipantId],
                        JoinTime = DateTimeOffset.FromUnixTimeMilliseconds(p.JoinTime).UtcDateTime,
                        IsAdministrator = p.IsAdministrator != 0
                    });
                }

                conversations[cp] = conversation;
            }

            ReceivedConversationsAndUsersList?.Invoke(server, conversations);
        }

        private void HandleReceivedRequestError(RemoteServer server, PacketReader pr)
        {
            RequestError.Deserialize(pr, out ulong token, out Packet.Codes faultyOperationCode, out byte errorCode);
            switch (faultyOperationCode)
            {
                case AddConversation.CODE:
                    {
                        var err = (AddConversation.Errors)errorCode;
                        if (err != AddConversation.Errors.AccountDoesNotExist)
                            throw new Error(UnexpectedPacketErrorMsg);

                        ReceivedRequestError?.Invoke(server, "|Server does not know your account|.");
                        // Nie przerywamy, bo błąd jest "biznesowy", a nie protokołu.
                        server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Notification);
                    }
                    break;
            }
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
            // Wątek Client.Process
            /* Nie używamy monitor locka do uzyskiwania wyłącznego dostępu do
            _uiRequest, bo HandleUIRequest jest wykonywane tylko po wywołaniu
            przez wątek UI _eventQueueWaitBreaker.Cancel i wątek UI jest
            blokowany do momentu zakończenia obsługi żądania przez wątek
            Client.Process. */

            /* Nie ma żadnego żądania do obsłużenia - nieprawdopodobne, bo
            HandleUIRequest wykonuje się tylko po wykonaniu
            _eventQueueWaitBreaker.Cancel w RequestAndWait, które ustawia
            _uiRequest. */
            if (_uiRequest is null)
                return;

            switch (_uiRequest.Operation)
            {
                case UIRequest.Operations.Disconnect:
                    DisconnectUIRequest((RemoteServer)_uiRequest.Parameter!);
                    break;
                case UIRequest.Operations.IntroduceClient:
                    IntroduceClientUIRequest((RemoteServer)_uiRequest.Parameter!);
                    break;
                case UIRequest.Operations.GetConversations:
                    GetConversationsUIRequest((RemoteServer)_uiRequest.Parameter!);
                    break;
            }

            _uiRequest.Callback?.Invoke();
        }

        private void IntroduceClientUIRequest(RemoteServer server)
        {
            server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets
                .Authentication_Or_NoAuthentication_Or_AccountAlreadyBlocked);
            server.EnqueueToSend(ClientIntroduction.Serialize(_privateKey!, server.PublicKey!,
                _login!, server.VerificationToken, server.LocalSeed), ClientIntroduction.CODE);
        }

        private void DisconnectUIRequest(RemoteServer server)
        {
            DisconnectThenNotify(server, "|was disconnected|.");
        }

        private void GetConversationsUIRequest(RemoteServer server)
        {
            server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Notification);
            server.EnqueueToSend(GetConversationsAndUsers.Serialize(_privateKey!, server.PublicKey!,
                server.GenerateToken()), GetConversationsAndUsers.CODE);
        }
        #endregion
    }
}
