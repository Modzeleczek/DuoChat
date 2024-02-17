using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
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
            // Zmieniłem formaty kluczy, więc trzeba wygenerować nowe i tu wkleić.

            // Arrange
            PrivateKey privateKey = PrivateKey.Parse(
                "GXLP0l69Qw27thjMPUTUmJ9x7vgXfQ/fTUjygPZTyQoLjIdBJq+S9SE0+zY6+qmc2k2TAO" +
                "VWwoJ8gigYfuJ+IRBnOfLue4XJwNDaZS8qdm8ILHtCfqAqIDUl2+iHSd7rCcaLEZaAnUBT" +
                "30ICrpsQz5/Umghn8sn6DESeJfzDv2k=;afd0rahxsmvuhSjyrGeLFSzt4zYmdnbWg13/p" +
                "zGKqS0zW2zmcoCocRKoN4wyrRy6ACXUzwLHmYLGMr+ri/Jxfx8o+8DDER8mqAacrY5ISOg" +
                "99CjrgtBV+NCIZHV00td4GLaDtK9k/cWNf4oRrFoeBUx/5/dvvKSF83vDiD1VoTw=");
            PublicKey publicKey = privateKey.ToPublicKey();
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1 << 20)); 
            byte[] signature = Convert.FromBase64String(
                "BJ3BwU0dvQOnSZqXD2SJ5mBdWbQrAn7T9jrw9C9fx1Jn12UoVwvKgmecYC3n+BWO0RHM25" +
                "3gPr0PJ9OfScL+Nc56VHXCO/gidc33ZQ64yfYT1f7trrx/CSAsw/Ee5N9NWh2lbuJdpgkO" +
                "U3dnRfd/ltKE/srQVVj3aBhCpn+kK7g83KOZYVUPRwWJmTuFwfoLuPzuDwmOALscXAOUcF" +
                "QHarR0odbzrSkJIYY7PqwCmdIqMoG0tPF8e2BaQgGhI/PJqBRb2Ddi0xwpHk6g2l700TIH" +
                "ClWNxm8dgUI3HYT25X/9yLIf2qIP7xq/ShvAjiXqnsF/1k1KNsGAh60ugFdbwQ==");

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

        [TestMethod]
        public void Rsa_Decrypt_DeserializedKeyAnd64BLongPlainText_ShouldReturnPlainText()
        {
            // Zmieniłem formaty kluczy, więc trzeba wygenerować nowe i tu wkleić.

            // Arrange
            // Składamy klucz prywatny z 2 liczb pierwszych.
            // Kodowanie bez znaku (unsigned) i bajty w kolejności little-endian
            string pULEString =
                "GXLP0l69Qw27thjMPUTUmJ9x7vgXfQ/fTUjygPZTyQoLjIdBJq+S9SE0+zY6+qmc2k2TAO" +
                "VWwoJ8gigYfuJ+IRBnOfLue4XJwNDaZS8qdm8ILHtCfqAqIDUl2+iHSd7rCcaLEZaAnUBT" +
                "30ICrpsQz5/Umghn8sn6DESeJfzDv2k=";
            string qULEString =
                "afd0rahxsmvuhSjyrGeLFSzt4zYmdnbWg13/pzGKqS0zW2zmcoCocRKoN4wyrRy6ACXUzw" +
                "LHmYLGMr+ri/Jxfx8o+8DDER8mqAacrY5ISOg99CjrgtBV+NCIZHV00td4GLaDtK9k/cWN" +
                "f4oRrFoeBUx/5/dvvKSF83vDiD1VoTw=";

            string pqString = $"{pULEString};{qULEString}";
            PrivateKey privateKey = PrivateKey.Parse(pqString);

            byte[] ciphText = Convert.FromBase64String("DJ6/nschLmUXIZdp5hPPUi449iRIFap" +
                "dSFude5mDlTnWZodNX+0NN87NIFbP1HMPv4xg8HiaG0ukQoKYkFEKlXDU8ndNGxbzI15hv" +
                "IR93oFJnk2TKtMXCnCgS3V20gTAmaxbgaV+0n2SlrNUZCqHiC7H/jUcT+llDhi2aQaK6N8" +
                "bWNR1pKGY6SpgsmlQZpOwJ3KVwMBTF+IxFNccVc0aHGL64IYHtyz1MfFyodIQbEm1fOyld" +
                "K5mD1bmnqLoUfO0Mfur8/i9y3/Op35Ddp4cSmEJIDVaGUUE9OzDWEk0WD32HxLH0Erk0aZ" +
                "24p1yh4rjIzOPRmkXJm/n7ArrouQjuQ==");
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
        public void Aes_Decrypt_WhenCipherTextCannotBeDecryptedWithPublicKey_ShouldThrowError()
        {
            // Arrange
            var (key, iv) = Aes.GenerateKeyIv();
            byte[] plainText = new byte[] { 1, 2, 3, };
            byte[] cipherText = Aes.Encrypt(key, iv, plainText);
            byte[] invalidKey = key;
            invalidKey[0] ^= 0b11111111;

            // Act
            var testDelegate = () => Aes.Decrypt(invalidKey, iv, cipherText);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("|Error occured while| |AES transforming.|"));
        }

        [TestMethod]
        public void Rsa_Encrypt_258BLongPublicKey_ShouldNotThrow()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                // Arrange
                PrivateKey privateKey = PrivateKey.Random(258 * 8);
                PublicKey publicKey = privateKey.ToPublicKey();

                byte[] expPlain = Encoding.UTF8.GetBytes("abcd");

                // Act
                byte[] cipher = Rsa.Encrypt(publicKey, expPlain);
                byte[] actPlain = Rsa.Decrypt(privateKey, cipher);

                // Assert
                Console.WriteLine(publicKey.Length);
                Console.WriteLine(Encoding.UTF8.GetString(actPlain));

                Assert.AreEqual(258, publicKey.Length);
                expPlain.BytesEqual(actPlain);
            }
        }

        [TestMethod]
        public void Rsa_Sign_WhenDataNumericValueIsGreaterThanPrivateKeyModulus_ShouldThrowError()
        {
            // Arrange
            var privateKey = PrivateKey.Random(2 * 8, 1 * 8);
            byte[] data = new byte[] { 255, 255, 255, 255 };

            // Act
            var testDelegate = () => Rsa.Sign(privateKey, data);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("|Error occured while| |RSA signing|."));
        }

        [TestMethod]
        public void Rsa_Encrypt_WhenPlainTextNumericValueIsGreaterThanPublicKeyModulus_ShouldThrowError()
        {
            // Arrange
            var privateKey = PrivateKey.Random(2 * 8, 1 * 8);
            var publicKey = privateKey.ToPublicKey();
            byte[] plainText = new byte[] { 255, 255, 255, 255 };

            // Act
            var testDelegate = () => Rsa.Encrypt(publicKey, plainText);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("|Error occured while| |RSA encrypting.|"));
        }

        [TestMethod]
        public void Rsa_Decrypt_WhenCipherTextCannotBeDecryptedWithPublicKey_ShouldThrowError()
        {
            // Arrange
            var privateKey = PrivateKey.Random();
            var publicKey = privateKey.ToPublicKey();
            byte[] plainText = new byte[] { 1, 2, 3 };
            byte[] cipherText = Rsa.Encrypt(publicKey, plainText);
            for (int i = 0; i < cipherText.Length; ++i)
                cipherText[i] ^= 0b11111111;

            // Act
            var testDelegate = () => Rsa.Decrypt(privateKey, cipherText);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("|Error occured while| |RSA decrypting.|"));
        }
    }
}
