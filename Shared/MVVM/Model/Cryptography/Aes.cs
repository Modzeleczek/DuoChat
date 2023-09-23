using Shared.MVVM.Core;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public static class Aes
    {
        /* Dla algorytmu Rijndael zgodnego ze specyfikacją AESa
        blok (na które przy szyfrowaniu jest dzielony tekst jawny)
        musi być 128-bitowy. */
        public const int KEY_LENGTH = 256 / 8, BLOCK_LENGTH = 128 / 8;

        public static (byte[], byte[]) GenerateKeyIv()
        {
            return (RandomGenerator.Generate(KEY_LENGTH),
                RandomGenerator.Generate(BLOCK_LENGTH));
        }

        public static byte[] Encrypt(byte[] key, byte[] iv, byte[] plain)
        {
            return Encrypt(key, iv, plain, 0, plain.Length);
        }

        public static byte[] Encrypt(byte[] key, byte[] iv,
            byte[] plain, int startIndex, int count)
        {
            try
            {
                using (var aes = CreateAes(key, iv))
                using (var encryptor = aes.CreateEncryptor())
                    return AesTransform(encryptor, plain, startIndex, count);
            }
            catch (Error e)
            {
                e.Prepend(AesTransformingError());
                throw;
            }
        }

        private static string AesTransformingError() =>
            "|Error occured while| |AES transforming.|";

        private static AesCng CreateAes(byte[] key, byte[] iv)
        {
            var aes = new AesCng(); 
            // w bitach
            aes.KeySize = KEY_LENGTH * 8;
            aes.BlockSize = BLOCK_LENGTH * 8;
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        private static byte[] AesTransform(ICryptoTransform transform,
            byte[] bytes, int startIndex, int count)
        {
            try
            {
                using (var output = new MemoryStream())
                {
                    using (var cs = new CryptoStream(output, transform,
                        CryptoStreamMode.Write, false))
                        cs.Write(bytes, startIndex, count);
                    /* Przy szyfrowaniu, Zanim pobierzemy zaszyfrowane bajty za
                    pomocą MemoryStream.ToArray, CryptoStream musi zostać
                    zamknięty lub sflushowany za pomocą cs.FlushFinalBlock.
                    W przeciwnym przypadku, ostatni blok danych nie zostanie zapisany
                    do MemoryStreama i przy odszyfrowywaniu MemoryStream.ToArray
                    wyrzuci CryptographicException: Padding is invalid and cannot
                    be removed. */
                    return output.ToArray();
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| " +
                    "|writing to AES transformation stream.|");
            }
        }

        public static byte[] Decrypt(byte[] key, byte[] iv, byte[] cipher)
        {
            return Decrypt(key, iv, cipher, 0, cipher.Length);
        }

        public static byte[] Decrypt(byte[] key, byte[] iv,
            byte[] cipher, int startIndex, int count)
        {
            try
            {
                using (var aes = CreateAes(key, iv))
                using (var decryptor = aes.CreateDecryptor())
                    return AesTransform(decryptor, cipher, startIndex, count);
            }
            catch (Error e)
            {
                e.Prepend(AesTransformingError());
                throw;
            }
        }
    }
}
