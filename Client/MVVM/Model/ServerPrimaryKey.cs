using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;

namespace Client.MVVM.Model
{
    public struct ServerPrimaryKey
    {
        public IPv4Address IpAddress { get; }
        public Port Port { get; }

        public ServerPrimaryKey(IPv4Address ipAddress, Port port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public ServerPrimaryKey(string textRepresentation)
        {
            const char separator = '_';
            var split = textRepresentation.Split();
            if (split.Length != 2)
                throw new Error("|Text representation does not consist of two parts separated with| " +
                    $"'{separator}'.");

            // np. gdy textRepresentation == "_"
            if (string.IsNullOrEmpty(split[0]))
                throw new Error("|IP address part| |is empty.|");

            if (string.IsNullOrEmpty(split[1]))
                throw new Error("|Port part| |is empty.|");

            IpAddress = IPv4Address.Parse(split[0]);
            Port = Port.Parse(split[1]);
        }

        public override string ToString()
        {
            // ':' nie może być w nazwie pliku w NTFS, dlatego jako łącznika używamy '_'
            return ToString("{0}_{1}");
        }

        public string ToString(string format)
        {
            return string.Format(format, IpAddress, Port);
        }

        public override bool Equals(object obj)
        {
            return obj is ServerPrimaryKey other
                && IpAddress.Equals(other.IpAddress) && Port.Equals(other.Port);
        }

        public override int GetHashCode()
        {
            return 31 * IpAddress.GetHashCode() + 17 * Port.GetHashCode();
        }
    }
}
