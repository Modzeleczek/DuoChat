using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking
{
    public abstract class Client
    {
        #region Classes
        private class PacketToSend
        {
            public byte[] Data { get; }
            private readonly object _monitorLock = new object();

            public PacketToSend(byte[] data) => Data = data;

            public void EnqueueAndWait(BlockingCollection<PacketToSend> queue)
            {
                Monitor.Enter(_monitorLock);
                queue.Add(this);

                /* Aktualny wątek zasypia i zwalnia sekcję krytyczną (monitor locka), aby wątek
                Client.Send mógł do niej wejść w metodzie PacketToSend.NotifyEnqueuer. Jeżeli
                w ciągu czasu timeout wątek Client.Send nie zajmie monitor locka i nie wykona
                na nim Monitor.Pulse, bo już wyszedł z nieskończonej pętli, to wątek zlecający
                wysłanie obudzi się. */
                bool packetSent = Monitor.Wait(_monitorLock, 1000);
                // Aktualny wątek budzi się i ostatecznie zwalnia sekcję krytyczną.
                Monitor.Exit(_monitorLock);
                if (!packetSent)
                    throw new Error("|Sending a packet timed out|.");
            }

            public void NotifyEnqueuer()
            {
                Monitor.Enter(_monitorLock);
                Monitor.Pulse(_monitorLock);
                Monitor.Exit(_monitorLock);
            }
        }
        #endregion

        #region Properties
        private volatile bool _stopRequested = false;
        public bool StopRequested
        {
            get => _stopRequested;
            private set => _stopRequested = value;
        }
        #endregion

        #region Fields
        public const int PREFIX_SIZE = sizeof(int);

        protected TcpClient _socket = null;
        protected Task _runner = null;
        // boole są flagami
        private volatile bool _disconnectRequested = false;

        private BlockingCollection<PacketToSend> _sendQueue = new BlockingCollection<PacketToSend>();
        private BlockingCollection<byte[]> _receiveQueue = new BlockingCollection<byte[]>();
        #endregion

        protected void ResetFlags()
        {
            StopRequested = false;
            _disconnectRequested = false;
        }

        protected void Process(Func<Result> processProtocol)
        {
            Result result = null;
            try
            {
                const TaskCreationOptions option = TaskCreationOptions.LongRunning;
                /* TODO: usunąć sendera, żeby wątek zlecający wysłanie pakietu sam go wysyłał -
                nie można, bo Client.Send, oprócz pakietów, wysyła okresowo keep-alive.
                Natomiast można usunąć 1 z 3 poniższych wątków (np. handler) i w wątku
                Client.Process wykonywać jego metodę (ProcessHandle), a po jej zakończeniu
                czekać na pozostałe wątki (Task.WaitAll(sender, receiver)). */
                var sender = Task.Factory.StartNew(ProcessSend, option);
                var receiver = Task.Factory.StartNew(ProcessReceive, option);
                var protocol = Task.Factory.StartNew(() =>
                {
                    var protocolResult = processProtocol();
                    StopProcessing();
                    return protocolResult;
                }, option);
                Task.WaitAll(sender, receiver, protocol);

                /* Rozłączenie przez nas - ignorujemy potencjalne błędy z procesów
                (sender, receiver, handler). Stan LostConnection służy do obsługi
                asynchronicznego rozłączenia przez rozmówcę, a nie przez nas. */
                if (_disconnectRequested)
                    result = new Cancellation();
                else
                    result = SpecifyConnectionEnding(sender.Result, receiver.Result, protocol.Result);
            }
            catch (Exception e)
            {
                result = new Failure(e, "|Error occured while| |executing Client.Process|.");
            }
            finally
            {
                _socket.Close();
                OnEndedConnection(result);
            }
        }

        protected abstract void OnEndedConnection(Result result);

        private Result SpecifyConnectionEnding(Result senderRes,
            Result receiverRes, Result handlerRes)
        {
            /* Wątek wysyłający złapał SocketException z Socket.Send -
            rozmówca (konkretnie odbiorca) wykonał Socket.Close lub zcrashował. */
            if (senderRes is InterlocutorFailure senderIF)
            {
                /* Wątek odbierający złapał SocketException z Socket.Receive -
                rozmówca (konkretnie nadawca) zcrashował. */
                if (receiverRes is InterlocutorFailure receiverIF)
                {
                    return receiverIF;
                }
                else // Wątek odbierający nie złapał SocketException.
                {
                    try
                    {
                        int receivedBytes = _socket.Client.Receive(
                            new byte[1], 0, 1, SocketFlags.None);
                        if (receivedBytes == 0)
                        {
                            // Rozmówca wykonał Socket.Close.
                            return new Success();
                        }
                        else
                        {
                            /* Rozmówca wysłał niezerową liczbę bajtów, więc chce
                            jeszcze rozmawiać. Rozłączenie jest z naszej inicjatywy. */
                            return new Cancellation();
                        }
                    }
                    catch (SocketException e)
                    {
                        // Rozmówca zcrashował.
                        return new InterlocutorFailure(e);
                    }
                }
            }
            else // Wątek wysyłający nie złapał SocketException.
            {
                // Wątek odbierający złapał SocketException.
                if (receiverRes is InterlocutorFailure receiverIF)
                {
                    /* Rozmówca zcrashował. Jeżeli wykonamy Socket.Send, to na pewno
                    wyrzuci SocketException, niezależnie czy rozmówca wykonał Socket.Close,
                    czy zcrashował. */
                    return receiverIF;
                }
                else // Wątek odbierający nie złapał SocketException.
                {
                    return SpecifyNonExceptionalEnding(senderRes, receiverRes, handlerRes);
                }
            }
        }

        private Result SpecifyNonExceptionalEnding(Result senderRes,
            Result receiverRes, Result handlerRes)
        {
            /* senderRes może być Cancellation lub Failure.
            receiverRes może być Cancellation, Failure lub Success.
            handlerRes może być Cancellation lub Failure. */

            /* Wątek odbierający w Socket.Receive odczytał 0 bajtów,
            czyli zdalny host (klient) się łagodnie rozłączył. */
            if (receiverRes is Success)
                return receiverRes;

            var error = new Error("|Connection broken.|");
            bool appendedErrorMsg = false;
            if (senderRes is Failure senderFail)
            {
                error.Append(senderFail.Reason.Message);
                appendedErrorMsg = true;
            }
            
            if (receiverRes is Failure receiverFail)
            {
                /* Error.Message wypisuje kolejne części komunikatu
                w oddzielnych liniach. */
                error.Append(receiverFail.Reason.Message);
                appendedErrorMsg = true;
            }

            if (handlerRes is Failure handlerFail)
            {
                error.Append(handlerFail.Reason.Message);
                appendedErrorMsg = true;
            }

            /* Rozłączenie przez błąd, ale nie wywołany przez SocketException,
            co zostało obsłużone w SpecifyConnectionEnding. */
            if (appendedErrorMsg)
                return new Failure(error.Message);

            /* W tym miejscu jest możliwe tylko, że sender-, receiver- i handlerRes są Cancellation.
            Rozłączenie przez _stopProcessing, ale bez żadnego błędu. Nie powinno się wydarzyć
            przy synchronicznym rozłączaniu, bo wtedy ustawiamy _disconnectRequested = true przed
            wykonaniem StopProcessing, więc SpecifyConnectionEnding w Process się nie wykonuje. */
            if (senderRes is Cancellation && receiverRes is Cancellation)
                return new Cancellation();

            // Nie powinno nigdy się wydarzyć.
            return new Failure("|Unexpected connection ending.|");
        }

        private Result ProcessSend()
        {
            var result = ProcessSendInner();
            StopProcessing();
            return result;
        }

        private Result ProcessSendInner()
        {
            var client = _socket.Client;
            if (client.SendBufferSize <= PREFIX_SIZE)
                return new Failure("|TCP socket's send buffer| |has insufficient capacity.|");
            var buffer = new byte[client.SendBufferSize];

            client.SendTimeout = 5000;

            while (true)
            {
                if (StopRequested)
                    return new Cancellation();

                int packetLength = 0, prefixValue = 0;
                if (_sendQueue.TryTake(out PacketToSend packet, 1000))
                {
                    var data = packet.Data;
                    packetLength = data.Length;
                    /* do wysyłania używamy ciągle tego samego bufora,
                    żeby nie zajmować dodatkowej pamięci */
                    if (PREFIX_SIZE + packetLength > client.SendBufferSize)
                        return new Failure(
                            "|Tried to send a packet larger than send buffer size.|");
                    /* jeżeli nadawca (host) ma kolejność bajtów little-endian (jak wiele Inteli),
                    to przerabiamy ją na big-endian, która jest konwencjonalna do wysyłania
                    przez internet; jeżeli host ma big-endian, to pozostaje bez zmian */
                    prefixValue = IPAddress.HostToNetworkOrder(packetLength);
                    Buffer.BlockCopy(data, 0, buffer, PREFIX_SIZE, packetLength);
                }
                /* else
                Wątek obudził się, bo od sekundy nic nie ma w kolejce do wysłania.
                Wysyłamy pakiet "keep alive" o rozmiarze 0 B - packetLength pozostaje 0. */

                /* kopiujemy do bufora prefiks o wartości 0 lub równy rozmiarowi
                pakietu w kolejności big-endian */
                // unsafe { Marshal.Copy((IntPtr)(&prefixValue), buffer, 0, PREFIX_SIZE); }

                // metoda bez unsafe
                for (int i = 0; i < PREFIX_SIZE; ++i)
                    buffer[i] = (byte)(prefixValue >> i);

                try
                {
                    /* przed pisaniem nie musimy sprawdzać, czy socket jest podłączony,
                    bo jeżeli nie jest, to client.Send wyrzuci wyjątek;
                    jeżeli odbiorca ma pełny bufor odbiorczy, to po czasie SendTimeout
                    Send wyrzuci SocketException; to znak dla nadawcy, że odbiorca jest przeciążony */
                    int totalSize = PREFIX_SIZE + packetLength;
                    int byteCount = client.Send(buffer, totalSize, SocketFlags.None);
                    if (byteCount != totalSize)
                        return new Failure("|Cannot send whole packet.|");
                }
                catch (SocketException e)
                {
                    return new InterlocutorFailure(e, "|Error occured while| " +
                        "|sending| |packet|.");
                }
                catch (ObjectDisposedException e)
                {
                    /* Według https://stackoverflow.com/a/1337117 Socket.Send i Receive
                    są wzajemnie thread-safe, więc nie musimy używać locka do synchronizowania
                    Send i Receive. Jeżeli w wątku odbierającym Receive spowoduje zdisposowanie
                    socketa, to wykonany po nim Send wyrzuci ObjectDisposedException lub odwrotnie.
                    Raczej nie powinno się wydarzyć, bo _socket.Close wykonujemy dopiero na końcu
                    Process, kiedy procesy (sender, receiver i handler) są już zakończone. */
                    return new Failure(e, "|Socket already disposed.|");
                }
                finally
                {
                    /* W finally, aby wykonało się po returnie z catcha, jeżeli Socket.Send rzuci
                    wyjątek. Bez tego wątek zlecający wysłanie pozostałby zablokowany przez metodę
                    Client.Send. */
                    // Jeżeli nie wysłaliśmy keep-alive.
                    if (!(packet is null))
                        packet.NotifyEnqueuer();
                }
            }
        }

        // uprzednio zbudowany (predefiniowany) pakiet
        public void Send(byte[] packet)
        {
            /* Trochę jak ViewModel.UIInvoke (Application.Current.Dispatcher.Invoke),
            bo wątek Client.ProcessSend kręci się w nieskończonej pętli, zgodnie ze wzorcem
            Dispatcher, i w metodzie Client.Send zlecamy mu wysłanie pakietu, co
            blokuje wątek zlecający do momentu wysłania, jak UIInvoke do momentu
            wykonania przekazanego mu kodu. */
            /* TODO: timeout, aby nie było sytuacji, że wątek nadający pakiet wrzuci go
            do kolejki i zablokuje się po tym, jak wątek Client.ProcessSend już wyszedł
            z pętli. */
            /* jeszcze dalsze TODO: wywalić wątek Client.ProcessSend i wysyłać wszystko
            w wątku Client.ProcessProtocol z timeoutem - jeżeli zostanie osiągnięty,
            to znaczy, że rozmówca ma pełny bufor odbiorczy. */
            var pts = new PacketToSend(packet);
            pts.EnqueueAndWait(_sendQueue);
        }

        private Result ProcessReceive()
        {
            var result = ProcessReceiveInner();
            StopProcessing();
            return result;
        }

        private Result ProcessReceiveInner()
        {
            var client = _socket.Client;
            if (client.ReceiveBufferSize <= PREFIX_SIZE) // może nie zmieścić się nawet sam prefiks
                return new Failure(
                    "|TCP socket's receive buffer| |has insufficient capacity.|");
            var receiveBuffer = new byte[client.ReceiveBufferSize]; // 65536
            var packetBuffer = new PacketBuffer();

            client.ReceiveTimeout = 5000;

            while (true)
            {
                if (StopRequested)
                    return new Cancellation();

                /* if (!IsSocketConnected(client))
                    return new Failure(interruptedMsg); */

                int receivedBytes;
                try
                {
                    receivedBytes = client.Receive(receiveBuffer, 0,
                        receiveBuffer.Length, SocketFlags.None);
                }
                /* client.Receive blokuje, czekając na dane i wyrzuca SocketException
                po przekroczeniu client.ReceiveTimeout */
                catch (SocketException e)
                {
                    return new InterlocutorFailure(e, "|Error occured while| " +
                        "|receiving| |packet|.");
                }
                catch (ObjectDisposedException e)
                {
                    // Patrz ProcessSendInner
                    return new Failure(e, "|Socket already disposed.|");
                }

                // odebrano 0 bajtów, co oznacza, że rozmówca bezpiecznie zamknął socket
                if (receivedBytes == 0)
                    return new Success();

                for (int i = 0; i < receivedBytes; ++i)
                {
                    if (!packetBuffer.Write(receiveBuffer[i]))
                        return new Failure(packetBuffer.ErrorMessage);

                    if (packetBuffer.PacketReady)
                    {
                        var packet = packetBuffer.FlushPacket();
                        if (packet.Length > 0)
                        {
                            if (!_receiveQueue.TryAdd(packet, 1000))
                                return new Failure("|Error occured while| " +
                                    "|adding| |received packet to queue.|");
                        }
#if DEBUG
                        // odebraliśmy pakiet "keep alive", który ignorujemy
                        else
                            LogReceivingKeepAlive();
#endif
                    }

                }
                /* jeżeli pakiet został sfabrykowany i ma prefiks o wartości większej od
                faktycznej liczby bajtów, to nie wykryjemy tego, bo część następnego pakietu
                zostanie potraktowana jako brakujące bajty aktualnego pakietu */
            }
        }

#if DEBUG
        protected void LogReceivingKeepAlive()
        {
            Debug.WriteLine("receive; keep alive");
        }
#endif

        public bool Receive(out byte[] packet, int millisecondsTimeout)
        {
            // Wątek Client.ProcessProtocol
            if (_receiveQueue.TryTake(out packet, millisecondsTimeout))
            {
#if DEBUG
                Debug.WriteLine($"receive; packet of length {packet.Length}, opcode " +
                    packet[0]);
#endif
                return true;
            }
            else // timeout
                return false;
        }

        protected void StopProcessing()
        {
            /* TODO: zrobić CancellationToken do przerywania TryTake
            w BlockingCollectionach i go tu cancelować. */
            StopRequested = true;
        }

        public Task DisconnectAsync()
        {
            /* Do interaktywnego (poprzez GUI) rozłączania należy używać
            tej funkcji (DisconnectAsync). Najpierw ustawiamy _disconnectRequested,
            aby w Process nie wykonało się SpecifyConnectionEnding. */
            _disconnectRequested = true;
            StopProcessing();
            return _runner;
        }
    }
}
