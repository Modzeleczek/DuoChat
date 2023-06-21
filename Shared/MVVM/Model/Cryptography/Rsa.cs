using Shared.MVVM.Core;
using System;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public class Rsa
    {
        private static readonly RSAEncryptionPadding RSA_PADDING = RSAEncryptionPadding.OaepSHA256;
        public RsaKey Key { private get; set; }

        public Rsa(RsaKey key) => Key = key;

        public byte[] Encrypt(byte[] plain)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    Key.ImportTo(rsa);
                    return rsa.Encrypt(plain, RSA_PADDING);
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |RSA encrypting.|");
            }
        }

        public byte[] Decrypt(byte[] cipher)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    Key.ImportTo(rsa);
                    return rsa.Decrypt(cipher, RSA_PADDING);
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |RSA decrypting.|");
            }
        }
    }
}
