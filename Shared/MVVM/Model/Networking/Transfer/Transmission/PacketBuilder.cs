using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using System;
using System.Collections.Generic;

namespace Shared.MVVM.Model.Networking.Transfer.Transmission
{
    /* Klasa do pakowania (enkapsulacji) pakietu - budujemy go od danych,
    wychodząc coraz wyżej aż do nagłówka, czyli od dołu do góry. */
    public class PacketBuilder
    {
        private const int ENCRYPTED_KEY_IV_SIZE = sizeof(ushort);

        private readonly LinkedList<byte[]> parts = new LinkedList<byte[]>();

        public PacketBuilder() { }

        public void Append(ulong number, int bytesCount) =>
            parts.AddLast(Serialize(number, bytesCount));

        private byte[] Serialize(ulong number, int bytesCount)
        {
            // Zakładamy, że number można zapisać na bytesCount bajtów.
            var buffer = new byte[bytesCount];
            /* Sieciowa kolejność bajtów to big-endian.
            Najmniej znaczący bajt number (używamy modulo,
            co uniezależnia algorytm od endiannessu hosta) zapisujemy
            na ostatniej pozycji bufora. */
            for (int i = bytesCount - 1; i >= 0; --i)
            {
                buffer[i] = (byte)(number % 256);
                number /= 256;
            }
            return buffer;
        }

        public void Append(byte[] bytes) => parts.AddLast(bytes);

        public void Prepend(ulong number, int bytesCount) =>
            parts.AddFirst(Serialize(number, bytesCount));

        public void Prepend(byte[] bytes) => parts.AddFirst(bytes);

        public void Sign(PrivateKey key)
        {
            /* Czasami klucz publiczny wyznaczany z klucza prywatnego ma długość
            255 bajtów zamiast 256 i wtedy sygnatura też ma 255 bajtów. */
            var mergedParts = MergeParts(CalculateSize());
            byte[] signature;
            try { signature = Rsa.Sign(key, mergedParts); }
            catch (Error e)
            {
                e.Prepend($"|Could not| |RSA sign| |merged packet parts|.");
                throw;
            }

            /* Zastępujemy części pakietu w oddzielnych buforach
            jednym złączonym buforem mergedParts. */
            parts.Clear();
            Append((ulong)signature.Length, 2);
            Append(signature);
            Append(mergedParts);
        }

        public void Encrypt(PublicKey key)
        {
            byte[] plainKeyIv = RandomAesEncrypt();
            byte[] encrKeyIv;
            try { encrKeyIv = Rsa.Encrypt(key, plainKeyIv); }
            catch (Error e)
            {
                e.Prepend($"|Could not| |RSA encrypt| |AES key and IV|.");
                throw;
            }

            Prepend(encrKeyIv);
            Prepend((ulong)encrKeyIv.Length, ENCRYPTED_KEY_IV_SIZE);
        }

        private byte[] RandomAesEncrypt()
        {
            // Zwracamy losowo wygenerowane i złączone klucz AES i IV.
            var (aesKey, aesIv) = Aes.GenerateKeyIv();
            byte[] encrypted;
            try { encrypted = Aes.Encrypt(aesKey, aesIv, MergeParts(CalculateSize())); }
            catch (Error e)
            {
                e.Prepend("|Could not| |AES encrypt| |merged packet parts|.");
                throw;
            }

            parts.Clear();
            Append(encrypted);
            return MergeKeyIv(aesKey, aesIv);
        }

        private byte[] MergeParts(int totalSize)
        {
            if (parts.Count == 1)
                return parts.First!.Value;

            var buffer = new byte[totalSize];
            int position = 0;
            // przepisujemy części z parts do bufora
            foreach (var b in parts)
            {
                Buffer.BlockCopy(b, 0, buffer, position, b.Length);
                position += b.Length;
            }
            return buffer;
        }

        private byte[] MergeKeyIv(byte[] aesKey, byte[] aesIv)
        {
            var ret = new byte[aesKey.Length + aesIv.Length];
            Buffer.BlockCopy(aesKey, 0, ret, 0, aesKey.Length);
            Buffer.BlockCopy(aesIv, 0, ret, aesKey.Length, aesIv.Length);
            return ret;
        }

        public byte[] Build()
        {
            return MergeParts(CalculateSize());
        }

        private int CalculateSize()
        {
            // sumujemy rozmiary wszystkich części pakietu
            var totalSize = 0;
            foreach (var b in parts)
                totalSize += b.Length;
            return totalSize;
        }

        public static PacketBuilder operator +(PacketBuilder pb, (long, int) numberBytesCount)
        {
            pb.Append((ulong)numberBytesCount.Item1, numberBytesCount.Item2);
            return pb;
        }

        public static PacketBuilder operator +(PacketBuilder pb, byte[] bytes)
        {
            pb.Append(bytes);
            return pb;
        }

        public static PacketBuilder operator +((long, int) numberBytesCount, PacketBuilder pb)
        {
            pb.Prepend((ulong)numberBytesCount.Item1, numberBytesCount.Item2);
            return pb;
        }

        public static PacketBuilder operator +(byte[] bytes, PacketBuilder pb)
        {
            pb.Prepend(bytes);
            return pb;
        }

        public void AppendSignature(PrivateKey privateKey, byte[] data)
        {
            byte[] signature = Rsa.Sign(privateKey, data);
            Append((ulong)signature.Length, 2);
            Append(signature);
        }
    }
}
