using System;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public class HybridCryptosystem : IDisposable
    {
        // rozmiar w bajtach bloku, na które będzie dzielona wiadomość
        private const int BLOCK_SIZE = 16;
        private RSA _rsa = RSA.Create();

        public HybridCryptosystem() { }

        public void Dispose()
        {
            _rsa.Dispose();
        }

        private byte[] GenerateRandom(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }

        private Aes CreateAes()
        {
            var aes = Aes.Create();
            /* PKCS7 jest metodą paddingu, czyli dopełniania tekstu jawnego do pełnych
             * bloków szyfru blokowego (u nas AES) przed jej zaszyfrowaniem. Zgodnie ze
             * specyfikacją w RFC5652, na końcu tekstu jawnego o długości l (w oktetach),
             * przy szyfrze o rozmiarze bloku równym k (w oktetach, metoda jest określona
             * tylko dla szyfrów o k < 256), dopisujemy k-(l mod k) oktetów o wartości 
             * k-(l mod k), np.
             * wiadomość DD DD DD DD | DD DD po dopełnieniu będzie ciągiem
             * DD DD DD DD | DD DD 02 02
             * wiadomość 12 34 56 78 | 90 12 34 56 po dopełnieniu będzie ciągiem
             * 12 34 56 78 | 90 12 34 56 | 04 04 04 04 */
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        /* public byte[] Encrypt(byte[] plain)
        {
            byte[] key = GenerateRandom(128 / 8); // 128 b - rozmiar klucza AESa
            byte[] iv = GenerateRandom(128 / 8); // 128 b - rozmiar bloku AESa
            using (var ms = new MemoryStream())
            {
                _rsa.
                var ciphTxt = _rsa.Encrypt(buffer, padding);
            }
            using (var aes = CreateAes())
            using (var enc = aes.CreateEncryptor(key, iv))
            using (var inFS = File.OpenRead(path))
            using (var outFS = File.OpenWrite(path + ".temp"))
            using (var cs = new CryptoStream(outFS, enc, CryptoStreamMode.Write))
            {
                progress.FineMax = inFS.Length;
                return TransformFile(progress, inFS, cs);
            }

            var padding = RSAEncryptionPadding.OaepSHA256;
            byte[] buffer = new byte[blockSize];
            using (var rsa = System.Security.Cryptography.RSA.Create())
            using (var ms = new MemoryStream())
            {
                for (int i = 0; i < plain.Length; i += blockSize)
                {
                    Buffer.BlockCopy(plain, i, buffer, 0, blockSize);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                int rem = plain.Length % blockSize;
                if (rem > 0)
                {
                    Array.Resize(ref buffer, rem);
                    Buffer.BlockCopy(plain, plain.Length - rem, buffer, 0, rem);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                return ms.ToArray();
            }
        }

        private byte[] EncryptAesKeyIv()
        {

        } */

        /* public byte[] Decrypt(byte[] cipher, int blockSize)
        {
            using (var aes = CreateAes())
            using (var dec = aes.CreateDecryptor(key, initializationVector))
            using (var inFS = File.OpenRead(path))
            using (var outFS = File.OpenWrite(path + ".temp"))
            using (var cs = new CryptoStream(inFS, dec, CryptoStreamMode.Read))
            {
                progress.FineMax = inFS.Length;
                return TransformFile(progress, cs, outFS);
            }

            var padding = RSAEncryptionPadding.OaepSHA256;
            byte[] buffer = new byte[blockSize];
            using (var ms = new MemoryStream())
            {
                for (int i = 0; i < plain.Length; i += blockSize)
                {
                    Buffer.BlockCopy(plain, i, buffer, 0, blockSize);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                int rem = plain.Length % blockSize;
                if (rem > 0)
                {
                    Array.Resize(ref buffer, rem);
                    Buffer.BlockCopy(plain, plain.Length - rem, buffer, 0, rem);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                return ms.ToArray();
            }
        } */

        public void ImportRsaKey(PrivateKey key) => key.ImportTo(_rsa);

        public void ImportRsaKey(PublicKey key) => key.ImportTo(_rsa);
    }
}
