using System;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public class Rsa
    {
        private static readonly RSAEncryptionPadding RSA_PADDING = RSAEncryptionPadding.OaepSHA256;
        public RsaKey Key { private get; set; }

        public Rsa(RsaKey key) => Key = key;

        public Status Encrypt(byte[] plain)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    Key.ImportTo(rsa);
                    return new Status(0, rsa.Encrypt(plain, RSA_PADDING)); // 0
                }
            }
            catch (Exception)
            {
                return new Status(-1, null, "|Error occured while| |RSA encrypting.|"); // -1
            }
        }

        public Status Decrypt(byte[] cipher)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    Key.ImportTo(rsa);
                    return new Status(0, rsa.Decrypt(cipher, RSA_PADDING)); // 0
                }
            }
            catch (Exception)
            {
                return new Status(-1, null, "|Error occured while| |RSA decrypting.|"); // -1
            }
        }
    }
}
