using Shared.MVVM.Model.Networking;

namespace Server.MVVM.Model
{
    // TODO: scalić z ServerPrimaryKey z programu klienta.
    public struct ClientPrimaryKey
    {
        public IPv4Address IpAddress { get; }
        public Port Port { get; }

        public ClientPrimaryKey(IPv4Address ipAddress, Port port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public string ToString(string format)
        {
            return string.Format(format, IpAddress, Port);
        }

        public override string ToString()
        {
            return ToString("{0}:{1}");
        }

        public override bool Equals(object? obj)
        {
            return obj is ClientPrimaryKey other
                && IpAddress.Equals(other.IpAddress) && Port.Equals(other.Port);
        }

        public override int GetHashCode()
        {
            return 31 * IpAddress.GetHashCode() + 17 * Port.GetHashCode();
        }
    }
}
