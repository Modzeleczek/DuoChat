using System;
using System.IO;
using System.Security.Cryptography;
using LibraryAes = System.Security.Cryptography.Aes;

namespace Shared.MVVM.Model.Cryptography
{
    public class Aes
    {
        private byte[] key, iv;

        public Aes(byte[] key, byte[] iv)
        {
            this.key = key;
            this.iv = iv;
        }

        public Status Encrypt(byte[] plain)
        {
            using (var aes = CreateAes())
            using (var encryptor = aes.CreateEncryptor(key, iv))
            {
                var status = AesTransform(encryptor, plain);
                if (status.Code != 0)
                    status.Prepend(-1, "|Error occured while| |AES transforming.|"); // -1
                return status;
            }
        }

        private LibraryAes CreateAes()
        {
            var aes = LibraryAes.Create();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        private Status AesTransform(ICryptoTransform transform, byte[] bytes)
        {
            using (var output = new MemoryStream())
            using (var cs = new CryptoStream(output, transform, CryptoStreamMode.Write))
            {
                try
                {
                    cs.Write(bytes, 0, bytes.Length);
                    return new Status(0, output.GetBuffer());
                }
                catch (Exception)
                {
                    return new Status(-1, null, "|Error occured while| " +
                        "|writing to AES transformation stream.|");
                }
            }
        }

        public Status Decrypt(byte[] cipher)
        {
            using (var aes = CreateAes())
            using (var encryptor = aes.CreateDecryptor(key, iv))
            {
                var status = AesTransform(encryptor, cipher);
                if (status.Code != 0)
                    status.Prepend(-1, "|Error occured while| |AES transforming.|"); // -1
                return status;
            }
        }
    }
}
