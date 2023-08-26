using Shared.MVVM.Core;
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
            if (!(obj is Port other)) return false;
            return Value == other.Value;
        }

        public override int GetHashCode() => Value;

        public static Port Parse(string text)
        {
            if (text == null)
                throw new Error("|String is null.|");

            if (!BigInteger.TryParse(text, out BigInteger value))
                throw new Error("|String is not a number.|");

            int min = IPEndPoint.MinPort, max = IPEndPoint.MaxPort;
            if (!(value >= min && value <= max))
                throw new Error($"|Port must be in range| <{min}, {max}>.");

            return new Port((ushort)value);
        }

        public override string ToString() => Value.ToString();

        public static implicit operator string(Port port) => port.ToString();
    }
}
