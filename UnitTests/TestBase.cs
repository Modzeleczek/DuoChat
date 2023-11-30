using Shared.MVVM.Model.Cryptography;
using System.Numerics;
using System.Security.Cryptography;

namespace UnitTests
{
    public class TestBase
    {
        protected BigInteger PrivateKey_GenerateRandom(int bitCount, bool sign,
            RandomNumberGenerator rng)
        {
            return typeof(PrivateKey).InvokeStatic<BigInteger>(
                "GenerateRandom", bitCount, sign, rng);
        }
    }
}
