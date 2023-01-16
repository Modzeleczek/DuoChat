using System.Net;

namespace Shared.MVVM.Model
{
    public class IPv4Address
    {
        public int BinaryRepresentation { get; private set; }

        public IPv4Address(int binaryRepresentation)
        {
            BinaryRepresentation = binaryRepresentation;
        }

        public static bool TryParse(string text, out IPv4Address ret)
        {
            ret = null;
            if (text == null) return false;
            var split = text.Split('.');
            if (split.Length != 4) return false;
            int binRepr = 0;
            for (int i = 3; i >= 0; --i)
            {
                binRepr <<= 8;
                if (!byte.TryParse(split[i], out byte parsedByte))
                    return false;
                // trzymamy bajty adresu w kolejności big-endian, czyli prawy (ostatni z oddzielonych kropkami) oktet jest zapisany w najbardziej znaczącym bajcie _binaryRepresentation
                binRepr |= parsedByte;
            }
            /* nie używać, bo może parsować adresy IPv6
            if (!IPAddress.TryParse(text, out IPAddress ip))
                return false; */
            ret = new IPv4Address(binRepr);
            return true;
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
    }
}
