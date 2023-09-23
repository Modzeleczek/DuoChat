using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var type = typeof(PrivateKey);
            PrivateType privateType = new PrivateType(type);
            object[] parameterValues = { bitCount, sign, rng };

            return (BigInteger)privateType.InvokeStatic(
                "GenerateRandom", parameterValues);
        }
    }
}
