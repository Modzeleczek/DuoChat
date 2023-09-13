using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking
{
    public abstract class Client
    {
        public const int PREFIX_SIZE = sizeof(int);
        private const int OPERATION_CODE_SIZE = sizeof(byte);
        private const int ENCRYPTED_KEY_IV_SIZE = sizeof(short);

        #region Classes
        public class PacketToSend
        {
            public byte[] Data { get; }
            public Action Callback { get; }

            public PacketToSend(byte[] data, Action callback)
            {
                Data = data;
                Callback = callback;
            }
        }
        #endregion

        #region Properties
        public bool IsConnected { get; protected set; } = false;
        #endregion

        #region Fields
        protected TcpClient _socket = null;
        protected Task _runner = null;
        // boole są flagami
        private volatile bool _stopProcessing = false;
        private volatile bool _disconnectRequested = false;

        protected BlockingCollection<PacketToSend> _sendQueue = new BlockingCollection<PacketToSend>();
        protected BlockingCollection<byte[]> _receiveQueue = new BlockingCollection<byte[]>();
        #endregion

        #region Events
        public event Callback LostConnection, ReceivedPacket;

        // wywoływane, kiedy my się rozłączamy
        protected void OnLostConnection(Result result) => LostConnection?.Invoke(result);
        #endregion

        protected void ResetFlags()
        {
            _stopProcessing = false;
            _disconnectRequested = false;
            /* ustawiamy przed uruchomieniem Process, bo Process mógłby zakończyć się
            (i ustawić IsConnected = false) szybciej niż wykona się IsConnected = true */
            IsConnected = true;
        }

        protected void Process()
        {
            Result result;
            try
            {
                const TaskCreationOptions option = TaskCreationOptions.LongRunning;
                // TODO: usunąć sendera, żeby wątek zlecający wysłanie pakietu sam go wysyłał
                var sender = Task.Factory.StartNew(ProcessSend, option);
                var receiver = Task.Factory.StartNew(ProcessReceive, option);
                var handler = Task.Factory.StartNew(ProcessHandle, option);
                Task.WaitAll(sender, receiver, handler);

                /* Rozłączenie przez nas - ignorujemy potencjalne błędy z procesów
                (sender, receiver, handler). Event
                LostConnection służy do obsługi asynchronicznego rozłączenia przez
                rozmówcę, a nie przez nas. */
                if (_disconnectRequested)
                {
                    FinishConnection();
                    return;
                }

                result = SpecifyConnectionEnding(sender.Result, receiver.Result, handler.Result);
            }
            catch (Exception e)
            {
                result = new Failure(e, "|No translation:|");
            }
            FinishConnection();
            LostConnection?.Invoke(result);
        }

        private void FinishConnection()
        {
            _socket.Close();
            IsConnected = false;
        }

        private Result SpecifyConnectionEnding(Result senderRes, Result receiverRes, Result handlerRes)
        {
            const string interlocutorCrashed = "|Interlocutor crashed.|";
            /* Wątek wysyłający złapał SocketException z Socket.Send -
            rozmówca (konkretnie odbiorca) wykonał Socket.Close lub zcrashował. */
            if (senderRes is InterlocutorFailure senderIF)
            {
                /* Wątek odbierający złapał SocketException z Socket.Receive -
                rozmówca (konkretnie nadawca) zcrashował. */
                if (receiverRes is InterlocutorFailure receiverIF)
                {
                    return new Failure(interlocutorCrashed);
                }
                else // Wątek odbierający nie złapał SocketException.
                {
                    try
                    {
                        int receivedBytes = _socket.Client.Receive(new byte[1], 0, 1, SocketFlags.None);
                        if (receivedBytes == 0)
                        {
                            // Rozmówca wykonał Socket.Close.
                            return new Success();
                        }
                        else
                        {
                            /* Rozmówca wysłał niezerową liczbę bajtów, więc chce jeszcze rozmawiać.
                            Rozłączenie jest z naszej inicjatywy. */
                            return new Cancellation();
                        }
                    }
                    catch (SocketException)
                    {
                        // Rozmówca zcrashował.
                        return new Failure(interlocutorCrashed);
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
                    return new Failure(interlocutorCrashed);
                }
                else // Wątek odbierający nie złapał SocketException.
                {
                    return SpecifyNonExceptionalEnding(senderRes, receiverRes, handlerRes);
                }
            }
        }

        private Result SpecifyNonExceptionalEnding(Result senderRes, Result receiverRes, Result handlerRes)
        {
            /* senderRes może być Cancellation lub Failure.
            receiverRes może być Cancellation, Failure lub Success.
            handlerRes może być tylko Cancellation. */

            // Wątek odbierający w Socket.Receive odczytał 0 bajtów.
            if (receiverRes is Success)
            {
                return receiverRes;
            }

            var error = new Error("|Connection broken.|");
            bool appendedErrorMsg = false;
            if (senderRes is Failure senderFail)
            {
                error.Append(senderFail.Reason.Message);
                appendedErrorMsg = true;
            }
            
            if (receiverRes is Failure receiverFail)
            {
                /* Przyczyny kilku (dokładnie dwóch) niepowodzeń
                wypisujemy w oddzielnych liniach. */
                if (appendedErrorMsg)
                    error.Append("\n");
                error.Append(receiverFail.Reason.Message);
                appendedErrorMsg = true;
            }

            /* Rozłączenie przez błąd, ale nie wywołany przez SocketException,
            co zostało obsłużone w SpecifyConnectionEnding. */
            if (appendedErrorMsg)
                return new Failure(error.Message);

            /* W tym miejscu jest możliwe tylko, że senderRes i receiverRes są Cancellation.
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
                if (_stopProcessing)
                    return new Cancellation();

                int prefixValue = 0;
                if (_sendQueue.TryTake(out PacketToSend packet, 1000))
                {
                    var data = packet.Data;
                    /* do wysyłania używamy ciągle tego samego bufora,
                    żeby nie zajmować dodatkowej pamięci */
                    if (PREFIX_SIZE + data.Length > client.SendBufferSize)
                        return new Failure(
                            "|Tried to send a packet larger than send buffer size.|");
                    /* jeżeli nadawca (host) ma kolejność bajtów little-endian (jak wiele Inteli),
                    to przerabiamy ją na big-endian, która jest konwencjonalna do wysyłania
                    przez internet; jeżeli host ma big-endian, to pozostaje bez zmian */
                    prefixValue = IPAddress.HostToNetworkOrder(data.Length);
                    Buffer.BlockCopy(data, 0, buffer, PREFIX_SIZE, data.Length);
                }
                else
                    /* wątek obudził się, bo od sekundy nic nie ma w kolejce do wysłania;
                    wysyłamy pakiet "keep alive" o rozmiarze 0 B */
                    packet = new PacketToSend(new byte[0], null);

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
                    int totalSize = PREFIX_SIZE + packet.Data.Length;
                    int byteCount = client.Send(buffer, totalSize, SocketFlags.None);
                    if (byteCount != totalSize)
                        return new Failure("|Cannot send whole packet.|");
                }
                catch (SocketException e)
                {
                    return new InterlocutorFailure(e, "|No translation:|");
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

                /* jeżeli wysyłamy pakiet, a nie keep alive;
                nie wykona się, jeżeli Send wyrzuci wyjątek */
                packet.Callback?.Invoke();
            }
        }

        // uprzednio zbudowany (predefiniowany) pakiet
        protected void EnqueueToSend(byte[] packet, Action afterSendingCallback = null)
        {
            // blokujące dodawanie
            _sendQueue.Add(new PacketToSend(packet, afterSendingCallback));
#if DEBUG
            Debug.WriteLine($"server; enqueue to send; {BitConverter.ToString(packet)}");
#endif
        }

        // pakiet nieszyfrowany i nieautentykowany
        protected void EnqueueToSend(byte operationCode, PacketBuilder pb,
            Action afterSendingCallback = null)
        {
            pb.Prepend(operationCode, OPERATION_CODE_SIZE);
            EnqueueToSend(pb.Build(), afterSendingCallback);
        }

        // pakiet nieszyfrowany i autentykowany (RsaKey jest kluczem prywatnym (PrivateKey) nadawcy)
        // lub szyfrowany i nieautentykowany (RsaKey jest kluczem publicznym (PublicKey) odbiorcy)
        protected void EnqueueToSend(byte operationCode, PacketBuilder pb, RsaKey key,
            Action afterSendingCallback = null)
        {
            byte[] aesKey = GenerateAesKey(), aesIv = GenerateAesIv();
            try { pb.Encrypt(new Cryptography.Aes(aesKey, aesIv)); }
            catch (Error e)
            {
                e.Prepend("|Could not| |AES encrypt| |packet data|.");
                throw;
            }

            byte[] keyIv;
            try { keyIv = new Rsa(key).Encrypt(Merge(aesKey, aesIv)); }
            catch (Error e)
            {
                e.Prepend("|Could not| |RSA encrypt| |AES key and IV|.");
                throw;
            }
            pb.Prepend(keyIv);
            pb.Prepend(keyIv.Length, ENCRYPTED_KEY_IV_SIZE);

            EnqueueToSend(operationCode, pb, afterSendingCallback);
        }

        // private const int AES_KEY_SIZE = 128, AES_BLOCK_SIZE = 128;
        protected byte[] GenerateAesKey() => GenerateRandom(128 / 8);

        // 128 b - rozmiar bloku AESa (rozmiar w bajtach bloku, na które będzie dzielona wiadomość)
        protected byte[] GenerateAesIv() => GenerateRandom(128 / 8);

        private byte[] GenerateRandom(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }

        private byte[] Merge(byte[] a, byte[] b)
        {
            var ret = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, ret, 0, a.Length);
            Buffer.BlockCopy(b, 0, ret, a.Length, b.Length);
            return ret;
        }

        // pakiet szyfrowany i autentykowany
        protected void EnqueueToSend(byte operationCode, PacketBuilder pb,
            PrivateKey senderKey, PublicKey receiverKey, Action afterSendingCallback = null)
        {
            byte[] aesKey = GenerateAesKey(), aesIv = GenerateAesIv();
            try { pb.Encrypt(new Cryptography.Aes(aesKey, aesIv)); }
            catch (Error e)
            {
                e.Prepend("|Could not| |AES encrypt| |packet data|.");
                throw;
            }

            var rsa = new Rsa(senderKey);
            byte[] senderRsa;
            try { senderRsa = rsa.Encrypt(Merge(aesKey, aesIv)); }
            catch (Error e)
            {
                e.Prepend("|Could not| |RSA encrypt| |AES key and IV| " +
                    "|using sender's private key|.");
                throw;
            }

            rsa.Key = receiverKey;
            byte[] receiverRsa;
            try { receiverRsa = rsa.Encrypt(senderRsa); }
            catch (Error e)
            {
                e.Prepend("|Could not| |RSA encrypt| |AES key and IV| " +
                    "|using receivers's public key|.");
                throw;
            }

            var keyIv = receiverRsa;
            pb.Prepend(keyIv);
            pb.Prepend(keyIv.Length, ENCRYPTED_KEY_IV_SIZE);

            EnqueueToSend(operationCode, pb, afterSendingCallback);
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
                if (_stopProcessing)
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
                    return new InterlocutorFailure(e, "|No translation:|");
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

        private bool IsSocketConnected(Socket s)
        {
            // https://stackoverflow.com/a/14925438
            // return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
            // The long, but simpler-to-understand version:
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            // jeżeli Available == 0, to znaczy, że rozmówca bezpiecznie zakończył połączenie
            bool part2 = (s.Available == 0);
            if ((part1 && part2) || !s.Connected)
                return false;
            else
                return true;
        }

#if DEBUG
        protected void LogReceivingKeepAlive()
        {
            Debug.WriteLine("receive; keep alive");
        }
#endif

        private Result ProcessHandle()
        {
            var result = ProcessHandleInner();
            StopProcessing();
            return result;
        }

        private Result ProcessHandleInner()
        {
            /* handler, podczas interpretowania requesta "edytującego", czyli takiego,
            który wymaga zapisu do bazy danych, musi mieć writer-locka na bazę danych od momentu
            rozpoczęcia zapisu bazy danych do momentu umieszczenia w kolejce do wysłania obiektu
            powiadomienia, aby zachować ciągłość zmian w bazie danych; dzięki temu jest gwarancja,
            że kolejne powiadomienia wysyłane do klientów reprezentują kolejne stany bazy danych
            i nie może wystąpić sytuacja, że najpierw zostanie wysłane powiadomienie z aktualnym
            stanem, a po nim powiadomienie z już nieaktualnym stanem i klient zinterpretuje to
            ostatnie powiadomienie jako aktualny stan */
            while (true)
            {
                if (_stopProcessing)
                    return new Cancellation();

                if (!_receiveQueue.TryTake(out byte[] packet, 1000))
                    continue;

                HandlePacket(packet);
            }
        }

        protected void StopProcessing()
        {
            _stopProcessing = true;
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

        private void HandlePacket(byte[] packet)
        {
            ReceivedPacket?.Invoke(new Success(packet));
#if DEBUG
            Debug.WriteLine($"handle; packet of length {packet.Length}, opcode " +
                packet[0]);
#endif
        }
    }
}
