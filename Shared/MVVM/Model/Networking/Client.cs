using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        protected Task<Result> _runner = null;
        protected volatile bool _disconnectRequested = false;

        protected BlockingCollection<PacketToSend> _sendQueue = new BlockingCollection<PacketToSend>();
        protected BlockingCollection<byte[]> _receiveQueue = new BlockingCollection<byte[]>();
        #endregion

        #region Events
        public event Callback Disconnected, ReceivedPacket;

        protected void OnDisconnected(Result result) => Disconnected?.Invoke(result);
        #endregion

        protected void ResetFlags()
        {
            _disconnectRequested = false;
            /* ustawiamy przed uruchomieniem Process, bo Process mógłby zakończyć się
            (i ustawić IsConnected = false) szybciej niż wykona się IsConnected = true */
            IsConnected = true;
        }

        protected Result Process()
        {
            Result result = null;
            try
            {
                const TaskCreationOptions option = TaskCreationOptions.LongRunning;
                // TODO: usunąć sendera, żeby wątek zlecający wysłanie pakietu sam go wysyłał
                var sender = Task.Factory.StartNew(ProcessSend, option);
                var receiver = Task.Factory.StartNew(ProcessReceive, option);
                var handler = Task.Factory.StartNew(ProcessHandle, option);
                Task.WaitAll(sender, receiver, handler);

                var results = new Result[] { sender.Result, receiver.Result, handler.Result };
                var error = new Error("|Lost connection to the server.|");
                bool appendedErrorMsg = false;
                foreach (var res in results)
                {
                    if (res is Failure failure)
                    {
                        // Przyczyny kilku niepowodzeń wypisujemy w oddzielnych liniach.
                        if (appendedErrorMsg)
                            error.Append("\n");
                        error.Append(failure.Reason.Message);
                        appendedErrorMsg = true;
                    }
                }
                if (appendedErrorMsg)
                    result = new Failure(error.Message);
                else
                    // Jeżeli nie było żadnego niepowodzenia.
                    result = new Success();
            }
            catch (Exception e)
            { result = new Failure(e, "|No translation:|"); }
            finally
            {
                _socket.Close();
                IsConnected = false;
            }
            return result;
        }

        private Result ProcessSend()
        {
            var result = ProcessSendInner();
            RequestDisconnect();
            return result;
        }

        private Result ProcessSendInner()
        {
            var client = _socket.Client;
            if (client.SendBufferSize <= PREFIX_SIZE)
                return new Failure("|TCP socket's send buffer| |has insufficient capacity.|");
            var buffer = new byte[client.SendBufferSize];

            client.SendTimeout = 5000;

            var interruptedMsg = "|Connection interrupted.|";
            while (true)
            {
                if (_disconnectRequested)
                    return new Cancellation();

                int prefixValue = 0;
                if (_sendQueue.TryTake(out PacketToSend packet, 1000))
                {
                    var data = packet.Data;
                    /* do wysyłania używamy ciągle tego samego bufora,
                    żeby nie zajmować dodatkowej pamięci */
                    if (4 + data.Length > client.SendBufferSize)
                        return new Failure(
                            "|Tried to send a packet larger than send buffer size.|");
                    /* jeżeli nadawca (host) ma kolejność bajtów little-endian (jak wiele Inteli),
                    to przerabiamy ją na big-endian, która jest konwencjonalna do wysyłania
                    przez internet; jeżeli host ma big-endian, to pozostaje bez zmian */
                    prefixValue = IPAddress.HostToNetworkOrder(data.Length);
                    Buffer.BlockCopy(packet.Data, 0, buffer, PREFIX_SIZE, data.Length);
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
                    client.Send(buffer, PREFIX_SIZE + packet.Data.Length, SocketFlags.None);
                }
                catch (SocketException e)
                {
                    return new Failure(e, interruptedMsg, "|Operating system error.|");
                }
                catch (ObjectDisposedException e)
                {
                    /* Według https://stackoverflow.com/a/1337117 Socket.Send i Receive
                    są wzajemnie thread-safe, więc nie musimy używać locka do synchronizowania
                    Send i Receive. Jeżeli w wątku odbierającym Receive spowoduje zdisposowanie
                    socketa, to wykonany po nim Send wyrzuci ObjectDisposedException lub odwrotnie. */
                    return new Failure(e, interruptedMsg, "|Socket already disposed.|");
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
                e.Prepend("|Error occured while| " +
                    "|AES encrypting packet data.|");
                throw;
            }

            byte[] keyIv;
            try { keyIv = new Rsa(key).Encrypt(Merge(aesKey, aesIv)); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| " +
                    "|RSA encrypting AES key and IV|.");
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
                e.Prepend("|Error occured while| " +
                    "|AES encrypting packet data.|");
                throw;
            }

            var rsa = new Rsa(senderKey);
            byte[] senderRsa;
            try { senderRsa = rsa.Encrypt(Merge(aesKey, aesIv)); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| " +
                    "|RSA encrypting AES key and IV| |using sender's private key.|");
                throw;
            }

            rsa.Key = receiverKey;
            byte[] receiverRsa;
            try { receiverRsa = rsa.Encrypt(senderRsa); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| " +
                    "|RSA encrypting AES key and IV| |using receivers's public key.|");
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
            RequestDisconnect();
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

            var interruptedMsg = "|Connection interrupted.|";
            var closedMsg = "|Connection closed.|";
            while (true)
            {
                if (_disconnectRequested)
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
                    return new Failure(e, interruptedMsg, "|Operating system error.|");
                }
                catch (ObjectDisposedException e)
                {
                    // Patrz ProcessSendInner
                    return new Failure(e, interruptedMsg, "|Socket already disposed.|");
                }

                // odebrano 0 bajtów, co oznacza, że rozmówca bezpiecznie zamknął socket
                if (receivedBytes == 0)
                    return new Success(closedMsg);

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
            RequestDisconnect();
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
                if (_disconnectRequested)
                    return new Cancellation();

                if (!_receiveQueue.TryTake(out byte[] packet, 1000))
                    continue;

                HandlePacket(packet);
            }
        }

        public void RequestDisconnect()
        {
            _disconnectRequested = true;
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
