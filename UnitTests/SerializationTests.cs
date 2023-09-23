using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Model.Cryptography;
using System;
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
            var type = typeof(PrivateKey);
            PrivateType privateType = new PrivateType(type);
            object[] parameterValues = { new BigInteger(0x12ABCD34) };
            var expected = new byte[] { 0x12, 0xAB, 0xCD, 0x34 };

            // Act
            byte[] actual = (byte[])privateType.InvokeStatic(
                "ToUnsignedBigEndian", parameterValues);

            // Assert
            Console.WriteLine(actual.ToHexString());

            expected.BytesEqual(actual);
        }

        [TestMethod]
        public void PrivateKey_TryParse_WhenGivenValidString_ShouldReturnTrue()
        {
            // Arrange
            // Używamy przykładowego klucza prywatnego z pola _privateKeyString.
            const bool expected = true;
            const string privateKeyString =
                "H59gUVkp3PJrvhWNj3Jb+k0ib3PRqkIhNlrIe3dMwb7KRkpjpOHjD8GtMX2" +
                "fKKyhGKzURVQ4Ocx1OcApOqqa+9wlNWpgmmnL8kAYRF4+s0Ei8VYCd8gqTB" +
                "nov3UZ3kNQa/2LYRXSC3IfnbauS0DPUdRSu6zo9V2TUnEDnhcDDKM=;Djjp" +
                "XLh/vOkQFe/EGnm44YYbDNdeFTEsWzKDT36deX8npw5ZzvmhkA2r0CxadCz" +
                "MaJ6VlTPEvbTxU7ZQ4IZnzK4+pTTtkumkY6JjRt++58ooREhX2cdqLG4E/v" +
                "11PWee9MqYCXfyrVZnJquv53wBlfP26jpEP00IV5qCMVfM+mM=";

            // Act
            var actual = PrivateKey.TryParse(privateKeyString, out PrivateKey key);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
