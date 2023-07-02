using System;
using System.Net;

namespace Shared.MVVM.Model.Networking
{
    public class PacketBuffer
    {
        private const int MAX_PACKET_SIZE = (1 << 20);
        private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
        private int _offset, _size;

        public bool PacketReady { get => _offset == _size; }
        public string ErrorMessage { get; private set; }

        public PacketBuffer()
        {
            ResetState();
        }

        public bool Write(byte b)
        {
            // jeszcze nie odczytaliśmy prefiksu aktualnego pakietu
            if (_size == -1)
            {
                // właśnie kończymy odczytywać prefiks pakietu
                if (_offset == Client.PREFIX_SIZE - 1)
                {
                    _buffer[_offset] = b;
                    // zamieniamy sieciową kolejność big-endian na kolejność hosta
                    var prefixValue = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_buffer, 0));
                    if (prefixValue > MAX_PACKET_SIZE)
                    {
                        /* nadawca nie mógł, tylko wykonując swój kod, stworzyć pakietu
                        o tak dużym rozmiarze, co oznacza, że ktoś sfabrykował pakiet */
                        ErrorMessage = "|Received fabricated packet.|";
                        return false;
                    }
                    _size = prefixValue;
                    /* wracamy na początek bufora, aby mieć miejsce na maksymalnie
                    MAX_PACKET_SIZE bajtów nie pomniejszone o Client.PREFIX_SIZE */
                    _offset = 0;
                    return true;
                }
            }
            else // już odczytaliśmy prefiks aktualnego pakietu
            {
                // jesteśmy już na końcu, a użytkownik bufora dalej próbuje do niego pisać
                if (PacketReady)
                {
                    ErrorMessage = "|Received packet's body is larger than its prefix value.|";
                    return false;
                }

                /* nie powinno nigdy mieć miejsca, bo byłaby to sytuacja, w której _offset > _size,
                co nie jest dopuszczalne przez warunek if (prefixValue > MAX_PACKET_SIZE) */
                if (_offset == MAX_PACKET_SIZE)
                {
                    ErrorMessage = "|Received packet too large for PacketBuffer.|";
                    return false;
                }
            }

            // zapisujemy bajt i przesuwamy offset (wkaźnik bufora)
            _buffer[_offset] = b;
            ++_offset;
            return true;
        }

        public byte[] FlushPacket()
        {
            var packet = new byte[_size];
            Buffer.BlockCopy(_buffer, 0, packet, 0, _size);
            ResetState();
            return packet;
        }

        private void ResetState()
        {
            _offset = 0;
            // wstępnie zapisujemy nieznany rozmiar pakietu
            _size = -1;
        }
    }
}

