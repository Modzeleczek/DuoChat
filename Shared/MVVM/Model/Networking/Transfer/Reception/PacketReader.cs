using Shared.MVVM.Model.Cryptography;
using System;
using System.Net;
using System.Text;

namespace Shared.MVVM.Model.Networking.Transfer.Reception
{
    /* Klasa do odpakowywania (dekapsulacji) pakietu - odczytujemy go od nagłówka,
    schodząc coraz niżej aż do danych, czyli od góry do dołu. */
    public class PacketReader
    {
        #region Properties
        private int Remaining => _packet.Length - _byteIndex;
        #endregion

        #region Fields
        private int _byteIndex = 0;
        private byte[] _packet;
        #endregion

        public PacketReader(byte[] packet)
        {
            _packet = packet;
        }

        public bool VerifySignature(PublicKey key)
        {
            /* Zakładamy, że podpisane dane znajdują się w pakiecie
            od razu po sygnaturze. */
            ushort signatureLength = ReadUInt16();
            byte[] signature = ReadBytes(signatureLength);
            byte[] signedData = ReadBytes(Remaining);
            /* Przewijamy do tyłu, aby klient klasy PacketReader
            mógł odczytać podpisane dane. */
            Proceed(-signedData.Length);

            return Rsa.Verify(key, signedData, signature);
        }

        public bool VerifySignature(PublicKey key, byte[] signedData)
        {
            // Danych nie ma w pakiecie.
            ushort signatureLength = ReadUInt16();
            byte[] signature = ReadBytes(signatureLength);

            return Rsa.Verify(key, signedData, signature);
        }

        public void Decrypt(PrivateKey rsaKey)
        {
            var encrKeyIvBytesLength = ReadUInt16();
            // Według obliczeń z protokol.txt, max 256 B.
            /* if (encrKeyIvBytesLength > 256)
                throw new Error(); */

            var plainKeyIv = Rsa.Decrypt(rsaKey, ReadBytes(encrKeyIvBytesLength));

            var (aesKey, aesIv) = UnmergeAesKeyIv(plainKeyIv);
            // Odszyfrowujemy do końca pakietu.
            _packet = Aes.Decrypt(aesKey, aesIv, _packet, _byteIndex, Remaining);
            _byteIndex = 0;
        }

        private (byte[], byte[]) UnmergeAesKeyIv(byte[] keyIv)
        {
            var key = new byte[Aes.KEY_LENGTH];
            var iv = new byte[Aes.BLOCK_LENGTH];
            Buffer.BlockCopy(keyIv, 0, key, 0, key.Length);
            Buffer.BlockCopy(keyIv, key.Length, iv, 0, iv.Length);
            return (key, iv);
        }

        public string ReadUtf8String(int length)
        {
            string ret = Encoding.UTF8.GetString(_packet, _byteIndex, length);
            Proceed(length);
            return ret;
        }

        /* public string ReadLengthAndUtf8String(int lengthBytes)
        {
            if (lengthBytes > 8)
                throw new ArgumentException("lengthBytes must be in range <0, 8>",
                    nameof(lengthBytes));

            ulong length = 0;
            IPAddress.NetworkToHostOrder(BitConverter.)
        } */

        public byte ReadUInt8()
        {
            /* Przy tylko 1 bajcie nie ma sensu przekształcanie
            kolejności bajtów za pomocą IPAddress.NetworkToHostOrder. */
            byte ret = _packet[_byteIndex];
            Proceed(1);
            return ret;
        }

        private void Proceed(int byteCount)
        {
            _byteIndex += byteCount;
        }

        public ushort ReadUInt16()
        {
            ushort ret = (ushort)IPAddress.NetworkToHostOrder(
                BitConverter.ToInt16(_packet, _byteIndex));
            Proceed(sizeof(ushort));
            return ret;
        }

        public uint ReadUInt32()
        {
            uint ret = (uint)IPAddress.NetworkToHostOrder(
                BitConverter.ToInt32(_packet, _byteIndex));
            Proceed(sizeof(uint));
            return ret;
        }

        public byte[] ReadBytes(int length)
        {
            // Dowolne (arbitrary) bajty.
            var ret = new byte[length];
            Buffer.BlockCopy(_packet, _byteIndex, ret, 0, length);
            Proceed(length);
            return ret;
        }

        public byte[] ReadBytesToEnd()
        {
            int remaining = Remaining;
            var ret = new byte[remaining];
            Buffer.BlockCopy(_packet, _byteIndex, ret, 0, remaining);
            Proceed(remaining);
            return ret;
        }

        public Guid ReadGuid()
        {
            byte[] slice = new byte[16];
            Buffer.BlockCopy(_packet, _byteIndex, slice, 0, slice.Length);
            Proceed(slice.Length);
            return new Guid(slice);
        }

        public ulong ReadUInt64()
        {
            ulong ret = (ulong)IPAddress.NetworkToHostOrder(
                BitConverter.ToInt64(_packet, _byteIndex));
            Proceed(sizeof(ulong));
            return ret;
        }
    }
}
