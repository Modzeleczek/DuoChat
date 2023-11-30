using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Model.Cryptography;
using System.Numerics;

namespace UnitTests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void Cast_UshortToShort_ShouldNotChangeBinaryRepresentation()
        {
            // Arrange
            ushort a = 0x9B40;
            byte[] expected = BitConverter.GetBytes(a);

            // Act
            byte[] actual = BitConverter.GetBytes((short)a);

            // Assert
            expected.BytesEqual(actual);
        }

        [TestMethod]
        public void BigInteger_ToByteArray_ShouldReturnBytesInLittleEndian()
        {
            // Arrange
            var expected = new byte[] { 0x12, 0x34, 0x56, 0x78 };

            // Act
            var actual = new BigInteger(0x78563412).ToByteArray();

            // Assert
            /* W tablicy otrzymanej poprzez BigInteger.ToByteArray
            na indeksie 0 znajduje się najmniej znaczący bajt liczby. */
            Console.WriteLine($"expected: {actual.ToHexString()}");
            Console.WriteLine($"actual: {actual.ToHexString()}");

            expected.BytesEqual(actual);
        }

        [TestMethod]
        public void PrivateKey_ToUnsignedBigEndian_12ABCD34_ShouldReturn12ABCD34()
        {
            // Arrange
            BigInteger bigInt = new BigInteger(0x12ABCD34);
            var expected = new byte[] { 0x12, 0xAB, 0xCD, 0x34 };

            // Act
            byte[] actual = typeof(PrivateKey).InvokeStatic<byte[]>(
                "BIToPaddedUBE", bigInt, bigInt.GetByteCount());

            // Assert
            Console.WriteLine(actual.ToHexString());

            expected.BytesEqual(actual);
        }

        [TestMethod]
        public void PrivateKey_TryParse_WhenGivenValidString_ShouldReturnTrue()
        {
            // Arrange
            const bool expected = true;
            const string privateKeyString = "Fw==;JQ=="; // <23 w base64>;<37 w base64>

            // Act
            var actual = PrivateKey.TryParse(privateKeyString, out PrivateKey? key);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
