using Shared.MVVM.View.Localization;
using System.Net;
using System.Numerics;

namespace Shared.MVVM.Model.Networking
{
    public class Port
    {
        public ushort Value { get; private set; }

        public Port(ushort value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Port)) return false;
            var port = (Port)obj;
            return Value == port.Value;
        }

        public override int GetHashCode() => base.GetHashCode();

        public static Status TryParse(string text)
        {
            var d = Translator.Instance;
            if (text == null)
                return new Status(-1, null, d["String is null."]);

            if (!BigInteger.TryParse(text, out BigInteger value))
                return new Status(-2, null, d["String is not a number."]);

            int min = IPEndPoint.MinPort, max = IPEndPoint.MaxPort;
            if (!(value >= min && value <= max))
                return new Status(-3, null, d["Port must be in range"], $"<{min}, {max}>.");

            return new Status(0, new Port((ushort)value));
        }

        public override string ToString() => Value.ToString();

        public static implicit operator string(Port port) => port.ToString();
    }
}
