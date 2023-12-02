using Client.MVVM.Model.Networking.PacketOrders;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.Model.Networking.Packets;
using Shared.MVVM.Model.Networking.Reception;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client.MVVM.Model.Networking
{
    public class RemoteServer
    {
        #region Fields and properties
        // Do użytku przez serwer
        public const int CONTIGUOUS_KEEP_ALIVES_LIMIT = 3;
        private const int SEND_TIMEOUT_MILLISECONDS = 1000;
        private const int RECEIVE_TIMEOUT_MILLISECONDS = 1000;

        private readonly IPEndPoint _remoteEndPoint;
        public ReceivePacketOrder? ReceiveOrder { get; private set; } = null;
        public ulong VerificationToken { get; set; }
        public bool IsRequestable { get; set; }
        // Jeżeli true, to wątek Client.Process ma ignorować zdarzenia od serwera.
        public bool IgnoreEvents { get; set; }
        public int ContiguousKeepAlivesCounter { get; set; } = 0;

        // Dane zdalnego hosta ustawiane jednorazowo w metodzie Introduce.
        public Guid? Guid { get; private set; } = null;
        public PublicKey? PublicKey { get; private set; } = null;
        public ulong LocalSeed { get; private set; } = 0;

        // Ustawiane jednorazowo w metodzie Authenticate.
        private ulong? _remoteSeed = null;

        private readonly BlockingCollection<SendPacketOrder> _sendQueue =
            new BlockingCollection<SendPacketOrder>();
        private readonly object _sendQueueCompleteAddingLock = new object();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        // Wczesna inicjalizacja
        private Task? _senderTask = null;
        private Task? _receiverTask = null;
        private readonly TcpClient _tcpClient;
        private readonly IEventProcessor _eventProcessor;
        private readonly PacketSendBuffer _sendBuffer = new PacketSendBuffer();
        private readonly PacketReceiveBuffer _receiveBuffer = new PacketReceiveBuffer();
        #endregion

        public RemoteServer(TcpClient tcpClient, IEventProcessor eventProcessor)
        {
            _tcpClient = tcpClient;
            if (_tcpClient.SendBufferSize < 1)
                throw new InsufficientMemoryException("TCP socket's send buffer has insufficient capacity.");
            // Nic nie robi, kiedy korzystamy z Socket.SendAsync.
            _tcpClient.SendTimeout = RECEIVE_TIMEOUT_MILLISECONDS;

            if (_tcpClient.ReceiveBufferSize < 1)
                throw new InsufficientMemoryException("TCP socket's receive buffer has insufficient capacity.");
            // Nic nie robi, kiedy korzystamy z Socket.ReceiveAsync.
            _tcpClient.ReceiveTimeout = RECEIVE_TIMEOUT_MILLISECONDS;

            _remoteEndPoint = (IPEndPoint)_tcpClient.Client.RemoteEndPoint!;
            _eventProcessor = eventProcessor;
        }

        public ServerPrimaryKey GetPrimaryKey()
        {
            // Klucz główny podłączonego serwera jest tylko do odczytu.
            var re = _remoteEndPoint;
            return new ServerPrimaryKey(new IPv4Address(re.Address), new Port((ushort)re.Port));
        }

        public void Introduce(Guid guid, PublicKey publicKey, ulong localSeed)
        {
            if (!(Guid is null))
                // Nieprawdopodobne
                throw new InvalidOperationException("Server is already introduced.");

            Guid = guid;
            PublicKey = publicKey;
            LocalSeed = localSeed;
        }

        public void Authenticate(ulong remoteSeed)
        {
            if (!(_remoteSeed is null))
                // Nieprawdopodobne
                throw new InvalidOperationException("Server has already authenticated client.");

            _remoteSeed = remoteSeed;
        }

        public void StartSenderAndReceiver()
        {
            if (!(_senderTask is null))
                // Nieprawdopodobne
                throw new InvalidOperationException("Sender has already been started.");

            if (!(_receiverTask is null))
                // Nieprawdopodobne
                throw new InvalidOperationException("Receiver has already been started.");

            const TaskCreationOptions longRunning = TaskCreationOptions.LongRunning;
            _senderTask = Task.Factory.StartNew(Sender, longRunning);
            _receiverTask = Task.Factory.StartNew(Receiver, longRunning);
        }

        #region Sender
        private void Sender()
        {
            /* Jeżeli u Sendera wystąpi błąd, to wrzuca event o tym do serverEventQueue
            i serwer informuje Receivera lub odwrotnie. Serwer jest mediatorem. */

            byte[] keepAlivePacket = new byte[0];
            SendPacketOrder? order = null;
            try
            {
                while (true)
                {
                    if (_sendQueue.TryTake(out order, 500, _cts.Token))
                    {
                        // Pobraliśmy rozkaz z kolejki.
                        _sendBuffer.SendUntilCompletedOrInterrupted(_tcpClient.Client,
                            _cts.Token, order.Packet);

                        // Jeżeli TryMarkAsDone zwróci true, to znaczy, że wysyłanie zakończyło się przed timeoutem.
                        if (order.TryMarkAsDone())
                            _eventProcessor.Enqueue(new ServerEvent(ServerEvent.Types.SendSuccess, this, order.Code));
                        /* Jeżeli false, to wystąpił timeout i w kolejce zdarzeń jest już zdarzenie o timeoucie.
                        Ignorujemy wysłanie. */
                    }
                    else
                    {
                        /* Timeout pobrania rozkazu z kolejki, więc wysyłamy pakiet keep alive
                        o rozmiarze 0 B. */
                        _sendBuffer.SendUntilCompletedOrInterrupted(_tcpClient.Client,
                            _cts.Token, keepAlivePacket);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancel przed lub podczas wywołania TryTake
                /* Poprzedni rozkaz (order) został zakończony,
                a następnego jeszcze nie wyjęliśmy z kolejki. */
            }
            catch (Exception e) when (
                e is AggregateException ae &&
                    // Cancel przed wywołaniem Socket.SendAsync
                    (ae.InnerException is TaskCanceledException
                    // Cancel podczas wywołania Socket.SendAsync
                    || ae.InnerException is OperationCanceledException))
            {
                // Client.Process lub wątek timeoutujący zcancelował CTS.
                /* Tylko anulujemy timeout (lub nie, jeżeli już wątek timeoutujący
                wykonał TryMarkAsTimedOut, ale przynajmniej próbujemy). */
                order!.TryMarkAsDone();
            }
            catch (Exception e)
            {
                // Inny błąd
                if (order!.TryMarkAsDone())
                    _eventProcessor.Enqueue(new ServerEvent(ServerEvent.Types.SendError, this, e));
            }
            /* Na koniec wątku Sendera zamykamy kolejkę i discardujemy
            pozostałe rozkazy, dodatkowo anulując im timeouty. */
            CompleteSendQueueAddition();
        }

        private void CompleteSendQueueAddition()
        {
            lock (_sendQueueCompleteAddingLock)
                _sendQueue.CompleteAdding();

            // Usuwamy rozkazy pozostałe w kolejce.
            while (_sendQueue.TryTake(out SendPacketOrder? item))
                item.TryMarkAsDone();
        }
         
        public bool EnqueueToSend(byte[] packet, Packet.Codes code)
        {
            // Wątek Client.Process
            var order = new SendPacketOrder(packet, code);
            lock (_sendQueueCompleteAddingLock)
            {
                if (_sendQueue.IsCompleted)
                    // Zwracamy false, jeżeli kolejka już jest zamknięta na dodawanie.
                    return false;
                _sendQueue.Add(order);
            }

            StartTimeoutTaskIfNeeded(order, SEND_TIMEOUT_MILLISECONDS,
                ServerEvent.Types.SendTimeout, null);
            return true;
        }
        #endregion

        #region Receiver
        private void Receiver()
        {
            try
            {
                while (true)
                {
                    byte[]? packet = _receiveBuffer.ReceiveUntilCompletedOrInterrupted(
                        new SocketWrapper(_tcpClient.Client), _cts.Token);
                    ServerEvent @event;
                    if (packet is null)
                        @event = new ServerEvent(ServerEvent.Types.ServerClosedSocket, this);
                    else
                        @event = new ServerEvent(ServerEvent.Types.ReceiveSuccess, this, packet);
                    _eventProcessor.Enqueue(@event);
                }
            }
            catch (Exception e) when (
                e is AggregateException ae &&
                    // Cancel przed wywołaniem Socket.ReceiveAsync
                    (ae.InnerException is TaskCanceledException
                    // Cancel podczas wywołania Socket.ReceiveAsync
                    || ae.InnerException is OperationCanceledException))
            {
                // Client.Process lub wątek timeoutujący zcancelował CTS.
            }
            catch (Exception e)
            {
                // Inny błąd
                _eventProcessor.Enqueue(new ServerEvent(ServerEvent.Types.ReceiveError, this, e));
            }
        }

        public bool SetExpectedPacket(ReceivePacketOrder.ExpectedPackets expectedPacket,
            int millisecondsTimeout = RECEIVE_TIMEOUT_MILLISECONDS)
        {
            // Wątek Client.Process
            // Już czegoś oczekujemy i jest ustawione odliczanie timeoutu.
            if (!(ReceiveOrder is null)
                // Próbujemy odwołać aktualny timeout.
                && !ReceiveOrder.TryMarkAsDone())
                /* Wystąpił timeout i task timeoutujący już dodał do kolejki
                zdarzenie o timeoucie. */
                return false;

            /* Jeszcze niczego nie oczekujemy i nie ma timeoutu lub czegoś już oczekujemy i udało
            się odwołać timeout (kolejne wywołanie SetExpectedPacket wystąpiło przed timeoutem). */

            ReceiveOrder = new ReceivePacketOrder(expectedPacket);
            ContiguousKeepAlivesCounter = 0;

            StartTimeoutTaskIfNeeded(ReceiveOrder, millisecondsTimeout,
                ServerEvent.Types.ReceiveTimeout, ReceiveOrder);
            return true;
        }
        #endregion

        private void StartTimeoutTaskIfNeeded(TimeoutableOrder order, int millisecondsTimeout,
            ServerEvent.Types timeoutEventType, object? data)
        {
            if (millisecondsTimeout == Timeout.Infinite)
                return;

            /* Zaczyna mierzyć czas w momencie zakolejkowania rozkazu wysłania,
            a nie wyjęcia go z kolejki (lub ustawienia oczekiwanego pakietu do
            odebrania, a nie rozpoczęcia jego odbierania). */
            Task.Factory.StartNew(() =>
            {
                try
                {
                    // Task.Delay można zcancelować.
                    Task.Delay(millisecondsTimeout, order.GetCancellationToken()).Wait();

                    if (order.TryMarkAsTimedOut())
                    {
                        // Wystąpił timeout.
                        // Cancelujemy Sender i Receiver.
                        _cts.Cancel();
                        _eventProcessor.Enqueue(new ServerEvent(timeoutEventType, this, data));
                    }
                    /* Jeżeli TryMarkAsTimedOut zwróci false, to znaczy, że wysyłanie
                    (lub odbieranie pakietu + interpretacja przez wątek Client.Process)
                    pakietu zakończyło się przed timeoutem, ale już po zwróceniu sterowania
                    z Wait. Wówczas pomijamy timeout. */
                }
                /* Wysyłanie (lub odbieranie pakietu + interpretacja przez wątek Client.Process)
                zakończyło się przed lub podczas Wait. */
                catch (AggregateException e) when (e.InnerException is TaskCanceledException) { }
            });
        }

        public void Disconnect()
        {
            // Wątek Client.Process
            _cts.Cancel();
            _tcpClient.Client?.Shutdown(SocketShutdown.Both);
            _tcpClient.Close();

            // Synchroniczne rozłączanie - czekamy na całkowite zakończenie wątków serwera.
            Task.WaitAll(_senderTask, _receiverTask);
        }

        public override string ToString()
        {
            string keyString = GetPrimaryKey().ToString();
            return keyString + (!(Guid is null) ? $" ({Guid})" : string.Empty);
        }
        
        public ulong GenerateToken()
        {
            return LocalSeed++;
        }

        public bool VerifyReceivedToken(ulong token)
        {
            return (_remoteSeed++) == token;
        }
    }
}
