using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Security.Cryptography;

namespace UnitTests
{
    [TestClass]
    public class RandomNumberGenerationTests : TestBase
    {
        [TestMethod]
        /* FirstProbablePrimeGreaterThan powinno się nazywać FirstProbablePrimeGreaterEquals,
        bo testuje też pierwszość przekazanej mu dolnej granicy zakresu. */
        public void PrivateKey_FirstProbablePrimeGreaterThan_2Power1023Minus1_ShouldBeLessEqual2Power1024Minus1()
        {
            // Arrange
            BigInteger maxExpected = (BigInteger.One << 1024) - 1;

            // Act
            // Testujemy prywatną statyczną metodę.
            BigInteger actual = PrivateKey_FirstProbablePrimeGreaterThan((BigInteger.One << 1023) - 1);

            // Assert
            Console.WriteLine($"max expected bits: " +
                $"{CountBitsInBinaryRepresentation(maxExpected)}");
            Console.WriteLine($"actual bits: {CountBitsInBinaryRepresentation(actual)}");

            Assert.IsTrue(actual <= maxExpected);
        }

        private BigInteger PrivateKey_FirstProbablePrimeGreaterThan(BigInteger min)
        {
            var type = typeof(PrivateKey);
            PrivateType privateType = new PrivateType(type);
            object[] parameterValues = { min };

            return (BigInteger)privateType.InvokeStatic(
                "FirstProbablePrimeGreaterOrEqual", parameterValues);
        }

        protected int CountBitsInBinaryRepresentation(BigInteger number)
        {
            int counter = 0;
            while (number > 0)
            {
                number /= 2;
                ++counter;
            }
            return counter;
        }

        [TestMethod]
        public void PrivateKey_GenerateRandom_WhenBitCountLessEqual1023_ShouldReturnNonNegativeBigIntegerLessEqual2Power1023Minus1OrNegativeGreaterEqualMinus2Power1023()
        {
            // Arrange
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int bits = 1; bits <= 1023; ++bits)
                {
                    BigInteger maxExpNonNegative = (BigInteger.One << bits) - 1;
                    BigInteger minExpNegative = -(BigInteger.One << bits);

                    // Act
                    BigInteger actNonNegative = PrivateKey_GenerateRandom(bits, false, rng);
                    BigInteger actNegative = PrivateKey_GenerateRandom(bits, true, rng);

                    // Assert
                    Assert.IsTrue(actNonNegative <= maxExpNonNegative);
                    Assert.IsTrue(actNegative >= minExpNegative);
                }
            }
        }

        [TestMethod]
        public void PrivateKey_Random_ShouldGeneratePrivateKey()
        {
            // Arrange
            var doWorkEventArgs = new DoWorkEventArgs(null);
            var progressReporter = new ProgressReporter(doWorkEventArgs);

            // Act
            PrivateKey.Random(progressReporter);
            var result = doWorkEventArgs.Result;

            // Assert
            // Wyrzuci wyjątek, jeżeli nie uda się zrzutować.
            var actual = (PrivateKey)((Success)result).Data;
            Console.WriteLine(actual);
        }
    }
}
