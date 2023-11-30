using Shared.MVVM.Core;
using System;
using System.Net;

namespace Shared.MVVM.Model.Networking
{
    public class IPv4Address
    {
        public uint BinaryRepresentation { get; }

        public IPv4Address(uint binaryRepresentation)
        {
            BinaryRepresentation = binaryRepresentation;
        }

        public IPv4Address(IPAddress ipAddress)
        {
            var bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
                throw new ArgumentException("ipAddress must be IPv4", nameof(ipAddress));
            BinaryRepresentation = BitConverter.ToUInt32(bytes, 0);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is IPv4Address other)) return false;
            return BinaryRepresentation == other.BinaryRepresentation;
        }

        public override int GetHashCode() => (int)BinaryRepresentation;

        public static IPv4Address Parse(string text)
        {
            if (text == null)
                throw new Error("|String is null.|");

            var split = text.Split('.');
            if (split.Length != 4)
                throw new Error("|String does not consist of four octets separated with periods.|");

            uint binRepr = 0;
            for (int i = 3; i >= 0; --i)
            {
                binRepr <<= 8;
                if (!byte.TryParse(split[i], out byte parsedByte))
                    throw new Error($"{i + 1}. " +
                        "|octet from the left is not valid number in range| <0,255>.");
                /* Traktujemy oktety adresu jako zapisane w kolejności big-endian,
                czyli prawy (ostatni z oddzielonych kropkami) oktet jest zapisany
                w najbardziej znaczącym bajcie _binaryRepresentation. */
                binRepr |= parsedByte;
            }
            /* nie używać, bo może parsować adresy IPv6
            if (!IPAddress.TryParse(text, out IPAddress ip))
                return false; */
            return new IPv4Address(binRepr);
        }

        public override string ToString()
        {
            uint binRepr = BinaryRepresentation;
            var octets = new byte[4];
            for (int i = 0; i <= 3; ++i)
            {
                octets[i] = (byte)(binRepr & 255);
                binRepr >>= 8;
            }
            return string.Join(".", octets);
        }

        public IPAddress ToIPAddress() => new IPAddress(BinaryRepresentation);

        public static implicit operator string(IPv4Address ip) => ip.ToString();
    }
}
