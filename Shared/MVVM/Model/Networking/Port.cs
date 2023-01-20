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

        public static Status TryParse(string text)
        {
            var d = Translator.Instance;
            if (text == null)
                return new Status(-1, d["String is null."]);

            if (!BigInteger.TryParse(text, out BigInteger value))
                return new Status(-2, d["String is not a number."]);

            int min = IPEndPoint.MinPort, max = IPEndPoint.MaxPort;
            if (!(value >= min && value <= max))
                return new Status(-3, (d["Port must be in range"] + $" <{min}, {max}>."));

            return new Status(0, null, new Port((ushort)value));
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
