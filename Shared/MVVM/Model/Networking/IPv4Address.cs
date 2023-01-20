using Shared.MVVM.View.Localization;
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

        public static Status TryParse(string text)
        {
            var d = Translator.Instance;
            if (text == null)
                return new Status(-1, d["String is null."]);
            var split = text.Split('.');
            if (split.Length != 4)
                return new Status(-2, d["String does not consist of four octets separated with periods."]);
            int binRepr = 0;
            for (int i = 3; i >= 0; --i)
            {
                binRepr <<= 8;
                if (!byte.TryParse(split[i], out byte parsedByte))
                    return new Status(-3, $"{i + 1}. " +
                        d["octet from the left is not valid number in range"] + " <0,255>.");
                // trzymamy bajty adresu w kolejności big-endian, czyli prawy (ostatni z oddzielonych kropkami) oktet jest zapisany w najbardziej znaczącym bajcie _binaryRepresentation
                binRepr |= parsedByte;
            }
            /* nie używać, bo może parsować adresy IPv6
            if (!IPAddress.TryParse(text, out IPAddress ip))
                return false; */
            return new Status(0, null, new IPv4Address(binRepr));
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
