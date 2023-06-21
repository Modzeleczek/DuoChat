using Shared.MVVM.Core;
using System.Net;

namespace Shared.MVVM.Model.Networking
{
    public class IPv4Address
    {
        public int BinaryRepresentation { get; private set; }

        public IPv4Address(int binaryRepresentation)
        {
            BinaryRepresentation = binaryRepresentation;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IPv4Address)) return false;
            var ip = (IPv4Address)obj;
            return BinaryRepresentation == ip.BinaryRepresentation;
        }

        public override int GetHashCode() => base.GetHashCode();

        public static IPv4Address Parse(string text)
        {
            if (text == null)
                throw new Error("|String is null.|");
            var split = text.Split('.');
            if (split.Length != 4)
                throw new Error("|String does not consist of four octets separated with periods.|");
            int binRepr = 0;
            for (int i = 3; i >= 0; --i)
            {
                binRepr <<= 8;
                if (!byte.TryParse(split[i], out byte parsedByte))
                    throw new Error($"{i + 1}. " +
                        "|octet from the left is not valid number in range| <0,255>.");
                /* trzymamy bajty adresu w kolejności big-endian, czyli prawy
                (ostatni z oddzielonych kropkami) oktet jest zapisany w najbardziej
                znaczącym bajcie _binaryRepresentation */
                binRepr |= parsedByte;
            }
            /* nie używać, bo może parsować adresy IPv6
            if (!IPAddress.TryParse(text, out IPAddress ip))
                return false; */
            return new IPv4Address(binRepr);
        }

        public override string ToString()
        {
            int binRepr = BinaryRepresentation;
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
