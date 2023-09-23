using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Model.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;
using Aes = Shared.MVVM.Model.Cryptography.Aes;

namespace UnitTests
{
    [TestClass]
    public class CryptographyTests : TestBase
    {
        [TestMethod]
        public void Rsa_Verify_WhenGivenSignatureOf1MiBLongData_ShouldReturnTrue()
        {
            // Arrange
            PrivateKey privateKey = PrivateKey.Random();
            PublicKey publicKey = privateKey.ToPublicKey();
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1 << 20));
            byte[] signature = Rsa.Sign(privateKey, data);

            const bool expected = true;

            // Act
            bool actual = Rsa.Verify(publicKey, data, signature);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Rsa_Verify_DeserializedKeyAnd1MiBLongData_ShouldReturnTrue()
        {
            // Arrange
            PrivateKey privateKey = PrivateKey.Parse(
                "ZLVfVP8XFEmW4S2Oslvevpl5C7LbNTRJrHhYXcltO/os6tcjjmNvfqe6YC3yUQMzhAr/SG" +
                "21YH2RJJpbKYfH20Tb7UD3XRUYC4UHlhdlNogHNwV5EjDGOV2PmmSKG+kcqibeb2aITeEt" +
                "xLIwVbZEE2DVDq7H5F+78bIVM9qhDXc=;Ye8n4Al5GTc/EeJ2RV5BlHQ/drOE1sUxZ2y5i" +
                "6mw88GUPpDPAgIiv93R5WZPOW8qjakdlYISmdwzAxCTO8weU58qepWGQyK1thPoZXQj5p/" +
                "6Y9CPZb19wT5rzI5I7swHX6jlsgpsCbtrUW2+WPPCtDMGISjXerg9tc3C+LLYUik=");
            PublicKey publicKey = privateKey.ToPublicKey();
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1 << 20));
            byte[] signature = Convert.FromBase64String(
                "E+gS9/iI6mwI7SNWcvv/ZxcMqB/zsC2hZWv0HTYz1OJsflKM6ODxhT23TfNzOlj8A5B8lK" +
                "Zn5rX+MRVrC95brtrt5/LWyF/vn5t7fVJrt1GyOOwUh0xX7/xsTZuNSRI7tsn7GrILpiTq" +
                "CKC2GVlubUvhiR7W2wFFTisUCG2xFHpTMyHAr2rgNV+onLGU2X+80tPo3KHRg5qMuRt1yw" +
                "CYaG13Mp5XKSIDNH9B+FY1kE3g3wYEKYsoYlTgtJ8e2Uki5nSnOLDCaI1dzaJbKwL74xFK" +
                "5Rlcc/xCw2PExJgaDXZPq/TJ0iFFpasitzBDBbgxkib9jUU9Bdq2ASU3rNmHGQ==");

            const bool expected = true;

            // Act
            bool actual = Rsa.Verify(publicKey, data, signature);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Rsa_Decrypt_RandomKeyAnd64BLongPlainText_ShouldReturnPlainText()
        {
            // Arrange
            PrivateKey privateKey = PrivateKey.Random();
            PublicKey publicKey = privateKey.ToPublicKey();

            string plainText = new string('a', 64);
            var ciphText = Rsa.Encrypt(publicKey, Encoding.UTF8.GetBytes(plainText));

            string expected = plainText;

            // Act
            string actual = Encoding.UTF8.GetString(Rsa.Decrypt(privateKey, ciphText));

            // Assert
            Console.WriteLine($"expected: {expected.Length} {expected}");
            Console.WriteLine($"actual: {actual.Length} {actual}");

            Assert.AreEqual(expected, actual);
        }

        private void Reverse(ref byte[] array)
        {
            for (int i = 0; i < array.Length / 2; ++i)
            {
                byte temp = array[i];
                array[i] = array[array.Length - 1 - i];
                array[array.Length - 1 - i] = temp;
            }
        }

        [TestMethod]
        public void Rsa_Decrypt_DeserializedKeyAnd64BLongPlainText_ShouldReturnPlainText()
        {
            // Arrange
            // Składamy klucz prywatny z 2 liczb pierwszych.
            // Bajty w kolejności little-endian
            string pLeString =
                "9exTRLqrUsCU4cncab1z1aQGIku2PupipLlYgmP6j5wEKK504dVM2pCWbZDmsLp5oBNggW" +
                "2I5KMIM9WWzo1ezUfhK2AZhUmAd0iAxSww8cCER+H9EN7EHpTN04hL3B7Di4NHtKHyvzMZ" +
                "V4JTYC8b/lew8gXZzetcMhEqrqCLjwU=";
            string qLeString =
                "hb4xlb5QblKo1EOTy3XDe8+uNRXidmwhoracoZriSLL96jEI4fOFgLuDpxzCHoNQDB7e+Z" +
                "vFjK4NMIqPmf0DLiaBw5CFpce7JcOavqupzcplqqwzuZ0WOVfZEvLkz3vXC2TB5GDWd1Gx" +
                "6pjPo4QvZq+wsVmggA2DIki5ZqsTRGs=";

            // Zamieniamy na big-endian
            byte[] p = Convert.FromBase64String(pLeString);
            Reverse(ref p);
            byte[] q = Convert.FromBase64String(qLeString);
            Reverse(ref q);

            string pBeString = Convert.ToBase64String(p);
            string qBeString = Convert.ToBase64String(q);

            string pqString = $"{pBeString};{qBeString}";
            PrivateKey privateKey = PrivateKey.Parse(pqString);

            byte[] ciphText = Convert.FromBase64String("ADu4fg594oNA3WNTkIkqYQevst2A/k" +
                "jHWqk+dCRg09bEYj5vefJQqA7fCxMyYfeJzyOQ+hQumNan3Fy4QV3WlxZrlrMV8qe0pCb" +
                "uPMpZ6m+AFZBxmWSo3Tb5TLNH2Nf2lVWcAdyIwO5pbetL4Dmn/I031E+Q2DUykuFm+qa1" +
                "9D6LDPmyo49qJr9LpJtV60cWvQH6hwuHd5SORMmhPTygB26NJbdEbXumaYj3jS2XqF3vw" +
                "RXVG/jvyyqYiuuiqpq+JMDEV68Q7l6Z+X+1jCdD00+ecLDUJ7evZt6un5qv85Q4ARN55H" +
                "msDzxhOr/DFJDkln9mO+mJyhOvXKh0EHpjfA==");
            string expected = new string('a', 64);

            // Act
            string actual = Encoding.UTF8.GetString(Rsa.Decrypt(privateKey, ciphText));

            // Assert
            Console.WriteLine($"expected: {expected.Length} {expected}");
            Console.WriteLine($"actual: {actual.Length} {actual}");

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Aes_Decrypt_WhenEncrypted1MiBLongPlainText_ShouldReturnPlainText()
        {
            // Arrange
            var (key, iv) = Aes.GenerateKeyIv();

            string plainText = new string('a', 1 << 20); // 2^20 razy 'a'
            byte[] ciphText = Aes.Encrypt(key, iv, Encoding.UTF8.GetBytes(plainText));

            string expected = plainText;

            // Act
            string actual = Encoding.UTF8.GetString(Aes.Decrypt(key, iv, ciphText));

            // Assert
            Console.WriteLine($"expected: {expected.Length}");
            Console.WriteLine($"actual: {actual.Length}");

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Rsa_Encrypt_258BLongPublicKey_ShouldNotThrow()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                // Arrange
                PrivateKey privateKey = PrivateKey.Random(2064);
                PublicKey publicKey = privateKey.ToPublicKey();

                byte[] plain = Encoding.UTF8.GetBytes("abcd");

                // Act
                byte[] cipher = Rsa.Encrypt(publicKey, plain);

                // Assert
                Console.WriteLine(publicKey.Length);
                Console.WriteLine(Encoding.UTF8.GetString(Rsa.Decrypt(privateKey, cipher)));
            }
        }
    }
}
