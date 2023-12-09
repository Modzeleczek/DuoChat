using Shared.MVVM.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking.Transfer.Reception
{
    public class PacketReceiveBuffer
    {
        #region Classes
        private enum State { Prefix, Content }
        #endregion

        #region Fields
        private const uint PREFIX_SIZE = SocketWrapper.PACKET_PREFIX_SIZE;
        private const uint MAX_PACKET_SIZE = (1 << 20);

        private State _state = State.Prefix;

        // Bufor cykliczny.
        private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
        // Cykliczny indeks
        private uint _bufferIndex = 0;
        // Całkowita liczba odebranych bajtów
        private uint _receivedBytes = 0;
        private uint _nowInterpretedByteIndex = 0;
        private uint _packetBeginIndexInclusive;
        private uint _packetEndIndexExclusive;
        private readonly byte[] _prefixBuffer = new byte[PREFIX_SIZE];
        private uint _prefixByteCounter;

        private byte[]? _lastReceivedPacket = null;
        #endregion

        public PacketReceiveBuffer()
        {
            Reset();
        }

        public void Reset()
        {
            _bufferIndex = 0;
            _receivedBytes = 0;
            _nowInterpretedByteIndex = 0;
            StartNewPacket();
        }

        private void StartNewPacket()
        {
            // Przechodzimy do stanu Prefix.
            _packetBeginIndexInclusive = _nowInterpretedByteIndex;
            // Ma sens, gdy PREFIX_SIZE == 0. Raczej to bez sensu, ale obsługujemy taką ewentualność.
            for (int i = 0; i < PREFIX_SIZE; ++i)
                _prefixBuffer[i] = 0;
            _prefixByteCounter = 0;
            _state = State.Prefix;
        }

        public byte[]? ReceiveUntilCompletedOrInterrupted(IReceiveSocket socket,
            CancellationToken cancellationToken)
        {
            /* Pętla pozwala odbierać pakiety większe niż socket.ReceiveBufferSize,
            natomiast wciąż nie większe niż MAX_PACKET_SIZE. */
            while (true)
            {
                while (_nowInterpretedByteIndex != _receivedBytes)
                {
                    InterpretByte();

                    if (!(_lastReceivedPacket is null))
                    {
                        var packet = _lastReceivedPacket;
                        _lastReceivedPacket = null;
                        return packet;
                    }
                }

                /* Pobieramy tyle bajtów, ile socket zwróci 
                w jednym wywołaniu socket.ReceiveAsync. */
                uint receivedBytesBackup = _receivedBytes;
                SocketReceive(socket, cancellationToken);
                /* Jeżeli nadawca nie wysłał całego pakietu w jednym wywołaniu
                socket.ReceiveAsync, to liczymy, że dośle brakujące bajty w
                następnym (-ych) wywołaniach. */

                if (_receivedBytes == receivedBytesBackup)
                    // Odebrano 0 bajtów, co oznacza, że rozmówca bezpiecznie zamknął socket.
                    return null;
            }
            /* Jeżeli nadawca nie wysłał całego pakietu w jednym wywołaniu
            ReceiveUntilCompletedOrInterrupted (bo zostało przerwane
            za pomocą CancellationTokena), to liczymy, że dośle brakujące bajty w
            następnym (-ych) wywołaniach. */
        }

        private void SocketReceive(IReceiveSocket socket, CancellationToken cancellationToken)
        {
            // Jeżeli doszliśmy do końca bufora, to wracamy na jego początek.
            if (_bufferIndex == MAX_PACKET_SIZE)
                _bufferIndex = 0;
            uint remainingBufferCapacity = MAX_PACKET_SIZE - _bufferIndex;

            ValueTask<int> valueTask = socket.ReceiveAsync(
                new Memory<byte>(_buffer, (int)_bufferIndex, (int)remainingBufferCapacity),
                SocketFlags.None, cancellationToken);
            Task<int> task = valueTask.AsTask();
            task.Wait();

            /* Dzięki ostatniemu parametrowi Memory i remainingBufferCapacity,
            _bufferIndex zawsze jest mniejsze lub równe MAX_PACKET_SIZE.
            Przesuwamy _bufferIndex i _receivedBytes o liczbę odebranych bajtów. */
            uint receivedBytes = (uint)task.Result;

            _bufferIndex += receivedBytes;
            _receivedBytes += receivedBytes;
        }

        private void InterpretByte()
        {
            switch (_state)
            {
                case State.Prefix: HandlePrefix(); break;
                case State.Content: HandleContent(); break;
            }
        }

        private void HandlePrefix()
        {
            if (_prefixByteCounter == PREFIX_SIZE)
                FinishPrefix();
            else
            {
                _prefixBuffer[_prefixByteCounter] = _buffer[CircularIndex(_nowInterpretedByteIndex)];
                ++_prefixByteCounter;
                ++_nowInterpretedByteIndex;
                if (_prefixByteCounter == PREFIX_SIZE)
                    FinishPrefix();
            }
        }

        private void FinishPrefix()
        {
            // Właśnie skończyliśmy odczytywać prefiks pakietu.

            // Zamieniamy sieciową kolejność big-endian na kolejność hosta.
            uint prefixValue = (uint)IPAddress.NetworkToHostOrder(
                (int)BitConverter.ToUInt32(_prefixBuffer, 0));
            if (PREFIX_SIZE + prefixValue > MAX_PACKET_SIZE)
                /* Klient nie mógł, tylko wykonując swój kod, stworzyć pakietu
                o tak dużym rozmiarze, co oznacza, że ktoś sfabrykował pakiet. */
                throw new Error("|Received packet with prefix value greater than max packet size|.");

            // _packetEndIndexExclusive = _packetBeginIndexInclusive + PREFIX_SIZE + prefixValue LUB
            _packetEndIndexExclusive = _nowInterpretedByteIndex + prefixValue;

            // Odebraliśmy pakiet keep alive.
            if (prefixValue == 0)
            {
                // Pozostajemy w stanie Prefix.
                FlushPacket();
                StartNewPacket();
            }
            // Odebraliśmy pakiet nie keep alive.
            else
            {
                // Przechodzimy do stanu Content.
                _state = State.Content;
            }
        }

        private uint CircularIndex(uint linearIndex)
        {
            /* // Zamiast tego można się posługiwać indeksami i licznikami typu unsigned.
            // https://stackoverflow.com/a/1082938
            int r = linearIndex % MAX_PACKET_SIZE
            return (r < 0 ? r + MAX_PACKET_SIZE : r) */

            return linearIndex % MAX_PACKET_SIZE;
        }

        private void FlushPacket()
        {
            /* _packetBeginIndexInclusive wskazuje na pierwszy bajt prefiksu aktualnego pakietu,
            więc dodajemy PREFIX_SIZE, aby uzyskać rozmiar pakietu bez prefiksu. */
            byte[] packet;
            uint contentBeginIndexInclusive = CircularIndex(_packetBeginIndexInclusive + PREFIX_SIZE);
            uint contentEndIndexExclusive = CircularIndex(_packetEndIndexExclusive);

            if (contentEndIndexExclusive >= contentBeginIndexInclusive)
            {
                uint length = contentEndIndexExclusive - contentBeginIndexInclusive;
                packet = new byte[length];
                Buffer.BlockCopy(_buffer, (int)contentBeginIndexInclusive, packet, 0, (int)length);
            }
            else
            {
                uint lengthToEnd = MAX_PACKET_SIZE - contentBeginIndexInclusive;
                uint lengthFromStart = contentEndIndexExclusive /* - 0 */;
                packet = new byte[lengthToEnd + lengthFromStart];
                Buffer.BlockCopy(_buffer, (int)contentBeginIndexInclusive, packet, 0, (int)lengthToEnd);
                Buffer.BlockCopy(_buffer, 0, packet, 0, (int)lengthFromStart);
            }

            _lastReceivedPacket = packet;
        }

        private void HandleContent()
        {
            if (_nowInterpretedByteIndex == _packetEndIndexExclusive)
                FinishContent();
            else
            {
                ++_nowInterpretedByteIndex;
                if (_nowInterpretedByteIndex == _packetEndIndexExclusive)
                    FinishContent();
            }
        }

        private void FinishContent()
        {
            // Przechodzimy do stanu Prefix.
            // Nie inkrementujemy _nowInterpretedByteIndex, bo już jesteśmy za poprzednim pakietem.
            FlushPacket();
            StartNewPacket();
        }
    }
}
