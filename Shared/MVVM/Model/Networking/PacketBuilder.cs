using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using System;
using System.Collections.Generic;
using System.Net;

namespace Shared.MVVM.Model.Networking
{
    public class PacketBuilder
    {
        private LinkedList<byte[]> parts = new LinkedList<byte[]>();

        public PacketBuilder() { }

        public void Append(int number, int bytesCount) =>
            parts.AddLast(Serialize(number, bytesCount));

        private byte[] Serialize(int number, int bytesCount)
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

        public void Prepend(int number, int bytesCount) =>
            parts.AddFirst(Serialize(number, bytesCount));

        public void Prepend(byte[] bytes) => parts.AddFirst(bytes);

        public void Encrypt(Aes encryptor)
        {
            try
            {
                var encrypted = encryptor.Encrypt(Merge(CalculateSize()));
                parts.Clear();
                parts.AddLast(encrypted);
            }
            catch (Error e)
            {
                e.Prepend("|Error occured while| " +
                    "|encrypting merged packet parts.|");
                throw;
            }
        }

        private byte[] Merge(int totalSize)
        {
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

        public byte[] Build()
        {
            var totalSize = CalculateSize();
            // dodajemy rozmiar całego pakietu jako pierwszą część pakietu
            Prepend(totalSize, Client.PREFIX_SIZE);
            // dodajemy rozmiar dodanego prefiksu
            return Merge(totalSize + Client.PREFIX_SIZE);
        }

        private int CalculateSize()
        {
            // sumujemy rozmiary wszystkich części pakietu
            var totalSize = 0;
            foreach (var b in parts)
                totalSize += b.Length;
            return totalSize;
        }

        public static PacketBuilder operator +(PacketBuilder pb, (int, int) numberBytesCount)
        {
            pb.Append(numberBytesCount.Item1, numberBytesCount.Item2);
            return pb;
        }

        public static PacketBuilder operator +(PacketBuilder pb, byte[] bytes)
        {
            pb.Append(bytes);
            return pb;
        }

        public static PacketBuilder operator +((int, int) numberBytesCount, PacketBuilder pb)
        {
            pb.Prepend(numberBytesCount.Item1, numberBytesCount.Item2);
            return pb;
        }

        public static PacketBuilder operator +(byte[] bytes, PacketBuilder pb)
        {
            pb.Prepend(bytes);
            return pb;
        }
    }
}
