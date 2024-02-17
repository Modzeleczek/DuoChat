using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
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
            return typeof(PrivateKey).InvokeStatic<BigInteger>(
                "FirstProbablePrimeGreaterOrEqual", min);
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
        public void PrivateKey_Random_WhenRequested258Bytes_ShouldGeneratePrivateKeyWith258ByteLongPublicKey()
        {
            // Arrange
            int numberOfBits = 258 * 8, enabledBitIndex = 64 * 8;

            // Act
            var actual = PrivateKey.Random(numberOfBits, enabledBitIndex);

            // Assert
            Console.WriteLine(actual);

            var actPublicKey = actual.ToPublicKey();
            Assert.AreEqual(258, actPublicKey.Length);
        }

        [TestMethod]
        public void PrivateKey_Random_WhenEnabledBitIndexGreaterOrEqualToNumberOfBits_ShouldThrowArgumentException()
        {
            // Arrange
            int numberOfBits = 256 * 8, enabledBitIndex = 256 * 8;

            // Act
            var testDelegate = () => PrivateKey.Random(numberOfBits, enabledBitIndex);

            // Assert
            var ex = Assert.ThrowsException<ArgumentException>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("enabledBitIndex must be less than numberOfBits"));
        }

        [TestMethod]
        public void PrivateKey_Random_WhenCanceledBeforeCalling_ShouldReturnCancellation()
        {
            // Arrange
            var doWorkEventArgs = new DoWorkEventArgs(null);
            var worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            var progressReporter = new ProgressReporter(worker, doWorkEventArgs);

            // Act
            worker.CancelAsync();
            PrivateKey.Random(progressReporter);
            var result = doWorkEventArgs.Result;

            // Assert
            Assert.IsInstanceOfType(result, typeof(Cancellation));
        }

        [TestMethod]
        public void PrivateKey_Random_WhenNumberOfBitsEqualTo0_ShouldThrowArgumentException()
        {
            // Arrange
            int numberOfBits = 0, enabledBitIndex = -1;

            // Act
            var testDelegate = () => PrivateKey.Random(numberOfBits, enabledBitIndex);

            // Assert
            var ex = Assert.ThrowsException<ArgumentException>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("bitCount must be greater than 0"));
        }
    }
}
