using Shared.MVVM.Model.Cryptography;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Shared.MVVM.Core;
using System.Threading;
using System.Collections.Concurrent;
using Client.MVVM.Model.Networking.PacketOrders;
using Client.MVVM.ViewModel.Observables;
using System.Collections.Generic;
using Shared.MVVM.Model.Networking.Packets;
using Shared.MVVM.Model.Networking.Packets.ServerToClient;
using Shared.MVVM.Model.Networking.Packets.ClientToServer;
using Shared.MVVM.Model;
using Client.MVVM.Model.Networking.UIRequests;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using System.Reflection;
using System.Diagnostics;

namespace Client.MVVM.Model.Networking
{
    // Klasa odpowiada za cały stan klienta i zarządzanie połączonym serwerem.
    public class ClientMonolith : IEventProcessor
    {
        #region Fields
        // Stałe
        private const int CONNECT_TIMEOUT_SECONDS = 2;

        // Dane lokalnego hosta
        private string? _login = null;
        private PrivateKey? _privateKey = null;

        // Zdalny host
        private RemoteServer? _remoteServer = null;

        // Do przebiegu sterowania
        private readonly Task _clientProcessTask;
        private bool _stopRequested = false;
        private readonly BlockingCollection<ServerEvent> _eventQueue =
            new BlockingCollection<ServerEvent>();
        // Wczesna (eager) inicjalizacja
        private CancellationTokenSource _eventQueueWaitBreaker = new CancellationTokenSource();
        private UIRequest? _uiRequest = null;
        #endregion

        #region Events
        public event Action<RemoteServer>? ServerIntroduced;
        public event Action<RemoteServer, Conversation[]>? ReceivedConversationsAndUsersList;
        public event Action<RemoteServer, string>? ReceivedRequestError;
        public event Action<RemoteServer, string>? ServerEndedConnection;
        #endregion

        public ClientMonolith()
        {
            // Uruchamiamy wątek Client.Process.
            _clientProcessTask = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
        }

        private void Process()
        {
            // Wątek Client.Process
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

                    /* Jeżeli token został zcancelowany przez wątek UI zlecający żądanie,
                    to tworzymy nowy token, bo próba wywołania _receiveQueue.TryTake z już
                    zcancelowanym tokenem od razu wyrzuci OperationCanceledException. */
                    _eventQueueWaitBreaker = new CancellationTokenSource();

                    HandleUIRequest();
                }
            }
        }

        public void Enqueue(ServerEvent @event)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}");

            _eventQueue.Add(@event);
        }

        private void StartHandshake()
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}");

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

            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}");

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
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}, {errorMsg}");

            // DisconnectThenRemoveServer
            server.IsRequestable = false;
            server.IgnoreEvents = true;
            // Synchroniczne rozłączanie
            server.Disconnect();
            // Możemy już nie mieć referencji do serwera, ale jeżeli jest, to ją usuwamy.
            _remoteServer = null;

            ServerEndedConnection?.Invoke(server, errorMsg);
        }

        private void OnSendSuccess(ServerEvent @event)
        {
            RemoteServer server = @event.Sender;

            var code = (Packet.Codes)@event.Data!;

            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}, {code}");

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

            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}, {packet.ToHexString()}");

            var receiveOrder = server.ReceiveOrder;
            if (receiveOrder is null)
            {
                // Nie oczekujemy żadnego pakietu.
                DisconnectThenNotify(server, "|sent a packet although| |client| |did not expect any|.");
                return;
            }

            var expectedPacket = receiveOrder.ExpectedPacket;

            Debug.WriteLine($"{receiveOrder}, {expectedPacket}, {packet.Length}, {packet.ToHexString()}");

            if (packet.Length == 0)
            {
                // Odebraliśmy pakiet keep alive.

                // Oczekujemy keep alive lub powiadomienia od serwera.
                if (expectedPacket == ReceivePacketOrder.ExpectedPackets.KeepAlive
                    || expectedPacket == ReceivePacketOrder.ExpectedPackets.Notification)
                {
                    /* Resetujemy timeout oczekiwanego keep alive lub powiadomienia
                    i ponawiamy oczekiwanie. */
                    // Jeżeli false, to wystąpił timeout - zdarzenie o nim jest już w kolejce.
                    Debug.WriteLine($"{nameof(OnReceiveSuccess)}, received keep alive and keep alive or notification expected");
                    server.SetExpectedPacket(expectedPacket);
                    return;
                }

                // Oczekujemy pakietu innego niż keep alive lub powiadomienie.
                // Nie resetujemy timeoutu.
                return;
            }

            // Odebraliśmy pakiet nie keep alive.
            // Musimy zresetować timeout gdzieś w (bez)pośrednio wywołanej tu metodzie.
            if (expectedPacket == ReceivePacketOrder.ExpectedPackets.Notification)
            {
                // Oczekujemy powiadomienia od serwera.
                HandleExpectedNotification(server, packet);
                return;
            }

            // Oczekujemy pakietu innego niż powiadomienie.
            switch (expectedPacket)
            {
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
                default: // Oczekujemy np. KeepAlive, ale go nie odebraliśmy.
                    DisconnectThenNotify(server, UnexpectedPacketErrorMsg);
                    break;
            }
        }

        private const string UnexpectedPacketErrorMsg = "|sent| |an unexpected packet|.";

        #region Strict packets
        private bool HandleExpectedNoSlots_Or_IPAlreadyBlocked(RemoteServer server,
            byte[] packet)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}, {packet.ToHexString()}");

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
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}, {packet.ToHexString()}");

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

            /* Jeżeli zwróci false, to zdarzenie o timeoucie już jest w kolejce.
            Sygnalizujemy, że obsłużyliśmy pakiet. Serwer ma timeout 10 sekund, kiedy czeka na
            ClientIntroduction. */
            server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.KeepAlive, Timeout.Infinite);
            /* Blokujemy bieżący wątek (Client.Process) w handlerze MainViewModel.OnServerIntroduced
            -> AskIfServerTrusted, więc SetExpectedPacket(KeepAlive) musi być przed eventem
            ServerIntroduced. */
            ServerIntroduced?.Invoke(server);
            return true;
        }

        private void HandleExpectedAuthentication_Or_NoAuthentication_Or_AccountAlreadyBlocked(
            RemoteServer server, byte[] packet)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}, {packet.ToHexString()}");

            var pr = new PacketReader(packet);
            try
            {
                var operationCode = ReadOperationCodeFromSignedEncryptedPacket(server, pr);
                if (operationCode is null)
                    return;

                switch (operationCode)
                {
                    case Packet.Codes.Authentication:
                        HandleExpectedAuthentication(server, pr);
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

        private void HandleExpectedAuthentication(RemoteServer server, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}");

            Authentication.Deserialize(pr, out ulong remoteSeed, out ulong accountId);

            server.Authenticate(remoteSeed, accountId);
            server.IsRequestable = true;

            /* Jesteśmy po uścisku dłoni, więc pobieramy konwersacje z serwera.
            Odpowiedź zamierzamy dostać w HandleReceivedConversationsAndUsersList. */
            server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Notification);
            server.EnqueueToSend(GetConversationsAndUsers.Serialize(_privateKey!, server.PublicKey!,
                server.GenerateToken()), GetConversationsAndUsers.CODE);
        }
        #endregion

        private Packet.Codes? ReadOperationCodeFromSignedEncryptedPacket(
            RemoteServer server, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}");

            pr.Decrypt(_privateKey!);
            if (!pr.VerifySignature(server.PublicKey!))
            {
                DisconnectThenNotify(server, "|sent| |invalid packet signature|.");
                return null;
            }

            var operationCode = (Packet.Codes)pr.ReadUInt8();

            if (server.VerifyReceivedToken(pr.ReadUInt64()))
                return operationCode;

            DisconnectThenNotify(server, "|sent| |an unrecognized token|.");
            return null;
        }

        #region Random notifications
        private void HandleExpectedNotification(RemoteServer server, byte[] packet)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}, {packet.ToHexString()}");

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
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {@event.ToDebugString()}");

            RemoteServer server = @event.Sender;
            var order = (ReceivePacketOrder)@event.Data!;

            DisconnectThenNotify(server, "|timed out receiving packet| " +
                $"{order.ExpectedPacket}.");
        }
        
        private void HandleReceivedConversationsAndUsersList(RemoteServer server, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}");

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
                    conversation.Participations.Add(new ConversationParticipation
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
            server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets.Notification);
        }

        private void HandleReceivedRequestError(RemoteServer server, PacketReader pr)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {server}");

            RequestError.Deserialize(pr, out Packet.Codes faultyOperationCode, out byte errorCode);

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
            // Zawsze w tej metodzie musi zostać wykonane server.SetExpectedPacket.
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

            // TODO: sprawdzić, czy bez tego timeout żądania IntroduceClient rozłączy klienta po sekundzie od połączenia
            if (!_uiRequest.TryMarkAsDone())
                /* Jeżeli nie uda się oznaczyć jako wykonane, to znaczy, że
                nastąpił timeout. Wówczas nie wykonujemy żądania. */
                return;

            switch (_uiRequest)
            {
                case Connect connect:
                    ConnectUIRequest(connect);
                    break;
                case IntroduceClient introduceClient:
                    IntroduceClientUIRequest(introduceClient);
                    break;
                case Disconnect disconnect:
                    DisconnectUIRequest(disconnect);
                    break;
                case StopProcess stopProcess:
                    StopProcessUIRequest(stopProcess);
                    break;
            }
        }

        private void ConnectUIRequest(Connect request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.PrivateKey}");

            // Wątek Client.Process
            ServerPrimaryKey serverKey = request.ServerKey;
            if (!(_remoteServer is null))
            {
                // Jesteśmy już połączeni, więc rozłączamy.
                DisconnectUIRequest(new Disconnect(serverKey, null));
            }

            _login = request.Login;
            _privateKey = request.PrivateKey;
            
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
                    var connectingTask = tcpClient.ConnectAsync(
                        serverKey.IpAddress.ToIPAddress(), serverKey.Port.Value);
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

                // Tworzymy nakładkę RemoteServer na Socket.
                /* TODO: fejkowe "factory" zwracające ciągle ten sam obiekt RemoteServer,
                tylko ze zresetowanym stanem (wyczyszczone bufory itp.). */
                _remoteServer = new RemoteServer(tcpClient, this);
                StartHandshake();

                request.Callback.Invoke(null);
                return;
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
            tcpClient.Close();
            _remoteServer = null;

            request.Callback.Invoke(error.Message);
        }

        private void IntroduceClientUIRequest(IntroduceClient request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.ServerKey}");

            ServerPrimaryKey serverKey = request.ServerKey;
            if (_remoteServer is null || !serverKey.Equals(_remoteServer.GetPrimaryKey()))
                return;
            RemoteServer server = _remoteServer;

            server.SetExpectedPacket(ReceivePacketOrder.ExpectedPackets
                .Authentication_Or_NoAuthentication_Or_AccountAlreadyBlocked);
            server.EnqueueToSend(ClientIntroduction.Serialize(_privateKey!, server.PublicKey!,
                _login!, server.VerificationToken, server.LocalSeed), ClientIntroduction.CODE);
        }

        private void DisconnectUIRequest(Disconnect request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.ServerKey}");

            ServerPrimaryKey serverKey = request.ServerKey;
            if (_remoteServer is null || !serverKey.Equals(_remoteServer.GetPrimaryKey()))
            {
                request.Callback?.Invoke();
                return;
            }
            RemoteServer remoteServer = _remoteServer;

            DisconnectThenNotify(remoteServer, "|was disconnected|.");

            request.Callback?.Invoke();
        }

        private void StopProcessUIRequest(StopProcess request)
        {
            Debug.WriteLine($"{MethodBase.GetCurrentMethod().Name}, {request.GetType().Name}");

            // Wątek Client.Process
            /* Pętla w Process zakończy się natychmiast po ustawieniu
            tego i powrocie ze StopProcessUIRequest. */
            _stopRequested = true;

            request.Callback?.Invoke();
        }
        #endregion
    }
}
