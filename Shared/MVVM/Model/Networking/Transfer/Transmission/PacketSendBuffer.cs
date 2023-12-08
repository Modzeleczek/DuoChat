using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace Shared.MVVM.Model.Networking.Transfer.Transmission
{
    public class PacketSendBuffer
    {
        #region Properties
        private bool Completed
        {
            get => _sentBytes >= _prefix.Length + _packetNoPrefix!.Length;
        }
        #endregion

        #region Fields
        public const int PREFIX_SIZE = SocketWrapper.PACKET_PREFIX_SIZE;
        /* Bufora _prefix używamy tylko do wysyłania prefiksu
        albo całego pakietu keep alive (bo on jest samym prefiksem). */
        private readonly byte[] _prefix = new byte[PREFIX_SIZE];
        private byte[]? _packetNoPrefix = null;
        private int _sentBytes = 0;
        #endregion

        public void Reset()
        {
            _packetNoPrefix = null;
            _sentBytes = 0;
        }

        public void SendUntilCompletedOrInterrupted(Socket socket,
            CancellationToken cancellationToken, byte[] packetNoPrefix)
        {
            if (packetNoPrefix != _packetNoPrefix)
            {
                /* Wysyłaliśmy już jakiś pakiet i chcemy rozpocząć wysyłanie
                następnego. Jest to niedopuszczalne, bo najpierw musimy
                w całości wysłać poprzedni pakiet. */
                if (!(_packetNoPrefix is null) && !Completed)
                    /* Kiedy zcancelujemy CTS i z SendUntilCompletedOrInterrupted wyleci
                    wyjątek wkazujący na cancel, to dla wywoływacza tej metody (jej
                    klienta) jest to znak, że pakiet jeszcze nie został do końca wysłany
                    i metoda powinna zostać wywołana jeszcze raz z tym samym pakietem.
                    Jeżeli wyjątek nie wyleci, to pakiet już został w całości wysłany. */
                    throw new InvalidOperationException("Tried to send a new packet " +
                        "before completely sending the previous one.");

                _packetNoPrefix = packetNoPrefix;
                PreparePrefix();
                _sentBytes = 0;
            }
            
            while (!Completed)
            {
                /* Wysyłamy tyle bajtów, ile socket przyjmie
                w jednym wywołaniu socket.SendAsync. */
                SendChunk(socket, cancellationToken);
            }
        }

        private void PreparePrefix()
        {
            /* Jeżeli nadawca (host) ma kolejność bajtów little-endian
            (jak wiele Inteli x86), to przerabiamy ją na big-endian, która
            jest konwencjonalna do wysyłania przez internet. Jeżeli host ma
            big-endian, to pozostaje bez zmian. */
            int prefixValue = IPAddress.HostToNetworkOrder(_packetNoPrefix!.Length);

            /* Kopiujemy do bufora prefiks o wartości 0 lub równy rozmiarowi
            pakietu w kolejności big-endian. */
            for (int i = 0; i < _prefix.Length; ++i)
                _prefix[i] = (byte)(prefixValue >> (i * 8));
            /* Można też za pomocą % zapewnić kolejność big-endian w packet.Length i wtedy
            nie trzeba używać IPAddress.HostToNetworkOrder. */
        }

        private void SendChunk(Socket socket, CancellationToken cancellationToken)
        {
            int currentlySent;
            if (_sentBytes < _prefix.Length)
            {
                currentlySent = SocketSend(socket, cancellationToken, _prefix, _sentBytes,
                    _prefix.Length - _sentBytes);
            }
            else
            {
                /* offset to liczba bajtów, które zostały już wysłane
                z bufora _packetNoPrefix. */
                int offset = _sentBytes - _prefix.Length;
                currentlySent = SocketSend(socket, cancellationToken, _packetNoPrefix!,
                    offset, _packetNoPrefix!.Length - offset);
            }
            _sentBytes += currentlySent;
        }

        private int SocketSend(Socket socket, CancellationToken cancellationToken,
            byte[] buffer, int offset, int byteCount)
        {
            ValueTask<int> valueTask = socket.SendAsync(
                new ReadOnlyMemory<byte>(buffer, offset, byteCount),
                SocketFlags.None, cancellationToken);
            Task<int> task = valueTask.AsTask();
            task.Wait();
            return task.Result;
        }
    }
}
